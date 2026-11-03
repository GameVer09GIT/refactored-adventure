using System.Collections;
using UnityEngine;

namespace HorrorAudio
{
    /// <summary>
    /// Quản lý tiếng tim đập hồi hộp, đập nhanh và to dần khi người chơi ở gần con ma (GhostAI).
    /// Nhận diện ma theo sqrMagnitude, làm mới danh sách ma mỗi 2 giây, hỗ trợ kiểm tra vật cản (occlusion),
    /// điều chỉnh BPM 60-160, làm mượt bằng SmoothDamp và vẽ Gizmos trực quan.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHeartbeat : MonoBehaviour
    {
        [Header("AudioSource")]
        [Tooltip("AudioSource 2D chuyên phát tiếng tim đập (Priority cao 0-32, spatialBlend 0)")]
        [SerializeField] private AudioSource heartbeatSource;

        [Header("Audio Clip")]
        [Tooltip("Clip tiếng tim đập thình-thịch (độ dài tối đa <= 0.30s)")]
        [SerializeField] private AudioClip heartbeatClip;

        [Header("Khoảng cách Cảm ứng")]
        [Tooltip("Khoảng cách tối đa bắt đầu đập nhẹ (mét)")]
        [SerializeField] private float maxDistance = 20.0f;

        [Tooltip("Khoảng cách tối thiểu đạt độ căng thẳng tối đa (mét)")]
        [SerializeField] private float minDistance = 3.0f;

        [Tooltip("Đường cong uốn nắn mức độ căng thẳng (Tension curve) theo khoảng cách")]
        [SerializeField] private AnimationCurve tensionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Nhịp Tim (BPM) & Âm lượng")]
        [Tooltip("Nhịp tim tối thiểu khi bình tĩnh (BPM)")]
        [SerializeField] private float minBPM = 60.0f;

        [Tooltip("Nhịp tim tối đa khi hoảng loạn tột độ (BPM)")]
        [SerializeField] private float maxBPM = 160.0f;

        [Tooltip("Mức độ tăng thêm căng thẳng khi ma đang ở trạng thái Chase (+0.15)")]
        [SerializeField] private float chaseBonus = 0.15f;

        [Tooltip("Thời gian làm mượt SmoothDamp (giây)")]
        [SerializeField] private float smoothTime = 0.35f;

        [Header("Kiểm tra Vật cản (Occlusion)")]
        [Tooltip("Bật/Tắt tính năng giảm nhịp tim khi bị tường/vật cản che khuất")]
        [SerializeField] private bool useOcclusionCheck = true;

        [Tooltip("Hệ số nhân khi bị vật cản che (mặc định 0.7 = giảm 30%)")]
        [Range(0.1f, 1f)][SerializeField] private float occlusionMultiplier = 0.7f;

        [Tooltip("LayerMask quét vật cản che khuất tầm nghe/nhìn")]
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Tùy chọn Bổ sung")]
        [Tooltip("Chạy nhanh tăng thêm nhịp tim nhẹ (mặc định tắt)")]
        [SerializeField] private bool sprintAffectsHeartbeat = false;

        [Tooltip("Mức tăng nhịp tim khi chạy nhanh")]
        [SerializeField] private float sprintBonus = 0.10f;

        [Tooltip("Chu kỳ tìm kiếm và làm mới danh sách ma (giây)")]
        [SerializeField] private float ghostRefreshInterval = 2.0f;

        private PlayerController playerController;
        private GhostAI[] cachedGhosts;
        private float ghostRefreshTimer = 0f;

        private float targetT = 0f;
        private float smoothedT = 0f;
        private float tVelocity = 0f;

        private float targetBPM = 60f;
        private float smoothedBPM = 60f;
        private float bpmVelocity = 0f;

        private float targetVolume = 0f;
        private float smoothedVolume = 0f;
        private float volVelocity = 0f;

        private float beatTimer = 0f;
        private bool isPlayerDead = false;

        void Awake()
        {
            playerController = GetComponent<PlayerController>();

            if (heartbeatSource == null)
            {
                Transform child = transform.Find("HeartbeatAudio");
                if (child != null) heartbeatSource = child.GetComponent<AudioSource>();
                if (heartbeatSource == null)
                {
                    GameObject go = new GameObject("HeartbeatAudio");
                    go.transform.SetParent(transform, false);
                    heartbeatSource = go.AddComponent<AudioSource>();
                }
            }

            if (heartbeatSource != null)
            {
                heartbeatSource.spatialBlend = 0f; // 2D Sound
                heartbeatSource.priority = 16;      // Ưu tiên cao (0 - 32)
                heartbeatSource.playOnAwake = false;
                heartbeatSource.loop = false;
            }

            TryAutoLoadClip();
        }

        public void TryAutoLoadClip()
        {
            if (heartbeatClip == null)
            {
                heartbeatClip = Resources.Load<AudioClip>("Audio/Heartbeat/heartbeat");
#if UNITY_EDITOR
                if (heartbeatClip == null)
                    heartbeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Heartbeat/heartbeat.wav");
#endif
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

        void Start()
        {
            RefreshGhostList();
            smoothedBPM = minBPM;
            smoothedVolume = 0f;
            smoothedT = 0f;
        }

        void Update()
        {
            if (isPlayerDead || Time.timeScale <= 0f) return;

            // Làm mới danh sách ma định kỳ mỗi 2 giây, tuyệt đối không gọi FindObjectsByType mỗi frame
            ghostRefreshTimer += Time.deltaTime;
            if (ghostRefreshTimer >= ghostRefreshInterval)
            {
                ghostRefreshTimer = 0f;
                RefreshGhostList();
            }

            // Tính toán mức độ căng thẳng dựa trên con ma gần nhất
            CalculateTension();

            // Làm mượt t, BPM và Volume bằng SmoothDamp
            smoothedT = Mathf.SmoothDamp(smoothedT, targetT, ref tVelocity, smoothTime);
            smoothedBPM = Mathf.SmoothDamp(smoothedBPM, targetBPM, ref bpmVelocity, smoothTime);
            smoothedVolume = Mathf.SmoothDamp(smoothedVolume, targetVolume, ref volVelocity, smoothTime);

            // Xử lý chu kỳ nhịp đập
            UpdateBeatPlayback();
        }

        /// <summary>
        /// Làm mới mảng danh sách GhostAI trong scene
        /// </summary>
        private void RefreshGhostList()
        {
            cachedGhosts = Object.FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
        }

        /// <summary>
        /// Tìm con ma gần nhất và tính toán t (0 - 1), BPM và Target Volume
        /// </summary>
        private void CalculateTension()
        {
            float minSqrDist;
            GhostAI nearestGhost = GetNearestGhost(out minSqrDist);

            if (nearestGhost == null)
            {
                targetT = 0f;
                targetBPM = minBPM;
                targetVolume = 0f;
                return;
            }

            float distance = Mathf.Sqrt(minSqrDist);

            // Chuẩn hóa khoảng cách từ 20m (t=0) về 3m (t=1)
            float rawT = Mathf.InverseLerp(maxDistance, minDistance, distance);

            // Làm cong bằng tensionCurve
            float t = tensionCurve.Evaluate(rawT);

            // Cộng thêm điểm căng thẳng nếu ma đang ở trạng thái rượt đuổi (Chase)
            if (nearestGhost.CurrentState == GhostAI.GhostState.Chase)
            {
                t += chaseBonus;
            }

            // Kiểm tra vật cản che khuất giữa Player và ma
            if (useOcclusionCheck && CheckOcclusion(nearestGhost))
            {
                t *= occlusionMultiplier;
            }

            // Tùy chọn chạy nhanh tăng thêm nhịp tim
            if (sprintAffectsHeartbeat && playerController != null && playerController.IsRunning)
            {
                t += sprintBonus;
            }

            targetT = Mathf.Clamp01(t);

            // Tính nhịp tim BPM và âm lượng
            targetBPM = Mathf.Lerp(minBPM, maxBPM, targetT);

            if (targetT < 0.02f)
            {
                targetVolume = 0f;
            }
            else
            {
                // Độ mở âm lượng dạng phi tuyến (t^1.5)
                targetVolume = Mathf.Lerp(0f, 1.0f, Mathf.Pow(targetT, 1.5f));
            }
        }

        /// <summary>
        /// Tìm con ma gần nhất bằng sqrMagnitude để tránh tính căn bậc hai không cần thiết
        /// </summary>
        private GhostAI GetNearestGhost(out float minSqrDist)
        {
            minSqrDist = float.MaxValue;
            if (cachedGhosts == null || cachedGhosts.Length == 0) return null;

            Vector3 playerPos = transform.position;
            GhostAI nearest = null;

            for (int i = 0; i < cachedGhosts.Length; i++)
            {
                GhostAI g = cachedGhosts[i];
                if (g == null || !g.isActiveAndEnabled || g.IsPlayerDead) continue;

                float sqrDist = (g.transform.position - playerPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = g;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Bắn tia Raycast kiểm tra vật cản giữa Player và ma
        /// </summary>
        private bool CheckOcclusion(GhostAI ghost)
        {
            if (ghost == null) return false;

            Vector3 startPos = playerController != null && playerController.playerCamera != null
                ? playerController.playerCamera.position
                : transform.position + Vector3.up * 1.6f;

            Vector3 endPos = ghost.transform.position + Vector3.up * 1.4f;
            Vector3 dir = endPos - startPos;
            float dist = dir.magnitude;

            if (dist < 0.5f) return false;

            RaycastHit hit;
            if (Physics.Raycast(startPos, dir.normalized, out hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Nếu tia chạm trúng vật không phải ma và không phải người chơi -> có vật cản
                if (hit.collider.transform != ghost.transform &&
                    !hit.collider.transform.IsChildOf(ghost.transform) &&
                    hit.collider.transform != transform &&
                    !hit.collider.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Quản lý thời gian phát nhịp tim
        /// </summary>
        private void UpdateBeatPlayback()
        {
            if (smoothedVolume < 0.02f || heartbeatClip == null || heartbeatSource == null)
            {
                beatTimer = 0f;
                return;
            }

            float beatInterval = 60.0f / Mathf.Max(30f, smoothedBPM);
            beatTimer += Time.deltaTime;

            if (beatTimer >= beatInterval)
            {
                beatTimer -= beatInterval;

                // Tăng nhẹ pitch khi nhịp tim dồn dập
                heartbeatSource.pitch = Mathf.Lerp(0.96f, 1.10f, smoothedT);
                heartbeatSource.PlayOneShot(heartbeatClip, Mathf.Clamp01(smoothedVolume));
            }
        }

        private void HandlePlayerDied()
        {
            isPlayerDead = true;
            targetVolume = 0f;
            StartCoroutine(FadeOutHeartbeat(0.3f));
        }

        private void HandlePlayerRespawned()
        {
            isPlayerDead = false;
            targetT = 0f;
            smoothedT = 0f;
            tVelocity = 0f;
            targetBPM = minBPM;
            smoothedBPM = minBPM;
            bpmVelocity = 0f;
            targetVolume = 0f;
            smoothedVolume = 0f;
            volVelocity = 0f;
            beatTimer = 0f;

            RefreshGhostList();
        }

        private IEnumerator FadeOutHeartbeat(float duration)
        {
            if (heartbeatSource == null) yield break;

            float startVol = smoothedVolume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                smoothedVolume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            smoothedVolume = 0f;
            targetVolume = 0f;
            beatTimer = 0f;
        }

        void OnDrawGizmosSelected()
        {
            // Vẽ phạm vi tim đập trực quan trong Scene View
            Vector3 center = transform.position + Vector3.up * 0.9f;

            // Vòng vàng 20m: Bắt đầu cảm ứng nhịp tim
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.8f);
            Gizmos.DrawWireSphere(center, maxDistance);

            // Vòng đỏ 3m: Nguy hiểm tột độ, nhịp tim tối đa 160 BPM
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.85f);
            Gizmos.DrawWireSphere(center, minDistance);
        }
    }
}
