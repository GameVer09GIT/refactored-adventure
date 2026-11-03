using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Máy trạng thái và trí tuệ nhân tạo (AI) cho con ma rượt đuổi người chơi.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GhostAI : MonoBehaviour
{
    public static GhostAI Instance { get; private set; }

    public enum GhostState
    {
        Patrol, // Tuần tra ngẫu nhiên
        Chase,  // Đang rượt đuổi người chơi
        Lost,   // Mất dấu người chơi, đến vị trí thấy lần cuối
        Kill    // Đã bắt được người chơi
    }

    [Header("Trạng thái hiện tại")]
    [Tooltip("Trạng thái hiện tại của con ma (chỉ đọc)")]
    [SerializeField] private GhostState currentState = GhostState.Patrol;

    [Header("Tham chiếu mục tiêu")]
    [Tooltip("Tham chiếu tới script PlayerController")]
    [SerializeField] private PlayerController player;

    [Tooltip("Transform đầu/camera của Player để kiểm tra Line of Sight và Jumpscare")]
    [SerializeField] private Transform playerHead;

    [Tooltip("LayerMask các vật cản che khuất tầm nhìn (tường, cửa, địa hình...)")]
    [SerializeField] private LayerMask obstacleMask = ~0; // Mặc định tất cả layer trừ layer của ma

    [Header("Cấu hình Tốc độ (tính từ Player)")]
    [Tooltip("Tự động tính tốc độ ma dựa trên tốc độ đi bộ và chạy nhanh của Player")]
    [SerializeField] private bool autoSpeedFromPlayer = true;

    [Tooltip("Hệ số độ khó tốc độ rượt: chaseSpeed = walk + factor * (sprint - walk). Mặc định 0.6")]
    [Range(0.1f, 1.2f)]
    [SerializeField] private float speedFactor = 0.6f;

    [Tooltip("Tốc độ rượt thủ công nếu tắt autoSpeedFromPlayer")]
    [SerializeField] private float manualChaseSpeed = 5.3f;

    [Tooltip("Tỷ lệ tốc độ tuần tra so với tốc độ đi bộ của người chơi (40% - 50%)")]
    [Range(0.2f, 0.8f)]
    [SerializeField] private float patrolSpeedFactor = 0.45f;

    [Tooltip("Thời gian tăng tốc dần khi chuyển từ Tuần tra sang Rượt đuổi (giây)")]
    [SerializeField] private float speedTransitionDuration = 0.5f;

    [Header("Phạm vi & Tầm nhìn")]
    [Tooltip("Khoảng cách phát hiện người chơi (mét)")]
    [SerializeField] private float detectRange = 12.0f;

    [Tooltip("Khoảng cách bỏ cuộc nếu người chơi chạy quá xa (mét)")]
    [SerializeField] private float loseRange = 25.0f;

    [Tooltip("Khoảng cách bắt được người chơi (mét)")]
    [SerializeField] private float killRange = 1.5f;

    [Tooltip("Bán kính luôn nghe thấy tiếng người chơi dù không nhìn thấy (mét)")]
    [SerializeField] private float hearRange = 3.0f;

    [Tooltip("Góc nhìn phát hiện phía trước khi tuần tra (độ)")]
    [Range(60f, 220f)]
    [SerializeField] private float fieldOfView = 140.0f;

    [Tooltip("Thời gian mất dấu liên tục trước khi bỏ rượt (giây)")]
    [SerializeField] private float loseSightTime = 5.0f;

    [Header("Tuần tra (Patrol)")]
    [Tooltip("Bán kính tìm điểm tuần tra quanh vị trí hiện tại")]
    [SerializeField] private float patrolRadius = 20.0f;

    [Tooltip("Thời gian dừng nghỉ tối thiểu tại mỗi điểm tuần tra")]
    [SerializeField] private float patrolWaitMin = 2.0f;

    [Tooltip("Thời gian dừng nghỉ tối đa tại mỗi điểm tuần tra")]
    [SerializeField] private float patrolWaitMax = 4.0f;

    [Tooltip("Chu kỳ kiểm tra tầm nhìn phát hiện (giây) để tối ưu hiệu năng")]
    [SerializeField] private float detectionInterval = 0.15f;

    [Header("Hồi sinh & Miễn nhiễm")]
    [Tooltip("Thời gian người chơi được miễn nhiễm sau khi hồi sinh (giây)")]
    [SerializeField] private float spawnImmunity = 4.0f;

    [Tooltip("Khoảng cách tối thiểu khi ma xuất hiện lại so với Player (mét)")]
    [SerializeField] private float respawnMinDistance = 28.0f;

    [Tooltip("Khoảng cách tối đa khi ma xuất hiện lại so với Player (mét)")]
    [SerializeField] private float respawnMaxDistance = 40.0f;

    [Header("Jumpscare khi bắt người chơi")]
#pragma warning disable CS0414
    [Tooltip("Tự động xoay camera người chơi nhìn vào mặt ma khi bị bắt")]
    [SerializeField] private bool jumpscareTurn = true;

    [Tooltip("Thời gian xoay camera về phía ma (giây)")]
    [SerializeField] private float jumpscareTurnDuration = 0.4f;

    [Tooltip("Thời gian dừng nhìn mặt ma trước khi hiện màn hình chết (giây)")]
    [SerializeField] private float jumpscareWaitDuration = 1.0f;
#pragma warning restore CS0414

    [Header("Âm thanh 3D")]
    [Tooltip("AudioSource phát âm thanh của ma")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Tiếng lướt/rượt đuổi")]
    [SerializeField] private AudioClip chaseAudioClip;

    [Tooltip("Tiếng rên rỉ/thở khi tuần tra ở gần")]
    [SerializeField] private AudioClip ambientAudioClip;

    [Tooltip("Tiếng thét jumpscare khi túm được người chơi")]
    [SerializeField] private AudioClip jumpscareAudioClip;

    // Các biến nội bộ
    private NavMeshAgent agent;
    private float calculatedChaseSpeed;
    private float calculatedPatrolSpeed;
    private float targetSpeed;
    private float speedTransitionVelocity;

    private float detectionTimer = 0f;
    private float loseSightTimer = 0f;
    private float patrolWaitTimer = 0f;
    private float immunityTimer = 0f;
    private bool isPlayerDead = false;
    private Vector3 lastSeenPosition;
    private bool isWaitingAtPatrolPoint = false;

    // Event thông báo khi bắt được người chơi
    public System.Action OnPlayerCaught;

    public GhostState CurrentState => currentState;
    public bool IsPlayerDead => isPlayerDead;

    void Awake()
    {
        Instance = this;
        agent = GetComponent<NavMeshAgent>();

        // Đảm bảo con ma luôn được gắn vật liệu URP chuẩn, không bao giờ bị màu hồng
        if (GetComponent<GhostMaterialFixer>() == null)
        {
            gameObject.AddComponent<GhostMaterialFixer>();
        }

        // Đảm bảo con ma luôn có âm thanh ghê rợn rên rỉ và hét rùng rợn khi người chơi lại gần
        if (GetComponent<GhostAmbientSound>() == null)
        {
            gameObject.AddComponent<GhostAmbientSound>();
        }

        // Tự động tìm Player nếu chưa được kéo thả
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player != null && playerHead == null)
        {
            playerHead = player.playerCamera != null ? player.playerCamera : player.transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Đảm bảo cấu hình âm thanh 3D
        if (audioSource != null)
        {
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 25.0f;
        }
    }

    void Start()
    {
        ConfigureAgentSettings();
        CalculateSpeeds();

        targetSpeed = calculatedPatrolSpeed;
        agent.speed = calculatedPatrolSpeed;

        // Bắt đầu ở trạng thái tuần tra
        EnterPatrolState();
    }

    void Update()
    {
        if (isPlayerDead) return;

        // Đếm ngược thời gian miễn nhiễm sau khi hồi sinh
        if (immunityTimer > 0f)
        {
            immunityTimer -= Time.deltaTime;
        }

        // Tăng/giảm tốc độ mượt mà khi chuyển trạng thái
        SmoothSpeedTransition();

        // Kiểm tra phát hiện mục tiêu theo chu kỳ tối ưu
        detectionTimer += Time.deltaTime;
        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            EvaluateDetection();
        }

        // Xử lý logic theo từng trạng thái
        switch (currentState)
        {
            case GhostState.Patrol:
                HandlePatrol();
                break;
            case GhostState.Chase:
                HandleChase();
                break;
            case GhostState.Lost:
                HandleLost();
                break;
        }
    }

    /// <summary>
    /// Cấu hình thông số chuẩn cho NavMeshAgent
    /// </summary>
    private void ConfigureAgentSettings()
    {
        if (agent == null) return;

        agent.acceleration = 12.0f;
        agent.angularSpeed = 240.0f;
        agent.stoppingDistance = Mathf.Max(0.5f, killRange * 0.7f); // Áp sát được người chơi
        agent.radius = 0.4f;
        agent.height = 2.0f;
    }

    /// <summary>
    /// Tính toán tốc độ tự động từ Player
    /// </summary>
    public void CalculateSpeeds()
    {
        if (player != null && autoSpeedFromPlayer)
        {
            float w = player.walkSpeed;
            float s = player.runSpeed;

            if (s <= w)
            {
                Debug.LogWarning("[GhostAI] Cảnh báo: Tốc độ chạy nhanh của Player (runSpeed) nhỏ hơn hoặc bằng đi bộ (walkSpeed)!");
            }

            calculatedChaseSpeed = w + speedFactor * (s - w);

            if (calculatedChaseSpeed >= s)
            {
                Debug.LogWarning("[GhostAI] Cảnh báo: chaseSpeed >= runSpeed. Ma sẽ luôn đuổi kịp người chơi dù đang chạy nhanh!");
            }

            calculatedPatrolSpeed = w * patrolSpeedFactor;
        }
        else
        {
            calculatedChaseSpeed = manualChaseSpeed;
            calculatedPatrolSpeed = manualChaseSpeed * patrolSpeedFactor;
        }
    }

    /// <summary>
    /// Chuyển đổi tốc độ mượt mà trong khoảng 0.5s
    /// </summary>
    private void SmoothSpeedTransition()
    {
        if (agent == null || !agent.enabled) return;

        agent.speed = Mathf.SmoothDamp(agent.speed, targetSpeed, ref speedTransitionVelocity, speedTransitionDuration);
    }

    /// <summary>
    /// Đánh giá tầm nhìn và khoảng cách tới Player
    /// </summary>
    private void EvaluateDetection()
    {
        if (player == null || immunityTimer > 0f) return;

        Vector3 ghostHeadPos = transform.position + Vector3.up * 1.65f;
        Vector3 targetPos = playerHead != null ? playerHead.position : (player.transform.position + Vector3.up * 1.6f);
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Kiểm tra Line of Sight (không xuyên tường)
        bool hasLineOfSight = CheckLineOfSight(ghostHeadPos, targetPos);

        // Góc nhìn phía trước
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        dirToPlayer.y = 0f;
        float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);

        if (currentState == GhostState.Patrol || currentState == GhostState.Lost)
        {
            // Điều kiện phát hiện:
            // 1. Trong bán kính nghe gần 3m: luôn phát hiện kể cả sau lưng
            // 2. Trong tầm detectRange, trong góc nhìn FOV, và có tầm nhìn thông suốt (không bị tường che)
            bool detectedByProximity = distanceToPlayer <= hearRange && hasLineOfSight;
            bool detectedByVision = distanceToPlayer <= detectRange && angleToPlayer <= (fieldOfView * 0.5f) && hasLineOfSight;

            if (detectedByProximity || detectedByVision)
            {
                EnterChaseState();
            }
        }
        else if (currentState == GhostState.Chase)
        {
            // Kiểm tra bắt được người chơi (Kill)
            if (distanceToPlayer <= killRange && hasLineOfSight)
            {
                EnterKillState();
                return;
            }

            // Kiểm tra người chơi chạy quá xa
            if (distanceToPlayer > loseRange)
            {
                EnterLostState();
                return;
            }

            // Cập nhật vị trí nhìn thấy cuối cùng và đếm mất dấu
            if (hasLineOfSight)
            {
                lastSeenPosition = player.transform.position;
                loseSightTimer = 0f;
            }
            else
            {
                loseSightTimer += detectionInterval;
                if (loseSightTimer >= loseSightTime)
                {
                    EnterLostState();
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra tia nhìn Raycast không bị cản bởi tường
    /// </summary>
    private bool CheckLineOfSight(Vector3 fromPos, Vector3 toPos)
    {
        Vector3 dir = toPos - fromPos;
        float dist = dir.magnitude;

        if (Physics.Raycast(fromPos, dir.normalized, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Nếu trúng chính Player hoặc con của Player thì coi như nhìn thấy
            if (hit.transform == player.transform || hit.transform.IsChildOf(player.transform))
            {
                return true;
            }
            // Bị vật cản che khuất
            return false;
        }

        // Không chạm vật cản nào
        return true;
    }

    #region State Handlers

    private void EnterPatrolState()
    {
        currentState = GhostState.Patrol;
        targetSpeed = calculatedPatrolSpeed;
        isWaitingAtPatrolPoint = false;
        patrolWaitTimer = 0f;

        PlaySound(ambientAudioClip, true);
        SetNewPatrolDestination();
    }

    private void HandlePatrol()
    {
        if (agent == null || !agent.enabled) return;

        // Nếu đã đến gần điểm tuần tra
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            if (!isWaitingAtPatrolPoint)
            {
                isWaitingAtPatrolPoint = true;
                patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
            }
            else
            {
                patrolWaitTimer -= Time.deltaTime;
                if (patrolWaitTimer <= 0f)
                {
                    isWaitingAtPatrolPoint = false;
                    SetNewPatrolDestination();
                }
            }
        }
    }

    private void SetNewPatrolDestination()
    {
        Vector3 randomPoint;
        if (GetRandomPointOnNavMesh(transform.position, patrolRadius, out randomPoint))
        {
            agent.isStopped = false;
            agent.SetDestination(randomPoint);
        }
    }

    private void EnterChaseState()
    {
        currentState = GhostState.Chase;
        targetSpeed = calculatedChaseSpeed;
        loseSightTimer = 0f;
        lastSeenPosition = player.transform.position;

        PlaySound(chaseAudioClip, true);
    }

    private void HandleChase()
    {
        if (agent == null || !agent.enabled || player == null) return;

        agent.isStopped = false;
        agent.SetDestination(player.transform.position);
    }

    private void EnterLostState()
    {
        currentState = GhostState.Lost;
        targetSpeed = calculatedPatrolSpeed;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(lastSeenPosition);
        }
    }

    private void HandleLost()
    {
        if (agent == null || !agent.enabled) return;

        // Khi đi đến vị trí nhìn thấy lần cuối mà vẫn không phát hiện lại
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.4f)
        {
            EnterPatrolState();
        }
    }

    private void EnterKillState()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;
        currentState = GhostState.Kill;

        // 1. Dừng con ma ngay lập tức
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 2. Vô hiệu hóa điều khiển và stamina của Player
        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        // 3. Gọi event OnPlayerCaught
        OnPlayerCaught?.Invoke();

        // 4. Phát âm thanh jumpscare
        PlaySound(jumpscareAudioClip, false);

        // 5. Bắt đầu chuỗi Jumpscare và hiện màn hình chết
        StartCoroutine(KillSequenceCoroutine());
    }

    private IEnumerator KillSequenceCoroutine()
    {
        // 1. Xoay camera người chơi hướng về mặt con ma chớp nhoáng (0.2s)
        if (jumpscareTurn && player != null && player.playerCamera != null)
        {
            Transform cam = player.playerCamera;
            Vector3 lookTarget = transform.position + Vector3.up * 1.7f;
            Quaternion startRot = cam.rotation;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - cam.position);

            float elapsed = 0f;
            float quickTurn = 0.2f;
            while (elapsed < quickTurn)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / quickTurn);
                cam.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            cam.rotation = targetRot;
        }

        // 2. Kích hoạt toàn màn hình tấm ảnh ma kinh dị (anh-ma-kinh-di-30) + tiếng hét to 3 giây
        DeathScreenManager ds = DeathScreenManager.Instance;
        if (ds == null) ds = FindFirstObjectByType<DeathScreenManager>();
        if (ds == null)
        {
            GameObject autoDs = new GameObject("DeathScreenManager_Auto");
            ds = autoDs.AddComponent<DeathScreenManager>();
        }

        ds.TriggerJumpscareSequence();
    }

    #endregion

    /// <summary>
    /// Hồi sinh lại con ma ở vị trí cách xa Player và ngoài tầm nhìn camera
    /// </summary>
    public void RespawnGhost(Vector3 playerPos, Camera playerCam)
    {
        isPlayerDead = false;
        immunityTimer = spawnImmunity;

        Vector3 spawnPos = FindSafeRespawnPosition(playerPos, playerCam);

        if (agent != null)
        {
            agent.Warp(spawnPos);
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }
        else
        {
            transform.position = spawnPos;
        }

        EnterPatrolState();
    }

    /// <summary>
    /// Tìm vị trí trên NavMesh cách xa Player (>= 28m) và nằm ngoài tầm nhìn camera
    /// </summary>
    private Vector3 FindSafeRespawnPosition(Vector3 playerPos, Camera playerCam)
    {
        const int angleSamples = 20;
        Vector3 bestCandidate = transform.position;
        float maxDistanceFound = 0f;

        for (int i = 0; i < angleSamples; i++)
        {
            float angle = i * (360f / angleSamples);
            float rad = angle * Mathf.Deg2Rad;
            float dist = Random.Range(respawnMinDistance, respawnMaxDistance);

            Vector3 offset = new Vector3(Mathf.Cos(rad) * dist, 0f, Mathf.Sin(rad) * dist);
            Vector3 testPos = playerPos + offset;

            if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 6.0f, NavMesh.AllAreas))
            {
                Vector3 candidate = hit.position;

                // Kiểm tra đường đi NavMesh có tới được Player không
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(candidate, playerPos, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    // Kiểm tra xem vị trí có nằm ngoài tầm nhìn camera không
                    bool isOutsideCamera = true;
                    if (playerCam != null)
                    {
                        Vector3 dirToGhost = (candidate - playerCam.transform.position).normalized;
                        float dot = Vector3.Dot(playerCam.transform.forward, dirToGhost);
                        
                        // Nếu góc nhìn hướng về phía ma (dot > 0.3) thì kiểm tra thêm có bị che khuất không
                        if (dot > 0.3f)
                        {
                            Vector3 camPos = playerCam.transform.position;
                            if (!Physics.Linecast(camPos, candidate + Vector3.up * 1.5f, obstacleMask))
                            {
                                isOutsideCamera = false; // Nhìn thấy trực tiếp trong tầm mắt
                            }
                        }
                    }

                    if (isOutsideCamera)
                    {
                        return candidate; // Tìm thấy vị trí hoàn hảo
                    }

                    float actualDist = Vector3.Distance(candidate, playerPos);
                    if (actualDist > maxDistanceFound)
                    {
                        maxDistanceFound = actualDist;
                        bestCandidate = candidate;
                    }
                }
            }
        }

        return bestCandidate;
    }

    /// <summary>
    /// Lấy điểm ngẫu nhiên trên NavMesh
    /// </summary>
    private bool GetRandomPointOnNavMesh(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += center;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = center;
        return false;
    }

    private void PlaySound(AudioClip clip, bool loop)
    {
        if (audioSource == null || clip == null) return;

        if (loop)
        {
            if (audioSource.clip != clip || !audioSource.isPlaying)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // Vàng: Tầm phát hiện nhìn thấy (detectRange)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, detectRange);

        // Đỏ: Tầm bắt chết ngay (killRange)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, killRange);

        // Xanh dương: Tầm bỏ cuộc (loseRange)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, loseRange);

        // Xanh lá: Tầm nghe gần (hearRange)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, hearRange);

        // Vẽ nón góc nhìn Field of View
        Vector3 forward = transform.forward;
        Quaternion leftRot = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f);
        Quaternion rightRot = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f);
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        Gizmos.DrawLine(pos, pos + leftRot * forward * detectRange);
        Gizmos.DrawLine(pos, pos + rightRot * forward * detectRange);
    }
}
