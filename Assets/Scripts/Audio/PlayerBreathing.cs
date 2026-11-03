using System.Collections;
using UnityEngine;

namespace HorrorAudio
{
    /// <summary>
    /// Quản lý âm thanh thở hổn hển khi thể lực (stamina) cạn kiệt và tiếng hớp hơi hoảng loạn khi stamina vừa chạm 0.
    /// Có cơ chế Hysteresis (bật thở khi <= 30%, tắt thở khi hồi phục >= 60%) và làm mượt âm lượng/cao độ.
    /// Hỗ trợ chuyển đổi chéo mượt mà 2 nguồn AudioSource (A/B Crossfade) để loại bỏ khựng ở điểm nối loop.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerBreathing : MonoBehaviour
    {
        [Header("AudioSource")]
        [Tooltip("AudioSource 2D chính phát tiếng thở hổn hển (Nguồn A, 2D)")]
        [SerializeField] private AudioSource breathingSource;

        [Tooltip("AudioSource 2D phụ phát luân phiên chéo crossfade (Nguồn B, 2D)")]
        [SerializeField] private AudioSource breathingSourceB;

        [Tooltip("AudioSource 2D phát tiếng hớp hơi một lần khi cạn kiệt lực (Loop tắt, 2D)")]
        [SerializeField] private AudioSource gaspSource;

        [Header("Cấu hình Crossfade Loop")]
        [Tooltip("Bật cơ chế crossfade luân phiên A/B chống khựng vòng lặp")]
        [SerializeField] private bool useCrossfadeLoop = true;

        [Header("Audio Clips")]
        [Tooltip("Clip thở hổn hển thực tế tiengtho")]
        [SerializeField] private AudioClip breathingLoopClip;

        [Tooltip("Các biến thể tiếng hớp hơi bất ngờ khi thể lực vừa chạm 0 (nếu có)")]
        [SerializeField] private AudioClip[] gaspClips;

        [Header("Cân chỉnh Âm lượng Tổng thể")]
        [Tooltip("Hệ số gain cho clip thở để đảm bảo âm lượng tại 0% stamina không vượt quá 0.9 (đề xuất: 0.95 dựa trên peak 0.7837)")]
        [Range(0.1f, 2.0f)][SerializeField] private float clipGain = 0.95f;

        [Header("Ngưỡng Kích hoạt Thể lực (Hysteresis)")]
        [Tooltip("Ngưỡng bắt đầu thở hổn hển (mặc định 0.30 = 30% thể lực)")]
        [Range(0.05f, 0.5f)][SerializeField] private float enterThreshold = 0.30f;

        [Tooltip("Ngưỡng ngừng thở hổn hển (mặc định 0.60 = 60% thể lực)")]
        [Range(0.35f, 0.9f)][SerializeField] private float exitThreshold = 0.60f;

        [Header("Thời gian Fade")]
        [Tooltip("Thời gian fade-in khi bắt đầu thở (giây)")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Tooltip("Thời gian fade-out khi hồi phục thể lực (giây)")]
        [SerializeField] private float fadeOutDuration = 2.5f;

        [Header("Cấu hình Âm lượng & Cao độ")]
        [Tooltip("Âm lượng tại ngưỡng bắt đầu thở (30%)")]
        [Range(0f, 1f)][SerializeField] private float minVolume = 0.15f;

        [Tooltip("Âm lượng tối đa khi kiệt sức (0%)")]
        [Range(0f, 1f)][SerializeField] private float maxVolume = 0.90f;

        [Tooltip("Cao độ (pitch) tại ngưỡng bắt đầu (30%)")]
        [SerializeField] private float minPitch = 0.95f;

        [Tooltip("Cao độ (pitch) khi kiệt sức (0%)")]
        [SerializeField] private float maxPitch = 1.10f;

        [Header("Cấu hình Tiếng Hớp hơi (Gasp)")]
        [Tooltip("Thời gian hồi giữa 2 lần phát tiếng hớp hơi khi cạn lực (giây)")]
        [SerializeField] private float gaspCooldown = 8.0f;

        [Tooltip("Âm lượng tiếng hớp hơi")]
        [Range(0f, 1f)][SerializeField] private float gaspVolume = 0.85f;

        private PlayerController playerController;
        private bool isBreathingActive = false;
        private float currentVolume = 0f;
        private float volumeVelocity = 0f;
        private float currentPitch = 1f;
        private float pitchVelocity = 0f;

        private float gaspTimer = 0f;
        private bool wasStaminaZero = false;
        private bool isPlayerDead = false;

        // Quản lý crossfade A/B không cấp phát rác
        private const float CrossfadeDuration = 0.4f;
        private int activeSourceIndex = 0; // 0 = Source A, 1 = Source B
        private bool isCrossfading = false;
        private float crossfadeTimer = 0f;

        void Awake()
        {
            playerController = GetComponent<PlayerController>();

            if (breathingSource == null)
            {
                Transform child = transform.Find("BreathingAudio");
                if (child != null) breathingSource = child.GetComponent<AudioSource>();
                if (breathingSource == null)
                {
                    GameObject go = new GameObject("BreathingAudio");
                    go.transform.SetParent(transform, false);
                    breathingSource = go.AddComponent<AudioSource>();
                }
            }

            if (breathingSourceB == null)
            {
                Transform child = transform.Find("BreathingAudio_B");
                if (child != null) breathingSourceB = child.GetComponent<AudioSource>();
                if (breathingSourceB == null)
                {
                    GameObject go = new GameObject("BreathingAudio_B");
                    go.transform.SetParent(transform, false);
                    breathingSourceB = go.AddComponent<AudioSource>();
                }
            }

            if (gaspSource == null)
            {
                Transform child = transform.Find("GaspAudio");
                if (child != null) gaspSource = child.GetComponent<AudioSource>();
                if (gaspSource == null)
                {
                    GameObject go = new GameObject("GaspAudio");
                    go.transform.SetParent(transform, false);
                    gaspSource = go.AddComponent<AudioSource>();
                }
            }

            TryAutoLoadClips();
            ConfigureAudioSources();
        }

        public void TryAutoLoadClips()
        {
            if (breathingLoopClip == null)
            {
                // Ưu tiên nạp bản sao loop WAV sạch đã fade 5ms
                breathingLoopClip = Resources.Load<AudioClip>("Audio/Breathing/tiengtho_loop");
#if UNITY_EDITOR
                if (breathingLoopClip == null)
                    breathingLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breathing/tiengtho_loop.wav");
                if (breathingLoopClip == null)
                    breathingLoopClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breathing/tiengtho.mp3");
#endif
            }

            // Nếu slot gasp trống do đã xóa, giữ mảng rỗng để không spam cảnh báo
            if (gaspClips == null)
            {
                gaspClips = new AudioClip[0];
            }
        }

        private void ConfigureAudioSources()
        {
            if (breathingSource != null)
            {
                breathingSource.spatialBlend = 0f; // 2D Sound
                breathingSource.loop = !useCrossfadeLoop;
                breathingSource.playOnAwake = false;
                breathingSource.volume = 0f;
                if (breathingLoopClip != null) breathingSource.clip = breathingLoopClip;
            }

            if (breathingSourceB != null)
            {
                breathingSourceB.spatialBlend = 0f; // 2D Sound
                breathingSourceB.loop = false;
                breathingSourceB.playOnAwake = false;
                breathingSourceB.volume = 0f;
                if (breathingLoopClip != null) breathingSourceB.clip = breathingLoopClip;
            }

            if (gaspSource != null)
            {
                gaspSource.spatialBlend = 0f; // 2D Sound
                gaspSource.loop = false;
                gaspSource.playOnAwake = false;
            }
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

        void Update()
        {
            if (isPlayerDead || Time.timeScale <= 0f) return;
            if (playerController == null) return;

            // Đếm thời gian hồi tiếng hớp hơi
            if (gaspTimer > 0f)
            {
                gaspTimer -= Time.deltaTime;
            }

            float stamina = playerController.NormalizedStamina;

            // Xử lý cơ chế Hysteresis bật/tắt thở
            UpdateHysteresisState(stamina);

            // Xử lý tiếng hớp hơi một lần khi vừa chạm đáy thể lực 0 (nếu có clip)
            CheckZeroStaminaGasp(stamina);

            // Cập nhật âm lượng, cao độ và chuyển nguồn A/B crossfade
            UpdateBreathingAudio(stamina);
        }

        private void UpdateHysteresisState(float stamina)
        {
            if (!isBreathingActive)
            {
                if (stamina <= enterThreshold)
                {
                    isBreathingActive = true;
                    activeSourceIndex = 0;
                    isCrossfading = false;
                    crossfadeTimer = 0f;

                    AudioSource srcA = breathingSource;
                    if (srcA != null && breathingLoopClip != null)
                    {
                        srcA.clip = breathingLoopClip;
                        srcA.time = 0f;
                        srcA.Play();
                    }
                    if (breathingSourceB != null)
                    {
                        breathingSourceB.Stop();
                        breathingSourceB.volume = 0f;
                    }
                }
            }
            else
            {
                // Chỉ tắt khi thể lực hồi phục vượt quá ngưỡng exitThreshold (60%)
                if (stamina >= exitThreshold)
                {
                    isBreathingActive = false;
                }
            }
        }

        private void CheckZeroStaminaGasp(float stamina)
        {
            if (stamina <= 0.005f)
            {
                if (!wasStaminaZero)
                {
                    wasStaminaZero = true;
                    if (gaspTimer <= 0f)
                    {
                        PlayGasp();
                        gaspTimer = gaspCooldown;
                    }
                }
            }
            else if (stamina >= 0.05f)
            {
                wasStaminaZero = false;
            }
        }

        private void PlayGasp()
        {
            if (gaspClips == null || gaspClips.Length == 0) return;

            AudioClip clip = gaspClips[Random.Range(0, gaspClips.Length)];
            if (clip == null) return;

            AudioSource targetSource = gaspSource != null ? gaspSource : breathingSource;
            if (targetSource != null)
            {
                targetSource.PlayOneShot(clip, gaspVolume);
            }
        }

        private void UpdateBreathingAudio(float stamina)
        {
            if (breathingSource == null) return;

            float targetVol = 0f;
            float targetPitch = 1f;

            if (isBreathingActive)
            {
                // Chuẩn hóa mức độ kiệt sức: 0 tại enterThreshold (30%), 1 tại stamina 0
                float exhaustionT = Mathf.InverseLerp(enterThreshold, 0f, stamina);

                targetVol = Mathf.Lerp(minVolume, maxVolume, exhaustionT) * clipGain;
                targetPitch = Mathf.Lerp(minPitch, maxPitch, exhaustionT);

                // Fade-in mượt mà
                float smoothTime = Mathf.Max(0.05f, fadeInDuration * 0.4f);
                currentVolume = Mathf.SmoothDamp(currentVolume, targetVol, ref volumeVelocity, smoothTime);
            }
            else
            {
                targetVol = 0f;
                // Fade-out chậm rãi theo fadeOutDuration
                float smoothTime = Mathf.Max(0.1f, fadeOutDuration * 0.4f);
                currentVolume = Mathf.SmoothDamp(currentVolume, 0f, ref volumeVelocity, smoothTime);

                if (currentVolume <= 0.005f)
                {
                    if (breathingSource.isPlaying) breathingSource.Stop();
                    if (breathingSourceB != null && breathingSourceB.isPlaying) breathingSourceB.Stop();
                    currentVolume = 0f;
                    isCrossfading = false;
                }
            }

            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, 0.2f);
            float masterVol = Mathf.Clamp01(currentVolume);
            float pitchVal = Mathf.Clamp(currentPitch, 0.8f, 1.3f);

            // Xử lý Crossfade Luân phiên A/B
            if (useCrossfadeLoop && breathingLoopClip != null && isBreathingActive)
            {
                AudioSource activeSrc = activeSourceIndex == 0 ? breathingSource : breathingSourceB;
                AudioSource otherSrc = activeSourceIndex == 0 ? breathingSourceB : breathingSource;

                if (activeSrc != null && activeSrc.isPlaying)
                {
                    float remaining = breathingLoopClip.length - activeSrc.time;

                    // Khi thời gian clip còn lại <= 0.4s thì bắt đầu chuyển nguồn chéo
                    if (!isCrossfading && remaining <= CrossfadeDuration && remaining > 0f)
                    {
                        isCrossfading = true;
                        crossfadeTimer = 0f;
                        if (otherSrc != null)
                        {
                            otherSrc.clip = breathingLoopClip;
                            otherSrc.time = 0f;
                            otherSrc.volume = 0f;
                            otherSrc.pitch = pitchVal;
                            otherSrc.Play();
                        }
                    }

                    if (isCrossfading)
                    {
                        crossfadeTimer += Time.deltaTime;
                        float t = Mathf.Clamp01(crossfadeTimer / CrossfadeDuration);

                        activeSrc.volume = masterVol * (1f - t);
                        activeSrc.pitch = pitchVal;

                        if (otherSrc != null)
                        {
                            otherSrc.volume = masterVol * t;
                            otherSrc.pitch = pitchVal;
                        }

                        if (t >= 1f)
                        {
                            activeSrc.Stop();
                            activeSrc.volume = 0f;
                            activeSourceIndex = 1 - activeSourceIndex; // Hoán đổi nguồn đang phát
                            isCrossfading = false;
                        }
                    }
                    else
                    {
                        activeSrc.volume = masterVol;
                        activeSrc.pitch = pitchVal;
                        if (otherSrc != null && otherSrc.isPlaying) otherSrc.Stop();
                    }
                }
            }
            else
            {
                // Fallback nếu không dùng crossfade
                breathingSource.volume = masterVol;
                breathingSource.pitch = pitchVal;
            }
        }

        private void HandlePlayerDied()
        {
            isPlayerDead = true;
            isBreathingActive = false;
            StartCoroutine(FadeOutBreathing(0.3f));
        }

        private void HandlePlayerRespawned()
        {
            isPlayerDead = false;
            isBreathingActive = false;
            currentVolume = 0f;
            volumeVelocity = 0f;
            gaspTimer = 0f;
            wasStaminaZero = false;
            isCrossfading = false;

            if (breathingSource != null)
            {
                breathingSource.volume = 0f;
                breathingSource.Stop();
            }
            if (breathingSourceB != null)
            {
                breathingSourceB.volume = 0f;
                breathingSourceB.Stop();
            }
        }

        private IEnumerator FadeOutBreathing(float duration)
        {
            float startVolA = breathingSource != null ? breathingSource.volume : 0f;
            float startVolB = breathingSourceB != null ? breathingSourceB.volume : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (breathingSource != null) breathingSource.volume = Mathf.Lerp(startVolA, 0f, t);
                if (breathingSourceB != null) breathingSourceB.volume = Mathf.Lerp(startVolB, 0f, t);
                yield return null;
            }

            if (breathingSource != null) { breathingSource.Stop(); breathingSource.volume = 0f; }
            if (breathingSourceB != null) { breathingSourceB.Stop(); breathingSourceB.volume = 0f; }
            currentVolume = 0f;
        }
    }
}
