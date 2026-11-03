using UnityEngine;

/// <summary>
/// Phát âm thanh ghê rợn liên tục trên con ma.
/// Càng gần người chơi, tiếng ma kêu rên rỉ và thở càng to dần, tạo cảm giác sợ hãi.
/// Hỗ trợ 2 lớp âm thanh: (1) rên rỉ liên tục và (2) tiếng la/rít đột ngột khi rất gần.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GhostAI))]
public class GhostAmbientSound : MonoBehaviour
{
    [Header("AudioSource Chính")]
    [Tooltip("AudioSource 3D phát tiếng rên rỉ liên tục của ma (Loop, 3D)")]
    [SerializeField] private AudioSource ambientSource;

    [Tooltip("AudioSource 3D phát tiếng la/rít ngắn đột ngột (OneShot, 3D)")]
    [SerializeField] private AudioSource scareSource;

    [Header("Audio Clips")]
    [Tooltip("Clip tiếng rên rỉ ghê rợn loop liền mạch")]
    [SerializeField] private AudioClip ghostMoanClip;

    [Tooltip("Clip tiếng thở lạnh lẽo loop liền mạch")]
    [SerializeField] private AudioClip ghostBreathClip;

    [Tooltip("Các biến thể tiếng la/rít đột ngột khi rất gần")]
    [SerializeField] private AudioClip[] ghostScareClips;

    [Header("Khoảng cách")]
    [Tooltip("Khoảng cách tối đa bắt đầu nghe tiếng ma (mét)")]
    [SerializeField] private float maxHearDistance = 25.0f;

    [Tooltip("Khoảng cách gần nhất, âm lượng tối đa (mét)")]
    [SerializeField] private float minHearDistance = 2.0f;

    [Tooltip("Khoảng cách kích hoạt tiếng la/rít đột ngột (mét)")]
    [SerializeField] private float scareDistance = 8.0f;

    [Header("Âm lượng")]
    [Tooltip("Âm lượng tối thiểu khi ở xa (gần như nghe lờ mờ)")]
    [Range(0f, 0.3f)][SerializeField] private float minVolume = 0.02f;

    [Tooltip("Âm lượng tối đa khi ở rất gần")]
    [Range(0.3f, 1f)][SerializeField] private float maxVolume = 0.95f;

    [Tooltip("Âm lượng tiếng la/rít đột ngột")]
    [Range(0f, 1f)][SerializeField] private float scareVolume = 0.85f;

    [Header("Cao độ (Pitch) biến đổi")]
    [Tooltip("Pitch khi ở xa (trầm hơn = rùng rợn)")]
    [SerializeField] private float farPitch = 0.75f;

    [Tooltip("Pitch khi ở gần (cao hơn = khẩn cấp)")]
    [SerializeField] private float nearPitch = 1.15f;

    [Header("Tiếng la/rít ngẫu nhiên")]
    [Tooltip("Thời gian hồi tối thiểu giữa 2 lần tiếng scare (giây)")]
    [SerializeField] private float scareCooldownMin = 8.0f;

    [Tooltip("Thời gian hồi tối đa giữa 2 lần tiếng scare (giây)")]
    [SerializeField] private float scareCooldownMax = 18.0f;

