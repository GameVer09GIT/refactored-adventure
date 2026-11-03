using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorAudio
{
    /// <summary>
    /// Quản lý âm thanh bước chân của người chơi: phân biệt Đất và Gỗ, đi bộ và chạy, tiếng ván gỗ cọt kẹt và tiếp đất.
    /// Hoạt động theo quãng đường di chuyển ngang thực tế, raycast nhận diện bề mặt và không cấp phát rác bộ nhớ.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("AudioSource")]
        [Tooltip("AudioSource 2D chuyên phát tiếng bước chân (không lặp, spatialBlend 0)")]
        [SerializeField] private AudioSource footstepSource;

        [Header("Clips Bước chân Đất")]
        [Tooltip("Các biến thể tiếng bước chân trên đất khi đi bộ")]
        [SerializeField] private AudioClip[] dirtWalkClips;
        [Tooltip("Các biến thể tiếng bước chân trên đất khi chạy")]
        [SerializeField] private AudioClip[] dirtRunClips;

        [Header("Clips Bước chân Gỗ")]
        [Tooltip("Các biến thể tiếng bước chân trên sàn gỗ khi đi bộ")]
        [SerializeField] private AudioClip[] woodWalkClips;
        [Tooltip("Các biến thể tiếng bước chân trên sàn gỗ khi chạy")]
        [SerializeField] private AudioClip[] woodRunClips;

        [Header("Clips Tiếng Gỗ Cọt Kẹt")]
        [Tooltip("Các biến thể tiếng ván gỗ cọt kẹt dưới chân")]
        [SerializeField] private AudioClip[] woodCreakClips;

        [Header("Thông số Nhịp bước (Stride)")]
        [Tooltip("Quãng đường ngang để phát 1 nhịp đi bộ (mét)")]
        [SerializeField] private float walkStride = 1.35f;
        [Tooltip("Quãng đường ngang để phát 1 nhịp chạy (mét)")]
        [SerializeField] private float runStride = 1.80f;
        [Tooltip("Vận tốc ngang tối thiểu để tính bước chân (m/s)")]
        [SerializeField] private float minMoveSpeed = 0.25f;

        [Header("Cấu hình Âm lượng & Cao độ")]
        [Tooltip("Âm lượng cơ bản khi đi bộ")]
        [Range(0f, 1f)][SerializeField] private float walkVolume = 0.95f;
        [Tooltip("Âm lượng cơ bản khi chạy")]
        [Range(0f, 1f)][SerializeField] private float runVolume = 1.0f;
        [Tooltip("Âm lượng cơ bản khi cúi/rón rén")]
#pragma warning disable CS0414
        [Range(0f, 1f)][SerializeField] private float crouchVolume = 0.45f;
#pragma warning restore CS0414
        [Tooltip("Cao độ ngẫu nhiên tối thiểu (pitch min)")]
        [SerializeField] private float minPitch = 0.92f;
        [Tooltip("Cao độ ngẫu nhiên tối đa (pitch max)")]
        [SerializeField] private float maxPitch = 1.08f;
        [Tooltip("Tỷ lệ biến thiên âm lượng ngẫu nhiên (±10%)")]
        [SerializeField] private float volumeVariation = 0.10f;

        [Header("Hiệu ứng Sàn Gỗ")]
        [Tooltip("Xác suất phát tiếng gỗ cọt kẹt khi bước trên gỗ (30%)")]
        [Range(0f, 1f)][SerializeField] private float creakChance = 0.30f;
        [Tooltip("Thời gian hồi giữa 2 lần phát tiếng cọt kẹt (giây)")]
        [SerializeField] private float creakCooldown = 2.0f;
        [Tooltip("Âm lượng tiếng gỗ cọt kẹt")]
        [Range(0f, 1f)][SerializeField] private float creakVolume = 0.85f;

        [Header("Nhận diện Bề mặt (Raycast)")]
        [Tooltip("LayerMask quét bề mặt (bỏ qua Player và Ma)")]
        [SerializeField] private LayerMask groundLayerMask = ~0;
        [Tooltip("Khoảng cách raycast xuống dưới từ chân người chơi (mét)")]
        [SerializeField] private float rayDistance = 1.2f;

        private PlayerController playerController;
        private CharacterController characterController;
        private Vector3 lastPosition;
        private float accumulatedDistance = 0f;
        private float creakTimer = 0f;
        private int lastClipIndex = -1;
        private int lastCreakIndex = -1;

        private bool wasMoving = false;
        private float stoppedTimer = 0f;

        private bool wasGrounded = true;
        private float airborneTimer = 0f;
        private bool isPlayerDead = false;

        // Bộ đệm Raycast không cấp phát bộ nhớ rác mỗi frame
        private readonly RaycastHit[] raycastHits = new RaycastHit[6];

        // Cache bề mặt theo Collider để tăng tốc và không gọi tìm kiếm chuỗi lặp đi lặp lại
        private struct SurfaceInfo
        {
            public SurfaceType surface;
            public string rule;
            public string materialName;
        }
        private readonly Dictionary<Collider, SurfaceInfo> surfaceCache = new Dictionary<Collider, SurfaceInfo>();

        void Awake()
        {
            surfaceCache.Clear();
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();

            if (footstepSource == null)
            {
                // Tìm child AudioSource có tên "FootstepAudio"
                Transform footstepChild = transform.Find("FootstepAudio");
                if (footstepChild != null)
                {
                    footstepSource = footstepChild.GetComponent<AudioSource>();
                }

                // Fallback: tìm bất kỳ AudioSource con nào
                if (footstepSource == null)
                {
                    footstepSource = GetComponentInChildren<AudioSource>();
                }

                // Fallback cuối: tự tạo AudioSource
                if (footstepSource == null)
                {
                    Debug.LogWarning("[PlayerFootsteps] Không tìm thấy AudioSource! Tự tạo mới.");
                    GameObject go = new GameObject("FootstepAudio");
                    go.transform.SetParent(transform, false);
                    footstepSource = go.AddComponent<AudioSource>();
                }
            }

            if (footstepSource != null)
            {
                footstepSource.spatialBlend = 0f; // 2D Sound
                footstepSource.playOnAwake = false;
                footstepSource.loop = false;
                footstepSource.volume = 1f;
            }
        }

        void OnEnable()
        {
            surfaceCache.Clear();
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
            lastPosition = transform.position;
            wasGrounded = characterController != null ? characterController.isGrounded : true;

            // Kiểm tra xem các clip gỗ có bị rỗng hoặc missing không (do vừa xóa file âm thanh cũ)
            bool needsWoodReload = (woodWalkClips == null || woodWalkClips.Length == 0 || woodWalkClips[0] == null)
                                || (woodRunClips == null || woodRunClips.Length == 0 || woodRunClips[0] == null);
            if (needsWoodReload)
            {
                LoadWoodClips();
            }

            // Kiểm tra và nạp clips tổng quát nếu chưa có
            int totalClips = GetTotalAssignedClips();
            if (totalClips == 0)
            {
                TryAutoLoadClips();
                totalClips = GetTotalAssignedClips();
            }

            Debug.Log($"<color=green><b>[PlayerFootsteps] Sẵn sàng: {totalClips} clip bước chân (Đất: {dirtWalkClips?.Length} đi, Gỗ: {woodWalkClips?.Length} đi / {woodRunClips?.Length} chạy), AudioSource={(footstepSource != null)}</b></color>");

            // Khởi tạo nhận diện bề mặt ban đầu ngay vị trí spawn
            Collider initialGround = GetGroundCollider();
            if (initialGround != null)
            {
                DetectSurface(initialGround);
            }
        }

        private int GetTotalAssignedClips()
        {
            int total = 0;
            if (dirtWalkClips != null) foreach (var c in dirtWalkClips) if (c != null) total++;
            if (dirtRunClips != null) foreach (var c in dirtRunClips) if (c != null) total++;
            if (woodWalkClips != null) foreach (var c in woodWalkClips) if (c != null) total++;
            if (woodRunClips != null) foreach (var c in woodRunClips) if (c != null) total++;
            return total;
        }

        /// <summary>
        /// Tự động nạp clip từ Resources hoặc Assets/Audio/ khi clip chưa được gán qua Editor Setup
        /// </summary>
        public void TryAutoLoadClips()
        {
            dirtWalkClips = LoadClips("Audio/Footsteps/Dirt/dirt_walk_", "Assets/Audio/Footsteps/Dirt/dirt_walk_", 4);
            dirtRunClips = LoadClips("Audio/Footsteps/Dirt/dirt_run_", "Assets/Audio/Footsteps/Dirt/dirt_run_", 4);
            
            // Tải clip bước chân sàn gỗ thực tế từ amthanhsango
            LoadWoodClips();

            woodCreakClips = LoadClips("Audio/Footsteps/Wood/wood_creak_", "Assets/Audio/Footsteps/Wood/wood_creak_", 3);

            int loaded = GetTotalAssignedClips();
            if (loaded > 0)
            {
                Debug.Log($"<color=cyan>[PlayerFootsteps] Đã nạp thành công {loaded} clips âm thanh bước chân!</color>");
            }
            else
            {
                Debug.LogError("[PlayerFootsteps] Không tìm thấy file âm thanh bước chân trong Resources/ hoặc Assets/Audio/!");
            }
        }

        /// <summary>
        /// Nạp các clip bước chân sàn gỗ thật từ amthanhsango, phân chia đi bộ và chạy hợp lý
        /// </summary>
        private void LoadWoodClips()
        {
            AudioClip[] allWoodClips = Resources.LoadAll<AudioClip>("Audio/Footsteps/Wood/amthanhsango");

#if UNITY_EDITOR
            if (allWoodClips == null || allWoodClips.Length == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Footsteps/Wood/amthanhsango" });
                if (guids != null && guids.Length > 0)
                {
                    var list = new List<AudioClip>();
                    foreach (string guid in guids)
                    {
                        string p = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                        if (c != null) list.Add(c);
                    }
                    allWoodClips = list.ToArray();
                }
            }
#endif

            if (allWoodClips != null && allWoodClips.Length > 0)
            {
                System.Array.Sort(allWoodClips, (a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

                // Nếu chỉ có 1 clip duy nhất: dùng chung cho cả đi bộ và chạy
                if (allWoodClips.Length == 1)
                {
                    woodWalkClips = new AudioClip[] { allWoodClips[0] };
                    woodRunClips = new AudioClip[] { allWoodClips[0] };
                    return;
                }

                // Nếu có nhiều clip: phân chia đi bộ và chạy
                // Nhóm chạy: các clip có độ nảy và lực đập sàn mạnh mẽ
                var runIndices = new HashSet<int> { 1, 5, 6, 7, 9, 14, 17, 21, 23, 28, 29, 30, 35, 38, 42 };
                // Nhóm nghi ngờ quá nhỏ (< 0.15 Peak) để người dùng kiểm tra qua tai nghe
                var quietIndices = new HashSet<int> { 11, 24, 25, 33, 37, 40 };

                var walkList = new List<AudioClip>();
                var runList = new List<AudioClip>();

                for (int i = 0; i < allWoodClips.Length; i++)
                {
                    int stepNum = i + 1;
                    if (runIndices.Contains(stepNum))
                    {
                        runList.Add(allWoodClips[i]);
                    }
                    else if (!quietIndices.Contains(stepNum))
                    {
                        walkList.Add(allWoodClips[i]);
                    }
                }

                if (walkList.Count == 0) walkList.AddRange(allWoodClips);
                if (runList.Count == 0) runList.AddRange(allWoodClips);

                woodWalkClips = walkList.ToArray();
                woodRunClips = runList.ToArray();
                return;
            }

            // Dự phòng nạp clips cũ nếu không có amthanhsango
            woodWalkClips = LoadClips("Audio/Footsteps/Wood/wood_walk_", "Assets/Audio/Footsteps/Wood/wood_walk_", 4);
            woodRunClips = LoadClips("Audio/Footsteps/Wood/wood_run_", "Assets/Audio/Footsteps/Wood/wood_run_", 4);
        }

        private AudioClip[] LoadClips(string resPrefix, string assetPathPrefix, int count)
        {
            var clips = new AudioClip[count];
            int valid = 0;
            for (int i = 0; i < count; i++)
            {
                string suffix = (i + 1).ToString("D2");
                // 1. Nạp từ Resources (hoạt động runtime ở mọi nền tảng)
                AudioClip clip = Resources.Load<AudioClip>($"{resPrefix}{suffix}");

#if UNITY_EDITOR
                // 2. Dự phòng nạp qua AssetDatabase trong Editor
                if (clip == null)
                {
                    clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{assetPathPrefix}{suffix}.wav");
                }
#endif
                if (clip != null)
                {
                    clips[i] = clip;
                    valid++;
                }
            }
            return valid > 0 ? clips : null;
        }

        void LateUpdate()
        {
            if (isPlayerDead || Time.timeScale <= 0f) return;
            if (playerController == null || !playerController.enabled) return;

            // Đếm thời gian hồi cho tiếng cọt kẹt
            if (creakTimer > 0f)
            {
                creakTimer -= Time.deltaTime;
            }

            Vector3 currentPos = transform.position;
            Vector3 delta = currentPos - lastPosition;
            delta.y = 0f; // Chỉ tính quãng đường ngang
            float horizontalDistance = delta.magnitude;
            lastPosition = currentPos;

            // Kiểm tra mặt đất kết hợp cả CharacterController và Raycast ngắn xuống sàn
            bool isGrounded = (characterController != null && characterController.isGrounded)
                || Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 0.95f, groundLayerMask, QueryTriggerInteraction.Ignore);

            // Xử lý tiếng tiếp đất khi rơi/nhảy
            HandleLanding(isGrounded);

            // Tính vận tốc di chuyển ngang thực tế
            float horizontalSpeed = Time.deltaTime > 0f ? (horizontalDistance / Time.deltaTime) : 0f;
            if (playerController != null && playerController.HorizontalSpeed > horizontalSpeed)
            {
                horizontalSpeed = playerController.HorizontalSpeed;
            }

            // Đang đi hoặc chạy trên mặt đất
            if (isGrounded && horizontalSpeed >= minMoveSpeed && horizontalDistance > 0.0005f)
            {
                bool isRunning = playerController != null && playerController.IsRunning;
                float currentStride = isRunning ? runStride : walkStride;

                // Nếu sàn gỗ chỉ có 1 clip duy nhất: rút ngắn nhịp bước khi chạy để tạo cảm giác bước dồn dập
                if (isRunning && woodRunClips != null && woodRunClips.Length == 1 && woodWalkClips != null && woodWalkClips.Length == 1 && woodRunClips[0] == woodWalkClips[0])
                {
                    Collider checkCol = GetGroundCollider();
                    if (DetectSurface(checkCol) == SurfaceType.Wood)
                    {
                        currentStride = walkStride * 0.85f;
                    }
                }

                if (!wasMoving)
                {
                    // Vừa bắt đầu bước đi: phát ngay nhịp bước đầu tiên sau ~0.15m để không có độ trễ im lặng
                    accumulatedDistance = currentStride * 0.85f;
                    wasMoving = true;
                    stoppedTimer = 0f;
                }

                accumulatedDistance += horizontalDistance;

                if (accumulatedDistance >= currentStride)
                {
                    accumulatedDistance -= currentStride;
                    PlayFootstep(isRunning);
                }
            }
            else
            {
                // Khi dừng lại, đợi một khoảng ngắn trước khi reset trạng thái bước chân
                if (wasMoving)
                {
                    stoppedTimer += Time.deltaTime;
                    if (stoppedTimer > 0.35f)
                    {
                        wasMoving = false;
                        stoppedTimer = 0f;
                        accumulatedDistance = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý sự kiện rơi xuống đất sau khi ở trên không
        /// </summary>
        private void HandleLanding(bool isGrounded)
        {
            if (!isGrounded)
            {
                airborneTimer += Time.deltaTime;
                wasGrounded = false;
            }
            else
            {
                if (!wasGrounded)
                {
                    // Vừa chạm đất sau một khoảng thời gian trên không >= 0.18 giây
                    if (airborneTimer >= 0.18f)
                    {
                        PlayLandingSound();
                    }
                    airborneTimer = 0f;
                    wasGrounded = true;
                }
            }
        }

        /// <summary>
        /// Phát âm thanh bước chân
        /// </summary>
        private void PlayFootstep(bool isRunning)
        {
            if (footstepSource == null) return;

            Collider groundCol = GetGroundCollider();
            SurfaceType surface = DetectSurface(groundCol);

            AudioClip[] clipPool;
            if (surface == SurfaceType.Wood)
            {
                clipPool = isRunning ? woodRunClips : woodWalkClips;
            }
            else
            {
                clipPool = isRunning ? dirtRunClips : dirtWalkClips;
            }

            if (clipPool == null || clipPool.Length == 0) return;

            // Chọn ngẫu nhiên không lặp lại ngay clip vừa phát
            int chosenIndex = Random.Range(0, clipPool.Length);
            if (clipPool.Length > 1 && chosenIndex == lastClipIndex)
            {
                chosenIndex = (chosenIndex + 1) % clipPool.Length;
            }
            lastClipIndex = chosenIndex;

            AudioClip clip = clipPool[chosenIndex];
            if (clip == null) return;

            // Tính âm lượng và pitch ngẫu nhiên
            float baseVol = isRunning ? runVolume : walkVolume;
            float randomizedVol = baseVol * Random.Range(1f - volumeVariation, 1f + volumeVariation);
            float randomizedPitch = Random.Range(minPitch, maxPitch);

            // Yêu cầu 4: Nếu chỉ có 1 clip gỗ duy nhất dùng chung cho đi bộ và chạy, khi chạy tăng pitch (khoảng 1.15 - 1.20)
            if (surface == SurfaceType.Wood && isRunning && woodRunClips != null && woodRunClips.Length == 1 && woodWalkClips != null && woodWalkClips.Length == 1 && woodRunClips[0] == woodWalkClips[0])
            {
                randomizedPitch *= Random.Range(1.15f, 1.20f);
            }

            footstepSource.pitch = randomizedPitch;
            footstepSource.PlayOneShot(clip, Mathf.Clamp01(randomizedVol));

            // Xác suất phát thêm tiếng cọt kẹt nếu đang đi trên sàn gỗ
            if (surface == SurfaceType.Wood && creakTimer <= 0f && woodCreakClips != null && woodCreakClips.Length > 0)
            {
                if (Random.value <= creakChance)
                {
                    PlayWoodCreak();
                }
            }
        }

        /// <summary>
        /// Phát tiếng ván gỗ cọt kẹt
        /// </summary>
        private void PlayWoodCreak()
        {
            if (footstepSource == null || woodCreakClips == null || woodCreakClips.Length == 0) return;

            int index = Random.Range(0, woodCreakClips.Length);
            if (woodCreakClips.Length > 1 && index == lastCreakIndex)
            {
                index = (index + 1) % woodCreakClips.Length;
            }
            lastCreakIndex = index;

            AudioClip creakClip = woodCreakClips[index];
            if (creakClip != null)
            {
                float creakPitch = Random.Range(0.95f, 1.05f);
                float vol = creakVolume * Random.Range(0.9f, 1.1f);
                footstepSource.PlayOneShot(creakClip, Mathf.Clamp01(vol));
                creakTimer = creakCooldown;
            }
        }

        /// <summary>
        /// Phát âm thanh khi tiếp đất
        /// </summary>
        private void PlayLandingSound()
        {
            if (footstepSource == null) return;

            Collider groundCol = GetGroundCollider();
            SurfaceType surface = DetectSurface(groundCol);

            AudioClip[] clipPool = (surface == SurfaceType.Wood) ? woodRunClips : dirtRunClips;
            if (clipPool == null || clipPool.Length == 0)
            {
                clipPool = (surface == SurfaceType.Wood) ? woodWalkClips : dirtWalkClips;
            }

            if (clipPool != null && clipPool.Length > 0)
            {
                int idx = Random.Range(0, clipPool.Length);
                AudioClip landClip = clipPool[idx];
                if (landClip != null)
                {
                    footstepSource.pitch = Random.Range(0.88f, 0.98f); // Hơi trầm hơn một chút khi tiếp đất
                    footstepSource.PlayOneShot(landClip, Mathf.Clamp01(runVolume * 1.1f));
                }
            }
        }

        private float noHitTimer = 0f;
        private SurfaceType lastDetectedSurface = SurfaceType.Dirt;
        private string lastDecidingRule = "Default";
        private string lastHitColliderName = "None";
        private string lastHitMaterialName = "None";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool showDebugOverlay = false;
#endif

        private static readonly HashSet<string> WoodTokens = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "wood", "wooden", "plank", "planks", "parquet", "timber", "lumber", "board", "boards", "deck", "floorboard"
        };

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CheckDebugInput();
            if (showDebugOverlay)
            {
                Collider currentGround = GetGroundCollider();
                DetectSurface(currentGround);
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void CheckDebugInput()
        {
            bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame)
                {
                    togglePressed = true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    togglePressed = true;
                }
            }
            catch {}
#endif
            if (togglePressed)
            {
                showDebugOverlay = !showDebugOverlay;
            }
        }

        void OnGUI()
        {
            if (!showDebugOverlay) return;

            string surfaceColor = (lastDetectedSurface == SurfaceType.Wood) ? "#55FF55" : "#FFCC44";
            string text = $"<b>[FOOTSTEP DEBUG - F3/F9]</b>\n" +
                          $"Bề mặt: <color={surfaceColor}><b>{lastDetectedSurface}</b></color>\n" +
                          $"Quy tắc: <b>{lastDecidingRule}</b>\n" +
                          $"Collider: <b>{lastHitColliderName}</b>\n" +
                          $"Material: <b>{lastHitMaterialName}</b>";

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.Box(new Rect(10, 10, 360, 100), GUIContent.none);
            GUI.color = Color.white;

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 12
            };
            labelStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(16, 14, 350, 92), text, labelStyle);
        }
#endif

        /// <summary>
        /// Bắn tia Raycast xuống chân người chơi để lấy collider mặt đất gần nhất, loại bỏ Player, Ma và Trigger Zones
        /// </summary>
        private Collider GetGroundCollider()
        {
            // Raycast từ vị trí chân nâng lên khoảng 0.2m, dài tối đa 1.0m (đủ bắt bậc thang/dốc, không xuyên qua sàn xuống đất nền dưới nhà)
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            float distance = Mathf.Clamp(rayDistance, 0.7f, 1.1f);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, raycastHits, distance, groundLayerMask, QueryTriggerInteraction.Ignore);

            Collider closestCol = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = raycastHits[i].collider;
                if (col == null) continue;
                if (col.isTrigger) continue;
                if (col == characterController) continue;
                if (col.transform.IsChildOf(transform)) continue;
                if (IsGhostCollider(col)) continue;

                // CHỌN COLLIDER GẦN NHẤT: Bề mặt người chơi đang thực sự tiếp xúc trực tiếp dưới chân!
                // Tránh tình trạng tia xuyên qua sàn gỗ chạm vào đất nền bên dưới do RaycastNonAlloc không tự sắp xếp thứ tự khoảng cách.
                if (raycastHits[i].distance < minDistance)
                {
                    minDistance = raycastHits[i].distance;
                    closestCol = col;
                }
            }

            return closestCol;
        }

        private static bool IsGhostCollider(Collider col)
        {
            if (col == null) return false;
            string n = col.gameObject.name;
            if (n.IndexOf("Ghost", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (col.GetComponentInParent<GhostAI>() != null) return true;
            try
            {
                if (col.CompareTag("Ghost")) return true;
            }
            catch
            {
                // Bỏ qua an toàn nếu tag chưa được nạp
            }
            return false;
        }

        private static readonly HashSet<string> DirtTokens = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "ground", "dirt", "earth", "darkearth", "mud", "soil", "terrain", "grass", "sand", "gravel", "pavement", "landscape"
        };

        /// <summary>
        /// Nhận diện loại bề mặt theo thứ tự ưu tiên nghiêm ngặt:
        /// 1. SÀN GỖ & CẦU THANG & ĐƯỜNG GỖ (Villa Floor, Stairs, WoodPath...): Ưu tiên tuyệt đối là WOOD.
        /// 2. NỀN ĐẤT NGOÀI TRỜI (Ground / Dirt / Terrain): Nhận diện chắc chắn từ collider mặt đất (Ground_North_Full, Terrain, DarkEarth...),
        ///    bảo vệ tuyệt đối nền đất ngoài trời không bị bất kỳ vùng ghi đè (Zone) nào can thiệp sai thành Gỗ.
        /// 3. FootstepSurface: Component gắn trực tiếp trên collider hoặc tối đa 2 cấp cha.
        /// 4. VÙNG GỖ TRONG NHÀ (FootstepZone): Đảm bảo các tầng nhà sàn, phòng văn phòng, bến tàu luôn phát âm thanh gỗ.
        /// 5. Tên Material / Texture chứa từ khóa gỗ (so khớp từ nguyên).
        /// 6. Tên Collider hoặc Cha chứa từ khóa Gỗ (ví dụ Struct_WoodBoard_A, Struct_WoodPath_A...).
        /// 7. PhysicsMaterial.
        /// 8. Mặc định: Dirt.
        /// </summary>
        public SurfaceType DetectSurface(Collider col)
        {
            // Xử lý khi raycast không trúng collider nào (rời mặt đất)
            if (col == null)
            {
                noHitTimer += Time.deltaTime;
                if (noHitTimer > 0.3f)
                {
                    lastDetectedSurface = SurfaceType.Dirt;
                    lastDecidingRule = "Default";
                    lastHitColliderName = "None (Airborne)";
                    lastHitMaterialName = "None";
                }
                return lastDetectedSurface;
            }

            noHitTimer = 0f;

            // Kiểm tra cache collider
            if (surfaceCache.TryGetValue(col, out SurfaceInfo cached))
            {
                lastDetectedSurface = cached.surface;
                lastDecidingRule = cached.rule;
                lastHitColliderName = col.name;
                lastHitMaterialName = cached.materialName;
                return cached.surface;
            }

            SurfaceType detected = EvaluateSurfaceRules(col);
            if (surfaceCache.Count < 500)
            {
                surfaceCache[col] = new SurfaceInfo
                {
                    surface = detected,
                    rule = lastDecidingRule,
                    materialName = lastHitMaterialName
                };
            }
            lastDetectedSurface = detected;
            return detected;
        }

        private SurfaceType EvaluateSurfaceRules(Collider col)
        {
            // 1. SÀN GỖ TRONG NHÀ & BẬC THANG & ĐƯỜNG GỖ: Ưu tiên TUYỆT ĐỐI là GỖ (Wood)!
            // Bất kỳ sàn nhà nào (Villa1_Floor, Villa2_Floor, Floor_Mid, WoodPath, ExtStairs, IntStairs, Docking...)
            // chắc chắn 100% phải là Wood, không thể bị bất kỳ component hay giá trị mặc định nào biến thành Dirt!
            if (IsExplicitWoodSurface(col, out string woodReason))
            {
                lastDecidingRule = "WoodRule";
                lastHitColliderName = col.name;
                lastHitMaterialName = woodReason;
                return SurfaceType.Wood;
            }

            // 2. NỀN ĐẤT NGOÀI TRỜI (Ground / Dirt / Terrain): Bảo vệ tuyệt đối
            // Nếu collider là Ground_North_Full, Terrain, DarkEarth, bùn đất ngoài trời...,
            // chắc chắn là DIRT, không cho phép bất kỳ Zone nào ghi đè thành Wood!
            if (IsExplicitDirtGround(col, out string dirtReason, out string matName))
            {
                lastDecidingRule = "Ground";
                lastHitColliderName = col.name;
                lastHitMaterialName = string.IsNullOrEmpty(matName) ? dirtReason : matName;
                return SurfaceType.Dirt;
            }

            // 3. FootstepSurface gắn trên object hoặc cấp cha
            FootstepSurface surfaceComp = GetFootstepSurfaceNearby(col);
            if (surfaceComp != null)
            {
                lastDecidingRule = "Component";
                lastHitColliderName = col.name;
                lastHitMaterialName = $"FootstepSurface ({surfaceComp.gameObject.name})";
                return surfaceComp.Surface;
            }

            // 3. VÙNG GỖ TRONG NHÀ (FootstepZone): Áp dụng cho các cụm nhà sàn gỗ trong nhà
            // Khi người chơi bước trên sàn nhà (không phải nền đất ngoài trời), vùng này đảm bảo âm thanh Gỗ trọn vẹn
            if (FootstepZone.TryGetZoneSurface(transform.position, out SurfaceType zoneSurface, out FootstepZone matchedZone))
            {
                lastDecidingRule = "Zone";
                lastHitColliderName = col.name;
                lastHitMaterialName = (matchedZone != null) ? matchedZone.name : "FootstepZone";
                return zoneSurface;
            }

            // 4. Tên Material và TEXTURE của Renderer trúng (so khớp từ nguyên)
            if (CheckRendererMaterialOrTextureWood(col, out string matchedName))
            {
                lastDecidingRule = "MaterialName";
                lastHitColliderName = col.name;
                lastHitMaterialName = matchedName;
                return SurfaceType.Wood;
            }

            // 5. Tên Collider hoặc Cha chứa từ khóa Gỗ (ví dụ: Struct_WoodBoard_A, Struct_WoodPath_A...)
            if (ContainsWoodWord(col.name) || (col.transform.parent != null && ContainsWoodWord(col.transform.parent.name)))
            {
                lastDecidingRule = "ObjectName";
                lastHitColliderName = col.name;
                lastHitMaterialName = "WoodObject";
                return SurfaceType.Wood;
            }

            // 6. PhysicsMaterial
            PhysicsMaterial physMat = col.sharedMaterial;
            if (physMat != null && ContainsWoodWord(physMat.name))
            {
                lastDecidingRule = "MaterialName";
                lastHitColliderName = col.name;
                lastHitMaterialName = physMat.name;
                return SurfaceType.Wood;
            }

            // 7. Mặc định là Dirt
            lastDecidingRule = "Default";
            lastHitColliderName = col.name;
            lastHitMaterialName = (col.sharedMaterial != null) ? col.sharedMaterial.name : "None";
            return SurfaceType.Dirt;
        }

        /// <summary>
        /// Nhận diện chắc chắn sàn gỗ trong nhà, cầu thang, đường đi bằng gỗ qua tên collider,
        /// cấu trúc nhà Villa, FootstepSurface component hoặc Material/Texture gỗ.
        /// </summary>
        public static bool IsExplicitWoodSurface(Collider col, out string woodReason)
        {
            woodReason = null;
            if (col == null) return false;

            // TerrainCollider không bao giờ là sàn gỗ
            if (col is TerrainCollider) return false;

            string goName = col.gameObject.name;
            Transform parent = col.transform.parent;
            string parentName = parent != null ? parent.name : string.Empty;

            // 1. Tên GameObject hoặc Parent chứa từ khóa sàn gỗ / cầu thang / đường ván
            // Ví dụ: Villa1_Floor_Mid_A, Villa2_Floor_Mid_A_4x, Struct_WoodPath_A, ExtStairs, IntStairs...
            if (ContainsWoodOrFloorKeyword(goName))
            {
                woodReason = $"Floor/WoodName ({goName})";
                return true;
            }

            if (!string.IsNullOrEmpty(parentName) && ContainsWoodOrFloorKeyword(parentName))
            {
                woodReason = $"ParentFloor/WoodName ({parentName})";
                return true;
            }

            // 2. Cấu trúc Villa / House (trừ móng Base hay đất nền)
            if ((goName.IndexOf("villa", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 parentName.IndexOf("villa", System.StringComparison.OrdinalIgnoreCase) >= 0) &&
                goName.IndexOf("ground", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                parentName.IndexOf("ground", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (goName.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    goName.IndexOf("stair", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    goName.IndexOf("deck", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    goName.IndexOf("plank", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    goName.IndexOf("rug", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    goName.IndexOf("carpet", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    parentName.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    woodReason = $"VillaStructure ({goName})";
                    return true;
                }
            }

            // 3. Renderer Material / Texture chứa từ khóa gỗ hoặc sàn
            Renderer rend = col.GetComponent<Renderer>();
            if (rend == null) rend = col.GetComponentInParent<Renderer>();
            if (rend != null && rend.sharedMaterials != null)
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    Material m = rend.sharedMaterials[i];
                    if (m == null) continue;

                    if (ContainsWoodOrFloorKeyword(m.name))
                    {
                        woodReason = $"Material ({m.name})";
                        return true;
                    }

                    if (m.mainTexture != null && ContainsWoodOrFloorKeyword(m.mainTexture.name))
                    {
                        woodReason = $"Texture ({m.mainTexture.name})";
                        return true;
                    }
                }
            }

            // 4. Nếu collider có FootstepSurface với Surface == SurfaceType.Wood
            FootstepSurface fs = col.GetComponent<FootstepSurface>();
            if (fs == null && parent != null) fs = parent.GetComponent<FootstepSurface>();
            if (fs != null && fs.Surface == SurfaceType.Wood)
            {
                woodReason = $"FootstepSurfaceComponent ({fs.gameObject.name})";
                return true;
            }

            return false;
        }

        private static bool ContainsWoodOrFloorKeyword(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string lower = text.ToLowerInvariant();

            // Nếu chứa từ khóa đất nền ngoài trời thì không phải gỗ
            string checkDirt = lower.Replace("underground", "").Replace("playground", "");
            if (checkDirt.Contains("ground") || checkDirt.Contains("darkearth") || checkDirt.Contains("dirt") || checkDirt.Contains("mud") || checkDirt.Contains("terrain"))
            {
                return false;
            }

            // Từ khóa sàn nhà & gỗ & cầu thang
            if (lower.Contains("floor") || lower.Contains("wood") || lower.Contains("plank") ||
                lower.Contains("parquet") || lower.Contains("timber") || lower.Contains("board") ||
                lower.Contains("stair") || lower.Contains("docking") || lower.Contains("deck") ||
                lower.Contains("rug") || lower.Contains("carpet") || lower.Contains("woodpath") ||
                lower.Contains("pier") || lower.Contains("bridge") || lower.Contains("matres") ||
                lower.Contains("mattress"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Nhận diện chắc chắn nền đất / bùn đất ngoài trời qua tên collider (Ground_North_Full, Terrain...),
        /// material (HorrorGround_DarkEarth...) hoặc texture.
        /// </summary>
        private static bool IsExplicitDirtGround(Collider col, out string dirtReason, out string matName)
        {
            dirtReason = null;
            matName = null;
            if (col == null) return false;

            // 1. TerrainCollider luôn là đất ngoài trời
            if (col is TerrainCollider)
            {
                dirtReason = "TerrainCollider";
                matName = "Terrain";
                return true;
            }

            string goName = col.gameObject.name;

            // Nếu GameObject hoặc Cha có tên chứa từ khóa Gỗ rõ rệt thì không coi là nền đất
            if (ContainsWoodWord(goName) || (col.transform.parent != null && ContainsWoodWord(col.transform.parent.name)))
            {
                return false;
            }

            // 2. Tên GameObject chứa từ khóa nền đất (ví dụ: Ground_North_Full, Ground_South_Full, Ground_East_Strip, Ground_West_Strip, horror_ground_system...)
            if (ContainsDirtWord(goName))
            {
                dirtReason = $"ColliderName ({goName})";
                matName = GetFirstMaterialName(col);
                return true;
            }

            // 3. Tên Material hoặc Texture chứa từ khóa đất (ví dụ: HorrorGround_DarkEarth)
            Renderer rend = col.GetComponent<Renderer>();
            if (rend == null) rend = col.GetComponentInParent<Renderer>();
            if (rend != null && rend.sharedMaterials != null)
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    Material m = rend.sharedMaterials[i];
                    if (m == null) continue;

                    // Nếu material có từ khóa gỗ thì không coi là đất
                    if (ContainsWoodWord(m.name)) return false;

                    if (ContainsDirtWord(m.name))
                    {
                        dirtReason = $"MaterialName ({m.name})";
                        matName = m.name;
                        return true;
                    }

                    if (m.mainTexture != null && ContainsDirtWord(m.mainTexture.name))
                    {
                        dirtReason = $"TextureName ({m.mainTexture.name})";
                        matName = m.mainTexture.name;
                        return true;
                    }
                }
            }

            return false;
        }

        private static string GetFirstMaterialName(Collider col)
        {
            Renderer rend = col.GetComponent<Renderer>();
            if (rend == null) rend = col.GetComponentInParent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                return rend.sharedMaterial.name;
            }
            return (col.sharedMaterial != null) ? col.sharedMaterial.name : "None";
        }

        public static bool ContainsDirtWord(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // So khớp từ nguyên
            int len = text.Length;
            int start = -1;
            for (int i = 0; i <= len; i++)
            {
                bool isChar = (i < len) && char.IsLetterOrDigit(text[i]);
                if (isChar)
                {
                    if (start < 0) start = i;
                }
                else
                {
                    if (start >= 0)
                    {
                        string word = text.Substring(start, i - start);
                        if (DirtTokens.Contains(word)) return true;
                        start = -1;
                    }
                }
            }

            // Kiểm tra chuỗi con thông dụng (chống trường hợp chữ hoa/thường viết liền như HorrorGround, DarkEarth...)
            string lower = text.ToLowerInvariant();
            if (lower.Contains("underground") || lower.Contains("playground"))
            {
                return false;
            }
            if (lower.Contains("ground") || lower.Contains("darkearth") || lower.Contains("dirt") || 
                lower.Contains("mud") || lower.Contains("terrain") || lower.Contains("soil") || lower.Contains("gravel"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tìm FootstepSurface trên chính collider hoặc tối đa 2 cấp cha (không leo xa hơn)
        /// </summary>
        private static FootstepSurface GetFootstepSurfaceNearby(Collider col)
        {
            if (col == null) return null;
            Transform t = col.transform;

            // Cấp 0: chính nó
            FootstepSurface comp = t.GetComponent<FootstepSurface>();
            if (comp != null) return comp;

            // Cấp 1: cha trực tiếp
            if (t.parent != null)
            {
                comp = t.parent.GetComponent<FootstepSurface>();
                if (comp != null) return comp;

                // Cấp 2: ông (cha của cha)
                if (t.parent.parent != null)
                {
                    comp = t.parent.parent.GetComponent<FootstepSurface>();
                    if (comp != null) return comp;
                }
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra tên Material hoặc Texture của Renderer theo từ nguyên (whole-word)
        /// </summary>
        private static bool CheckRendererMaterialOrTextureWood(Collider col, out string matchedName)
        {
            matchedName = null;
            Renderer rend = col.GetComponent<Renderer>();
            if (rend == null) rend = col.GetComponentInParent<Renderer>();
            if (rend == null) return false;

            Material[] mats = rend.sharedMaterials;
            if (mats != null)
            {
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null) continue;

                    if (ContainsWoodWord(m.name))
                    {
                        matchedName = $"Mat: {m.name}";
                        return true;
                    }

                    if (m.mainTexture != null && ContainsWoodWord(m.mainTexture.name))
                    {
                        matchedName = $"Tex: {m.mainTexture.name}";
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// So khớp từ nguyên: tách chuỗi theo ký tự không phải chữ/số rồi kiểm tra từng từ trong danh sách từ khóa gỗ
        /// </summary>
        public static bool ContainsWoodWord(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            int len = text.Length;
            int start = -1;
            for (int i = 0; i <= len; i++)
            {
                bool isChar = (i < len) && char.IsLetterOrDigit(text[i]);
                if (isChar)
                {
                    if (start < 0) start = i;
                }
                else
                {
                    if (start >= 0)
                    {
                        string word = text.Substring(start, i - start);
                        if (WoodTokens.Contains(word)) return true;
                        start = -1;
                    }
                }
            }

            return false;
        }

        public void ClearSurfaceCache()
        {
            surfaceCache.Clear();
        }

        private void HandlePlayerDied()
        {
            isPlayerDead = true;
            accumulatedDistance = 0f;
            StartCoroutine(FadeOutFootsteps(0.3f));
        }

        private void HandlePlayerRespawned()
        {
            isPlayerDead = false;
            accumulatedDistance = 0f;
            creakTimer = 0f;
            airborneTimer = 0f;
            wasGrounded = true;
            lastPosition = transform.position;
        }

        private IEnumerator FadeOutFootsteps(float duration)
        {
            if (footstepSource == null) yield break;
            float startVol = footstepSource.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                footstepSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
            footstepSource.Stop();
            footstepSource.volume = startVol;
        }
    }
}
