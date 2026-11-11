using System.Collections;
using System.Collections.Generic;
using HorrorGame.Inventory;
using KhoaHon.Core;
using KhoaHon.DebugTools;
using UnityEngine;
using UnityEngine.AI;

namespace KhoaHon.AI
{
    /// <summary>
    /// "Chấp Pháp Quản Gia" — AI săn đuổi tầng 2 (PROMPT 5.8). Viết mới, mượn ý tưởng LOS + jumpscare của GhostAI.
    /// Quyết định trạng thái nằm ở <see cref="HousekeeperBrain"/> (test được); class này lo cảm nhận, di chuyển, âm thanh, hình ảnh.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class HousekeeperAI : MonoBehaviour, ISaveParticipant
    {
        public static HousekeeperAI Instance { get; private set; }

        [SerializeField] private HousekeeperConfig config;
        [Tooltip("Điểm tuần tra theo thứ tự (hành lang vòng + ghé phòng)")]
        [SerializeField] private Transform[] waypoints = new Transform[0];
        [Tooltip("Điểm nhìn/đầu (dùng cho LOS và jumpscare)")]
        [SerializeField] private Transform head;
        [SerializeField] private Animator animator;
        [Tooltip("Gốc hình ảnh để nghiêng/run khi choáng")]
        [SerializeField] private Transform visualRoot;

        [Header("Âm thanh")]
        [SerializeField] private AudioSource keysSource;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioClip keysLoop;
        [SerializeField] private AudioClip mumbleLoop;
        [SerializeField] private AudioClip lullabyClip;
        [SerializeField] private AudioClip screamClip;
        [SerializeField] private AudioClip whisperRunClip;
        [SerializeField] private AudioSource huntMusic;
        [SerializeField] private AudioClip breathLoop;
        [SerializeField] private AudioSource breathSource;

        public HousekeeperState State => brain != null ? brain.State : HousekeeperState.Patrol;
        public bool Weakened => brain != null && brain.Weakened;
        public HousekeeperBrain Brain => brain;

        private NavMeshAgent agent;
        private HousekeeperBrain brain;
        private PlayerController player;
        private Transform playerCam;
        private FlashlightItem flashlight;
        private int floor2Mask;
        private int wpIndex;
        private float waitUntil;
        private Vector3 investigatePos;
        private bool hasInvestigate;
        private float nextDetect;
        private Perception perception;
        private bool pendingNoise;
        private Vector3 lastSeenPos;
        private float nextLullaby;
        private bool hesitateSaid;
        private bool killing;
        private bool breathStarted;
        private float lookAroundT;
        private readonly RaycastHit[] hitBuf = new RaycastHit[16];

        public void SetWaypoints(Transform[] w) => waypoints = w;

        /// <summary>Cảnh nền menu: đứng bất động sau cửa sổ tầng 2 nhìn ra sân.</summary>
        public void PoseAt(Vector3 pos, float yaw)
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            agent.Warp(pos);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        private void Awake()
        {
            Instance = this;
            agent = GetComponent<NavMeshAgent>();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable()
        {
            GameEvents.NoiseEmitted += OnNoise;
            GameEvents.FlagSet += OnFlag;
            DeathScreenManager.OnPlayerRespawned += OnRespawned;
        }

        private void OnDisable()
        {
            GameEvents.NoiseEmitted -= OnNoise;
            GameEvents.FlagSet -= OnFlag;
            DeathScreenManager.OnPlayerRespawned -= OnRespawned;
        }

        private void Start()
        {
            if (config == null) config = ScriptableObject.CreateInstance<HousekeeperConfig>();
            if (waypoints == null || waypoints.Length == 0)
            {
                // Dự phòng: tự lấy các con của "HK_Waypoints"
                var wr = GameObject.Find("HK_Waypoints");
                if (wr != null) { waypoints = new Transform[wr.transform.childCount]; for (int i = 0; i < waypoints.Length; i++) waypoints[i] = wr.transform.GetChild(i); }
            }
            brain = HousekeeperBrain.FromConfig(config, GetInstanceID());
            brain.Changed += OnStateChanged;
            player = FindFirstObjectByType<PlayerController>();
            if (player != null) playerCam = player.playerCamera;
            int area = NavMesh.GetAreaFromName("Floor2");
            floor2Mask = area >= 0 ? 1 << area : NavMesh.AllAreas;
            agent.areaMask = floor2Mask;
            agent.speed = config.patrolSpeed;
            agent.angularSpeed = 300f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.4f;
            if (GameManager.Instance != null && GameManager.Instance.Has(StoryFlag.Relic_Tho)) brain.SetWeakened(true);
            SetupAudio();
            nextLullaby = Time.time + Random.Range(config.lullabyInterval.x, config.lullabyInterval.y);
            GoToWaypoint(0);
        }

        private void SetupAudio()
        {
            foreach (var s in new[] { keysSource, voiceSource })
            {
                if (s == null) continue;
                s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = config.audioMinDistance; s.maxDistance = config.audioMaxDistance; s.dopplerLevel = 0f;
            }
            if (keysSource != null && keysLoop != null) { keysSource.clip = keysLoop; keysSource.loop = true; keysSource.Play(); }
            if (voiceSource != null && mumbleLoop != null) { voiceSource.clip = mumbleLoop; voiceSource.loop = true; voiceSource.volume = 0.55f; voiceSource.Play(); }
            if (huntMusic != null) { huntMusic.loop = true; huntMusic.volume = 0f; huntMusic.spatialBlend = 0f; if (huntMusic.clip != null) huntMusic.Play(); }
            if (breathSource != null && breathLoop != null)
            {
                breathSource.clip = breathLoop; breathSource.loop = true; breathSource.spatialBlend = 1f;
                breathSource.rolloffMode = AudioRolloffMode.Logarithmic; breathSource.minDistance = 1.2f; breathSource.maxDistance = 14f; breathSource.Play();
            }
        }

        // =====================================================================
        private void Update()
        {
            if (brain == null || player == null) return;
            if (!CheatSettings.AIEnabled || !GameFlow.IsPlaying)
            {
                agent.isStopped = true; UpdateAnim();
                if (huntMusic != null) huntMusic.volume = 0f;
                return;
            }
            if (killing) return;

            if (Time.time >= nextDetect)
            {
                nextDetect = Time.time + config.detectInterval;
                Sense();
            }
            var p = perception;
            p.heardNoise = pendingNoise;
            p.arrivedAtTarget = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.25f;
            pendingNoise = false;
            brain.Tick(Time.deltaTime, p);
            if (p.seesPlayer) lastSeenPos = player.transform.position;
            Act();
            UpdateAudio();
            UpdateAnim();
        }

        private void Sense()
        {
            var hide = HidingSpot.Current;
            perception.playerHidden = hide != null;
            perception.sawPlayerHide = hide != null && hide.SeenEntering;
            perception.seesPlayer = hide == null && CanSeePlayerNow();
            perception.flashlightOnFace = hide == null && FlashlightOnFace();
            perception.playerReachable = IsReachable(hide != null ? hide.OutPoint.position : player.transform.position);
            var dp = player.transform.position - transform.position;
            perception.distanceToPlayer = Mathf.Abs(dp.y) < 1.6f ? new Vector2(dp.x, dp.z).magnitude : dp.magnitude;
            if (perception.flashlightOnFace) SetInvestigate(player.transform.position);
        }

        private void Act()
        {
            switch (brain.State)
            {
                case HousekeeperState.Patrol:
                    agent.isStopped = false; agent.speed = config.patrolSpeed;
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
                    {
                        if (waitUntil < 0f) waitUntil = Time.time + Random.Range(config.patrolWait.x, config.patrolWait.y);
                        else if (Time.time >= waitUntil) { waitUntil = -1f; GoToWaypoint(wpIndex + 1); }
                    }
                    break;

                case HousekeeperState.Suspicious:
                    agent.isStopped = false; agent.speed = config.suspiciousSpeed;
                    if (hasInvestigate) { SetDest(investigatePos); hasInvestigate = false; lookAroundT = 0f; }
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
                    {
                        lookAroundT += Time.deltaTime;
                        transform.rotation *= Quaternion.Euler(0, Mathf.Sin(lookAroundT * 1.6f) * 90f * Time.deltaTime, 0);
                    }
                    break;

                case HousekeeperState.Hunt:
                    float run = player.runSpeed;
                    agent.speed = run * (brain.Weakened ? config.weakenedHuntMultiplier : config.huntSpeedMultiplier);
                    if (brain.Hesitating)
                    {
                        agent.isStopped = true;
                        FaceTowards(player.transform.position, 6f);
                        if (!hesitateSaid) { hesitateSaid = true; Say(whisperRunClip, "sub.hk_run"); }
                    }
                    else
                    {
                        agent.isStopped = false;
                        SetDest(player.transform.position);
                    }
                    break;

                case HousekeeperState.Inspect:
                    var spot = HidingSpot.Current;
                    agent.isStopped = false; agent.speed = config.suspiciousSpeed;
                    if (spot != null)
                    {
                        SetDest(spot.OutPoint.position);
                        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
                        if (arrived)
                        {
                            FaceTowards(spot.transform.position, 4f);
                            if (!breathStarted && BreathHold.Instance != null)
                            {
                                breathStarted = true;
                                BreathHold.Instance.Begin(config.breathHoldDuration, () => brain.OnBreathFailed());
                            }
                        }
                    }
                    break;

                case HousekeeperState.Stunned:
                    agent.isStopped = true;
                    if (visualRoot != null)
                        visualRoot.localRotation = Quaternion.Euler(18f + Mathf.Sin(Time.time * 31f) * 3f, Mathf.Sin(Time.time * 23f) * 4f, 0);
                    break;

                case HousekeeperState.Kill:
                    if (!killing) StartCoroutine(KillRoutine());
                    break;
            }
        }

        private void OnStateChanged(HousekeeperState from, HousekeeperState to)
        {
            if (from == HousekeeperState.Stunned && visualRoot != null) visualRoot.localRotation = Quaternion.identity;
            if (from == HousekeeperState.Inspect && BreathHold.Instance != null && BreathHold.Instance.Active && to != HousekeeperState.Kill) BreathHold.Instance.End();
            breathStarted = false;
            hesitateSaid = false;
            waitUntil = -1f;
            switch (to)
            {
                case HousekeeperState.Patrol: GoToNearestWaypoint(); break;
                case HousekeeperState.Suspicious:
                    if (!hasInvestigate) SetInvestigate(from == HousekeeperState.Hunt ? lastSeenPos : transform.position);
                    break;
                case HousekeeperState.Stunned:
                    if (voiceSource != null && screamClip != null) voiceSource.PlayOneShot(screamClip, 1f);
                    break;
            }
        }

        // =====================================================================
        // Cảm nhận
        /// <summary>Bà có nhìn thấy Vy lúc này không (FOV, tầm nhìn theo đèn pin, LOS).</summary>
        public bool CanSeePlayerNow()
        {
            if (player == null || playerCam == null || head == null) return false;
            var eye = head.position;
            var to = playerCam.position - eye;
            float dist = to.magnitude;
            float range = FlashlightOn() ? config.sightRangeLit : config.sightRangeDark;
            if (dist > range) return false;
            var fwd = transform.forward; fwd.y = 0;
            var flat = to; flat.y = 0;
            // Cận kề (≤ 3 m, cùng tầng): cảm nhận được dù không nằm trong góc nhìn
            bool close = flat.magnitude <= 3f && Mathf.Abs(to.y) < 1.6f;
            if (!close && Vector3.Angle(fwd, flat) > config.fieldOfView * 0.5f) return false;
            return HasLineOfSight(eye, playerCam.position);
        }

        private bool FlashlightOn()
        {
            if (flashlight == null && player != null) flashlight = player.GetComponentInChildren<FlashlightItem>(true);
            return flashlight != null && flashlight.IsOn && flashlight.IsEquipped;
        }

        private bool FlashlightOnFace()
        {
            if (!FlashlightOn() || head == null) return false;
            var to = head.position - playerCam.position;
            if (to.magnitude > config.flashlightFaceRange) return false;
            if (Vector3.Angle(playerCam.forward, to) > config.flashlightConeHalfAngle) return false;
            return HasLineOfSight(head.position, playerCam.position);
        }

        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            int n = Physics.RaycastNonAlloc(from, dir.normalized, hitBuf, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var t = hitBuf[i].collider.transform;
                if (t.IsChildOf(transform) || t.IsChildOf(player.transform)) continue;
                return false;
            }
            return true;
        }

        private bool IsReachable(Vector3 pos)
        {
            if (!NavMesh.SamplePosition(pos, out var hit, 1.2f, floor2Mask)) return false;
            return Mathf.Abs(hit.position.y - pos.y) < 1.4f;
        }

        private void OnNoise(Vector3 pos, float radius, NoiseType type)
        {
            if (brain == null || killing) return;
            if (Vector3.Distance(pos, transform.position) > radius) return;
            if (!IsReachable(pos) && type != NoiseType.Altar) return;
            pendingNoise = true;
            SetInvestigate(pos);
        }

        private void OnFlag(StoryFlag f)
        {
            if (f == StoryFlag.Relic_Tho && brain != null) brain.OnCombTaken();
        }

        /// <summary>Gọi từ máy ảnh flash (Phase 3) khi chụp trúng mặt bà.</summary>
        public void OnFlashed() => brain?.OnFlashed();

        private void SetInvestigate(Vector3 p)
        {
            if (NavMesh.SamplePosition(p, out var h, 3f, floor2Mask)) { investigatePos = h.position; hasInvestigate = true; }
        }

        // =====================================================================
        // Di chuyển
        private void GoToWaypoint(int i)
        {
            if (waypoints == null || waypoints.Length == 0) return;
            wpIndex = (i % waypoints.Length + waypoints.Length) % waypoints.Length;
            SetDest(waypoints[wpIndex].position);
        }

        private void GoToNearestWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                float d = (waypoints[i].position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            GoToWaypoint(best);
        }

        private void SetDest(Vector3 p)
        {
            if (!agent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(p, out var h, 2.5f, floor2Mask)) agent.SetDestination(h.position);
        }

        private void FaceTowards(Vector3 target, float speed)
        {
            var d = target - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), speed * Time.deltaTime);
        }

        // =====================================================================
        // Giết → jumpscare → màn hình chết (DeathScreenManager có sẵn)
        private IEnumerator KillRoutine()
        {
            killing = true;
            agent.isStopped = true;
            var spot = HidingSpot.Current;
            if (spot != null) spot.Exit();
            player.SetControlEnabled(false);
            var cam = playerCam;
            var start = cam.rotation;
            float t = 0f;
            if (voiceSource != null && screamClip != null) voiceSource.PlayOneShot(screamClip, 1f);
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                var look = Quaternion.LookRotation((head.position - cam.position).normalized);
                cam.rotation = Quaternion.Slerp(start, look, t / 0.3f);
                FaceTowards(player.transform.position, 14f);
                yield return null;
            }
            // Mặt bà áp sát ống kính rồi giữ nguyên trong suốt màn jumpscare
            var camStart = cam.position;
            var closePos = head.position + transform.forward * 0.32f + Vector3.up * 0.02f;
            GameEvents.RaisePlayerCaught();
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                cam.position = Vector3.Lerp(camStart, closePos, t / 0.18f);
                cam.rotation = Quaternion.LookRotation((head.position - cam.position).normalized);
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
            if (DeathScreenManager.Instance != null) DeathScreenManager.Instance.TriggerJumpscareSequence();
        }

        private void OnRespawned()
        {
            killing = false;
            if (brain == null) return;
            // Về tuần tra ở điểm xa chỗ hồi sinh nhất (SpawnPoint do CheckpointManager dời theo checkpoint)
            if (waypoints != null && waypoints.Length > 0 && player != null)
            {
                var sp = GameObject.Find("SpawnPoint");
                var refPos = sp != null ? sp.transform.position : player.transform.position;
                int far = 0; float fd = -1f;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    float d = Vector3.Distance(waypoints[i].position, refPos);
                    if (d > fd) { fd = d; far = i; }
                }
                agent.Warp(waypoints[far].position);
                wpIndex = far;
            }
            brain.ResetToPatrol();
            if (GameManager.Instance != null) GameManager.Instance.Sanity = Mathf.Max(GameManager.Instance.Sanity, 50f);
        }

        // =====================================================================
        private void UpdateAudio()
        {
            if (keysSource != null) keysSource.volume = Mathf.Lerp(keysSource.volume, (agent.velocity.magnitude > 0.2f ? 1f : 0.25f) * AudioMix.Sfx, 4f * Time.deltaTime);
            if (voiceSource != null) voiceSource.volume = 0.55f * AudioMix.Voice;
            if (breathSource != null) breathSource.volume = (brain.State == HousekeeperState.Hunt ? 1f : 0.7f) * AudioMix.Sfx;
            if (huntMusic != null) huntMusic.volume = Mathf.MoveTowards(huntMusic.volume, (brain.State == HousekeeperState.Hunt ? 0.9f : 0f) * AudioMix.Music, Time.deltaTime * (brain.State == HousekeeperState.Hunt ? 2f : 0.5f));
            if (brain.State == HousekeeperState.Patrol && Time.time >= nextLullaby)
            {
                nextLullaby = Time.time + Random.Range(config.lullabyInterval.x, config.lullabyInterval.y);
                if (lullabyClip != null && voiceSource != null) voiceSource.PlayOneShot(lullabyClip, 0.8f);
                if (player != null && Vector3.Distance(player.transform.position, transform.position) < 14f)
                    KhoaHonHUD.Subtitle(T.Get("sub.hk_lullaby"), 5f);
            }
        }

        private void UpdateAnim()
        {
            if (animator == null) return;
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        private void Say(AudioClip clip, string subtitleKey)
        {
            if (clip != null && voiceSource != null) voiceSource.PlayOneShot(clip, 1f);
            if (!string.IsNullOrEmpty(subtitleKey)) KhoaHonHUD.Subtitle(T.Get(subtitleKey), 2.2f);
        }

        // Lưu: chỉ cần cờ chế độ yếu (suy ra từ Relic_Tho)
        public void Capture(SaveData data) { }
        public void Restore(SaveData data)
        {
            if (brain != null && GameManager.Instance != null) brain.SetWeakened(GameManager.Instance.Has(StoryFlag.Relic_Tho));
        }

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            var eye = head != null ? head.position : transform.position + Vector3.up * 1.6f;
            var l = Quaternion.Euler(0, -config.fieldOfView / 2f, 0) * transform.forward;
            var r = Quaternion.Euler(0, config.fieldOfView / 2f, 0) * transform.forward;
            Gizmos.DrawRay(eye, l * config.sightRangeDark); Gizmos.DrawRay(eye, r * config.sightRangeDark);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.4f);
            if (waypoints != null) for (int i = 0; i < waypoints.Length; i++) if (waypoints[i] != null) { Gizmos.DrawSphere(waypoints[i].position, 0.2f); if (i > 0 && waypoints[i - 1] != null) Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position); }
            if (hasInvestigate) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(investigatePos, 0.4f); }
        }
    }
}