    [Header("Đường cong Âm lượng")]
    [Tooltip("Đường cong điều chỉnh phi tuyến âm lượng theo khoảng cách (0=xa, 1=gần)")]
    [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Biến nội bộ
    private GhostAI ghostAI;
    private Transform playerTransform;
    private float currentVolume = 0f;
    private float currentPitch = 0.75f;
    private float scareTimer = 0f;
    private float nextScareCooldown;
    private int lastScareIndex = -1;
    private bool isPlayerDead = false;

    void Awake()
    {
        ghostAI = GetComponent<GhostAI>();

        // Tạo AudioSource tự động nếu chưa có
        if (ambientSource == null)
        {
            Transform ambientChild = transform.Find("GhostAmbientAudio");
            if (ambientChild == null)
            {
                GameObject go = new GameObject("GhostAmbientAudio");
                go.transform.SetParent(transform, false);
                ambientSource = go.AddComponent<AudioSource>();
            }
            else
            {
                ambientSource = ambientChild.GetComponent<AudioSource>();
                if (ambientSource == null)
                    ambientSource = ambientChild.gameObject.AddComponent<AudioSource>();
            }
        }

        if (scareSource == null)
        {
            Transform scareChild = transform.Find("GhostScareAudio");
            if (scareChild == null)
            {
                GameObject go = new GameObject("GhostScareAudio");
                go.transform.SetParent(transform, false);
                scareSource = go.AddComponent<AudioSource>();
            }
            else
            {
                scareSource = scareChild.GetComponent<AudioSource>();
                if (scareSource == null)
                    scareSource = scareChild.gameObject.AddComponent<AudioSource>();
            }
        }

        ConfigureAudioSource(ambientSource, true);
        ConfigureAudioSource(scareSource, false);

        nextScareCooldown = Random.Range(scareCooldownMin, scareCooldownMax);
    }

    private void ConfigureAudioSource(AudioSource src, bool loop)
    {
        if (src == null) return;
        src.spatialBlend = 1.0f; // 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minHearDistance;
        src.maxDistance = maxHearDistance;
        src.loop = loop;
        src.playOnAwake = false;
        src.volume = 0f;
        src.priority = loop ? 64 : 32;
        src.dopplerLevel = 0f; // Không dùng Doppler cho ma
    }

    void OnEnable()
    {
        DeathScreenManager.OnPlayerDied += HandlePlayerDied;
        DeathScreenManager.OnPlayerRespawned += HandlePlayerRespawned;
    }

    void OnDisable()
    {
        DeathScreenManager.OnPlayerDied -= HandlePlayerDied;
        DeathScreenManager.OnPlayerRespawned -= HandlePlayerRespawned;
    }

    void Start()
    {
        // Tìm Player
        if (playerTransform == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                playerTransform = pc.transform;
            }
            else
            {
                GameObject pGo = GameObject.FindWithTag("Player");
                if (pGo != null) playerTransform = pGo.transform;
            }
        }

        if (ghostMoanClip == null || ghostBreathClip == null || ghostScareClips == null || ghostScareClips.Length == 0)
        {
            TryAutoLoadClips();
        }

        // Bắt đầu phát tiếng rên rỉ
        StartAmbientLoop();
    }

    /// <summary>
    /// Tự động nạp các clip âm thanh ma từ Resources hoặc Assets/Audio/Ghost
    /// </summary>
    public void TryAutoLoadClips()
    {
        // 1. Thử Resources.Load
        if (ghostMoanClip == null)
            ghostMoanClip = Resources.Load<AudioClip>("Audio/Ghost/ghost_moan_loop");
        if (ghostBreathClip == null)
            ghostBreathClip = Resources.Load<AudioClip>("Audio/Ghost/ghost_breath_loop");

        if (ghostScareClips == null || ghostScareClips.Length == 0)
        {
            var scares = new AudioClip[3];
            int valid = 0;
            for (int i = 0; i < 3; i++)
            {
                scares[i] = Resources.Load<AudioClip>($"Audio/Ghost/ghost_scare_{i + 1:D2}");
                if (scares[i] != null) valid++;
            }
            if (valid > 0) ghostScareClips = scares;
        }

#if UNITY_EDITOR
        // 2. Dự phòng nạp qua AssetDatabase trong Editor
        if (ghostMoanClip == null)
            ghostMoanClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ghost/ghost_moan_loop.wav");
        if (ghostBreathClip == null)
            ghostBreathClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ghost/ghost_breath_loop.wav");

        if (ghostScareClips == null || ghostScareClips.Length == 0)
        {
            var scares = new AudioClip[3];
            int valid = 0;
            for (int i = 0; i < 3; i++)
            {
                scares[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Ghost/ghost_scare_{i + 1:D2}.wav");
                if (scares[i] != null) valid++;
            }
            if (valid > 0) ghostScareClips = scares;
        }
#endif
        Debug.Log($"<color=magenta><b>[GhostAmbientSound] Đã nạp âm thanh ma: Moan={(ghostMoanClip != null)}, Breath={(ghostBreathClip != null)}, Scares={ghostScareClips?.Length ?? 0}</b></color>");
    }

    private void StartAmbientLoop()
    {
        if (ambientSource == null) return;

        AudioClip clip = ghostMoanClip != null ? ghostMoanClip : ghostBreathClip;
        if (clip != null)
        {
            ambientSource.clip = clip;
            ambientSource.loop = true;
            ambientSource.volume = minVolume;
            ambientSource.Play();
            Debug.Log($"<color=magenta><b>[GhostAmbientSound] Bắt đầu phát tiếng rên rỉ ghê rợn 3D của ma ({clip.name})!</b></color>");
        }
    }

    void Update()
    {
        if (isPlayerDead || Time.timeScale <= 0f) return;

        // Tự động tìm lại Player nếu bị mất
        if (playerTransform == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerTransform = pc.transform;
            else return;
        }

        float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
        float distance = Mathf.Sqrt(sqrDist);

        // Proximity: 0 = xa (>= maxHearDistance), 1 = rất gần (<= minHearDistance)
        float proximity = 1f - Mathf.InverseLerp(minHearDistance, maxHearDistance, distance);
        proximity = Mathf.Clamp01(proximity);

        // Áp dụng đường cong phi tuyến: càng đến gần ma, âm lượng càng tăng vọt đáng sợ
        float curvedProximity = Mathf.Pow(proximity, 1.35f);

        // Tính target volume và pitch theo cự ly
        float targetVol = Mathf.Lerp(minVolume, maxVolume, curvedProximity);
        float targetPitch = Mathf.Lerp(farPitch, nearPitch, curvedProximity);

        // Nếu người chơi ở ngoài tầm nghe
        if (distance > maxHearDistance)
        {
            targetVol = 0f;
        }

        // Tăng cực đại âm lượng và cao độ rít khi ma đang rượt đuổi người chơi (Chase)
        if (ghostAI != null && ghostAI.CurrentState == GhostAI.GhostState.Chase)
        {
            targetVol = 1.0f;
            targetPitch = Mathf.Max(targetPitch, 1.25f);
        }

        // Chuyển mượt âm lượng và cao độ phản hồi tức thì khi di chuyển
        currentVolume = Mathf.MoveTowards(currentVolume, targetVol, Time.deltaTime * 2.0f);
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, Time.deltaTime * 1.2f);

        if (ambientSource != null)
        {
            ambientSource.volume = Mathf.Clamp01(currentVolume);
            ambientSource.pitch = Mathf.Clamp(currentPitch, 0.5f, 1.6f);

            // Đảm bảo clip luôn loop phát khi trong tầm nghe
            if (!ambientSource.isPlaying && ambientSource.clip != null && currentVolume > 0.005f)
            {
                ambientSource.Play();
            }
        }

        // Xử lý tiếng la/rít đột ngột khi gần người chơi
        HandleScareSound(distance, proximity);
    }

