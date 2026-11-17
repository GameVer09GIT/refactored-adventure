using System.Collections;
using System.Collections.Generic;
using Blackwood.Inventory;
using Blackwood.Core;
using Blackwood.DebugTools;
using Blackwood.Items;
using UnityEngine;
using UnityEngine.AI;

namespace Blackwood.AI
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
        [Tooltip("Tiếng rít khi bị đèn pin rọi vào mặt (loop)")]
        [SerializeField] private AudioClip burnHissLoop;
        [Tooltip("Tiếng thét khi bị chói mắt đến choáng")]
        [SerializeField] private AudioClip blindShriekClip;

        public HousekeeperState State => brain != null ? brain.State : HousekeeperState.Patrol;
        public bool Weakened => brain != null && brain.Weakened;
        public HousekeeperBrain Brain => brain;

        // ---- Chống lại bằng đèn pin (đọc bởi HUD / hình ảnh / hậu kỳ)
        /// <summary>Thanh chói mắt 0..1.</summary>
        public float LightExposure01 => exposure != null ? exposure.Value : 0f;
        /// <summary>Đang nhờn đèn (rọi vô dụng).</summary>
        public bool LightImmune => exposure != null && exposure.Immune;
        /// <summary>Tia đèn pin đang trúng mặt bà trong tầm làm chói.</summary>
        public bool BeingLit { get; private set; }
        /// <summary>Đang choáng vì chói mắt (khác choáng do flash / lược).</summary>
        public bool BlindedStun => brain != null && brain.State == HousekeeperState.Stunned && brain.Stun == StunCause.Light;
        /// <summary>Vị trí nguồn sáng (để giơ tay che mặt đúng hướng).</summary>
        public Vector3 LightSourcePos => beamOrigin != null ? beamOrigin.position : (playerCam != null ? playerCam.position : transform.position + transform.forward);
        /// <summary>Bà đang nhìn thấy người chơi (để quay đầu theo).</summary>
        public bool SeesPlayer => perception.seesPlayer;
        public Vector3 PlayerEyePos => playerCam != null ? playerCam.position : transform.position + transform.forward * 2f + Vector3.up * 1.6f;
        /// <summary>Tốc độ phát animation để bước chân khớp tốc độ di chuyển.</summary>
        public float LocomotionPlayback { get; private set; } = 1f;
        public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

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
        private LightExposure exposure;
        private Transform beamOrigin;      // đèn Spot của đèn pin (chỉ đọc hướng, không sửa)
        private float lightSlow = 1f;
        private float blindUntil;
        private AudioSource hissSource;
        private float staggerLeft;
        private static bool lightHintShown;
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
            RebuildExposure();
            player = FindFirstObjectByType<PlayerController>();
            if (player != null) playerCam = player.playerCamera;
            int area = NavMesh.GetAreaFromName("Floor2");
            floor2Mask = area >= 0 ? 1 << area : NavMesh.AllAreas;
            agent.areaMask = floor2Mask;
            agent.speed = config.patrolSpeed;
            agent.angularSpeed = 300f;
            agent.acceleration = 9f;
            agent.stoppingDistance = 0.4f;
            agent.autoBraking = true;
            // Tự xoay người mượt (SmoothTurn) thay vì để agent bẻ góc tức thời
            agent.updateRotation = false;
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
            if (burnHissLoop != null && voiceSource != null)
            {
                foreach (var a in voiceSource.GetComponents<AudioSource>()) if (a != voiceSource) { hissSource = a; break; }
                if (hissSource == null) hissSource = voiceSource.gameObject.AddComponent<AudioSource>();
                hissSource.clip = burnHissLoop; hissSource.loop = true; hissSource.playOnAwake = false; hissSource.volume = 0f;
                hissSource.spatialBlend = 1f; hissSource.rolloffMode = AudioRolloffMode.Logarithmic;
                hissSource.minDistance = 1.5f; hissSource.maxDistance = 18f; hissSource.dopplerLevel = 0f;
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
                if (hissSource != null) hissSource.volume = 0f;
                BeingLit = false;
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
            UpdateLightExposure();
            brain.Tick(Time.deltaTime, p);
            if (p.seesPlayer) lastSeenPos = player.transform.position;
            Act();
            SmoothTurn();
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
                    agent.isStopped = false; agent.speed = config.suspiciousSpeed * lightSlow;
                    if (hasInvestigate) { SetDest(investigatePos); hasInvestigate = false; lookAroundT = 0f; }
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
                    {
                        lookAroundT += Time.deltaTime;
                        transform.rotation *= Quaternion.Euler(0, Mathf.Sin(lookAroundT * 1.6f) * 90f * Time.deltaTime, 0);
                    }
                    break;

                case HousekeeperState.Hunt:
                    float run = player.runSpeed;
                    agent.speed = run * (brain.Weakened ? config.weakenedHuntMultiplier : config.huntSpeedMultiplier) * Difficulty.HuntSpeed * lightSlow;
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
                    if (brain.Stun == StunCause.Light)
                    {
                        // Chói mắt: loạng choạng lùi lại nửa giây đầu rồi đứng ôm mặt (tư thế do HousekeeperCreep/BodyIK lo)
                        if (staggerLeft > 0f && agent.isOnNavMesh)
                        {
                            staggerLeft -= Time.deltaTime;
                            agent.Move(-transform.forward * (1.5f * Mathf.Clamp01(staggerLeft / 0.6f)) * Time.deltaTime);
                        }
                    }
                    else if (visualRoot != null)
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
                    if (brain.Stun == StunCause.Light)
                    {
                        staggerLeft = 0.6f;
                        var shriek = blindShriekClip != null ? blindShriekClip : screamClip;
                        if (voiceSource != null && shriek != null) voiceSource.PlayOneShot(shriek, 1f);
                    }
                    else if (voiceSource != null && screamClip != null) voiceSource.PlayOneShot(screamClip, 1f);
                    break;
                case HousekeeperState.Hunt:
                    // Gợi ý (tối đa 2 lần): có thể rọi đèn pin vào mặt bà để tự vệ
                    if (!lightHintShown && FlashlightOn() && PlayerPrefs.GetInt("kh_hint_light", 0) < 2)
                    {
                        lightHintShown = true;
                        PlayerPrefs.SetInt("kh_hint_light", PlayerPrefs.GetInt("kh_hint_light", 0) + 1);
                        BlackwoodHUD.Toast(T.Get("hint.light_repel"));
                    }
                    break;
            }
            if (from == HousekeeperState.Stunned && wasLightStun) blindUntil = Time.time + config.lightBlindAfter;
            wasLightStun = to == HousekeeperState.Stunned && brain.Stun == StunCause.Light;
        }

        private bool wasLightStun;

        // =====================================================================
        // Chống lại bằng đèn pin: rọi thẳng vào mặt → chậm dần → chói mắt choáng → nhờn đèn một lúc
        private int exposureDifficulty = -1;

        /// <summary>Tạo lại thanh chói theo độ khó hiện tại (đổi độ khó giữa chừng có hiệu lực ngay).</summary>
        private void RebuildExposure()
        {
            exposureDifficulty = Difficulty.Level;
            exposure = new LightExposure(config.lightFillTime * Difficulty.LightFillTime, config.lightDecayTime,
                                         config.lightImmuneTime * Difficulty.LightImmuneTime);
        }

        private void UpdateLightExposure()
        {
            if (exposureDifficulty != Difficulty.Level && !exposure.Immune && exposure.Value <= 0f) RebuildExposure();
            var st = brain.State;
            bool can = st != HousekeeperState.Stunned && st != HousekeeperState.Kill && HidingSpot.Current == null;
            BeingLit = can && BeamOnFace(config.lightStunRange, config.lightStunHalfAngle);
            bool burning = BeingLit && !exposure.Immune;
            if (burning && BatteryPool.Instance != null) BatteryPool.Instance.Drain(CellType.D, config.lightBatteryDrainPerSecond * Time.deltaTime);
            float target = exposure.SpeedFactor(BeingLit, config.lightSlowMultiplier);
            lightSlow = Mathf.MoveTowards(lightSlow, target, Time.deltaTime * (target < lightSlow ? 2.5f : 0.9f));
            if (exposure.Tick(Time.deltaTime, BeingLit))
            {
                brain.OnBlinded();
                lightSlow = 1f;
                Blackwood.Horror.HorrorPostFX.Instance?.Flash(0.22f);
                AchievementService.Unlock("first_blind");
            }
            if (hissSource != null)
            {
                float hv = burning ? Mathf.Lerp(0.35f, 1f, exposure.Value) * AudioMix.Sfx : 0f;
                hissSource.volume = Mathf.MoveTowards(hissSource.volume, hv, Time.deltaTime * 3f);
                hissSource.pitch = 0.9f + exposure.Value * 0.35f;
                if (hissSource.volume > 0.01f && !hissSource.isPlaying) hissSource.Play();
                else if (hissSource.volume <= 0.01f && hissSource.isPlaying) hissSource.Stop();
            }
        }

        /// <summary>Tia đèn pin (hướng thật của bóng đèn, không phải hướng camera) có trúng mặt bà không.</summary>
        private bool BeamOnFace(float range, float halfAngle)
        {
            if (!FlashlightOn() || head == null) return false;
            if (beamOrigin == null)
            {
                var l = flashlight.GetComponentInChildren<Light>(true);
                beamOrigin = l != null ? l.transform : playerCam;
            }
            var face = head.position;
            var to = face - beamOrigin.position;
            float dist = to.magnitude;
            if (dist > range) return false;
            // Ở gần, mặt chiếm góc lớn hơn → nới góc nhắm để không quá khó
            float allow = halfAngle + Mathf.Atan2(0.22f, Mathf.Max(0.3f, dist)) * Mathf.Rad2Deg;
            if (Vector3.Angle(beamOrigin.forward, to) > allow) return false;
            // Phải rọi từ phía trước mặt (bà quay lưng thì không chói)
            var flat = -to; flat.y = 0;
            var fwd = transform.forward; fwd.y = 0;
            if (Vector3.Angle(fwd, flat) > 95f) return false;
            return HasLineOfSight(face, beamOrigin.position);
        }

        /// <summary>Xoay người mượt theo hướng di chuyển (thay cho xoay tức thời của NavMeshAgent).</summary>
        private void SmoothTurn()
        {
            if (agent.isStopped) return;
            var v = agent.desiredVelocity; v.y = 0;
            if (v.sqrMagnitude < 0.04f) return;
            bool fast = brain.State == HousekeeperState.Hunt;
            float maxDeg = (fast ? config.turnSpeed.y : config.turnSpeed.x) * Time.deltaTime;
            var want = Quaternion.LookRotation(v);
            // Giảm tốc khi phải quay gắt (người thật không trượt ngang khi đổi hướng)
            float ang = Quaternion.Angle(transform.rotation, want);
            if (ang > 70f) agent.velocity = Vector3.Lerp(agent.velocity, Vector3.zero, Time.deltaTime * 4f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, maxDeg);
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
            float range = (FlashlightOn() ? config.sightRangeLit : config.sightRangeDark) * Difficulty.SightRange;
            if (Time.time < blindUntil) range = Mathf.Min(range, 2.5f); // còn lóa mắt sau khi bị rọi choáng
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
            exposure?.Reset(); lightSlow = 1f; blindUntil = 0f; BeingLit = false;
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
                    BlackwoodHUD.Subtitle(T.Get("sub.hk_lullaby"), 5f);
            }
        }

        private void UpdateAnim()
        {
            if (animator == null) return;
            // Khớp bước chân với tốc độ thật: dưới tốc độ clip đi → phát chậm clip đi (không trộn với đứng yên gây trượt chân);
            // trên tốc độ clip chạy → phát nhanh clip chạy.
            float v = agent.velocity.magnitude;
            float param = v, play = 1f;
            float walkV = Mathf.Max(0.3f, config.walkClipSpeed), runV = Mathf.Max(walkV + 0.5f, config.runClipSpeed);
            if (v > 0.35f && v < walkV) { param = walkV; play = Mathf.Clamp(v / walkV, 0.55f, 1f); }
            else if (v > runV) { param = runV; play = Mathf.Clamp(v / runV, 1f, 1.4f); }
            animator.SetFloat("Speed", param, 0.16f, Time.deltaTime);
            LocomotionPlayback = Mathf.Lerp(LocomotionPlayback, play, Time.deltaTime * 6f);
        }

        private void Say(AudioClip clip, string subtitleKey)
        {
            if (clip != null && voiceSource != null) voiceSource.PlayOneShot(clip, 1f);
            if (!string.IsNullOrEmpty(subtitleKey)) BlackwoodHUD.Subtitle(T.Get(subtitleKey), 2.2f);
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