    private void HandleScareSound(float distance, float proximity)
    {
        if (ghostScareClips == null || ghostScareClips.Length == 0) return;
        if (scareSource == null) return;

        scareTimer += Time.deltaTime;

        // Chỉ phát khi trong phạm vi scare và đã hết cooldown
        if (distance <= scareDistance && scareTimer >= nextScareCooldown)
        {
            // Xác suất cao hơn khi gần hơn
            float scareChance = Mathf.Lerp(0.3f, 0.9f, proximity);
            if (Random.value <= scareChance * Time.deltaTime * 2f || scareTimer >= nextScareCooldown + 3f)
            {
                PlayScareSound();
                scareTimer = 0f;
                nextScareCooldown = Random.Range(scareCooldownMin, scareCooldownMax);

                // Khi đang rượt, la nhiều hơn
                if (ghostAI != null && ghostAI.CurrentState == GhostAI.GhostState.Chase)
                {
                    nextScareCooldown *= 0.5f;
                }
            }
        }
    }

    private void PlayScareSound()
    {
        if (ghostScareClips == null || ghostScareClips.Length == 0) return;

        int index = Random.Range(0, ghostScareClips.Length);
        if (ghostScareClips.Length > 1 && index == lastScareIndex)
        {
            index = (index + 1) % ghostScareClips.Length;
        }
        lastScareIndex = index;

        AudioClip clip = ghostScareClips[index];
        if (clip != null && scareSource != null)
        {
            float pitch = Random.Range(0.85f, 1.1f);
            scareSource.pitch = pitch;
            scareSource.PlayOneShot(clip, scareVolume);
        }
    }

    private void HandlePlayerDied()
    {
        isPlayerDead = true;
        // Không tắt hẳn - để tiếng ma vẫn vang trong lúc jumpscare
    }

    private void HandlePlayerRespawned()
    {
        isPlayerDead = false;
        scareTimer = 0f;
        nextScareCooldown = Random.Range(scareCooldownMin, scareCooldownMax);
        currentVolume = 0f;

        if (ambientSource != null)
        {
            ambientSource.volume = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Vòng tròn xanh lá: nghe thấy tiếng ma
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxHearDistance);

        // Vòng tròn cam: tiếng la/rít đột ngột
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, scareDistance);

        // Vòng tròn đỏ: âm lượng tối đa
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, minHearDistance);
    }
}
