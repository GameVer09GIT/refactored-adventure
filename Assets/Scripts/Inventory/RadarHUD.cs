using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Giao diện Radar góc dưới phải màn hình (quét 360 độ, hiển thị vị trí ma tương đối, blip pool)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class RadarHUD : MonoBehaviour
    {
        public static RadarHUD Instance { get; private set; }

        [Header("Cấu trúc Radar")]
        [Tooltip("Gốc xoay kim quét (Sweep Hand)")]
        [SerializeField] private RectTransform sweepTransform;

        [Tooltip("Khu vực chứa các chấm blip")]
        [SerializeField] private RectTransform blipContainer;

        [Tooltip("Prefab hoặc đối tượng Blip trong pool")]
        [SerializeField] private Image[] blipPool = new Image[4];

        [Tooltip("Bán kính vùng quét trên UI (pixel, ví dụ ~90px với radar 220px)")]
        [SerializeField] private float radarRadiusPixels = 92f;

        [Header("Thông báo trạng thái")]
        [Tooltip("Dòng chữ hiển thị trạng thái (Không phát hiện / Đang nạp...)")]
        [SerializeField] private TMP_Text statusText;

        [Header("Cấu hình Quét")]
        [Tooltip("Phạm vi quét tối đa của Radar (mét)")]
        [SerializeField] private float scanRange = 35.0f;

        [Tooltip("Thời gian quét trọn 360 độ (giây)")]
        [SerializeField] private float sweepDuration = 1.5f;

        [Tooltip("Thời gian hồi giữa 2 lần quét (giây)")]
        [SerializeField] private float scanCooldown = 5.0f;

        [Tooltip("Thời gian mờ dần của chấm blip (giây)")]
        [SerializeField] private float blipFadeDuration = 4.0f;

        [Header("Âm thanh")]
        [Tooltip("Clip tiếng quét sóng radar")]
        [SerializeField] private AudioClip scanSound;

        [Tooltip("Clip tiếng bíp khi phát hiện ma")]
        [SerializeField] private AudioClip blipSound;

        [Tooltip("AudioSource phát âm thanh radar")]
        [SerializeField] private AudioSource audioSource;

        private CanvasGroup canvasGroup;
        private Coroutine fadeCoroutine;
        private Coroutine scanCoroutine;
        private Coroutine statusCoroutine;

        private float cooldownTimer = 0f;
        private bool isScanning = false;
        private Transform playerTransform;
        private Transform cameraTransform;

        // Dữ liệu blip đang hiển thị
        private class ActiveBlip
        {
            public Image image;
            public Vector2 normalizedDir;
            public float normalizedDist;
            public float angleDeg; // Góc so với hướng nhìn (0 = thẳng trước mặt)
            public float spawnTime;
            public bool triggered;
        }

        private readonly List<ActiveBlip> activeBlips = new List<ActiveBlip>(4);

        public float ScanRange => scanRange;
        public float CooldownRemaining => cooldownTimer;
        public bool IsScanning => isScanning;
        public bool CanScan => !isScanning && cooldownTimer <= 0f;

        void Awake()
        {
            Instance = this;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }

            // Ẩn tất cả blip trong pool
            if (blipPool != null)
            {
                foreach (var b in blipPool)
                {
                    if (b != null)
                    {
                        b.gameObject.SetActive(false);
                        b.color = new Color(0.2f, 1f, 0.2f, 0f);
                    }
                }
            }

            if (statusText != null) statusText.text = "";
        }

        public void BindPlayer(PlayerController player)
        {
            if (player == null) return;
            playerTransform = player.transform;
            cameraTransform = player.playerCamera != null ? player.playerCamera : player.transform;
        }

        public void ShowHUD(bool show)
        {
            if (!gameObject.activeInHierarchy)
            {
                if (canvasGroup != null) canvasGroup.alpha = show ? 1f : 0f;
                if (!show) CancelScan();
                return;
            }

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeHUD(show ? 1f : 0f, 0.2f));

            if (!show)
            {
                // Khi cất radar: dừng quét nếu đang quét dở
                CancelScan();
            }
        }

        private IEnumerator FadeHUD(float targetAlpha, float duration)
        {
            if (canvasGroup == null) yield break;
            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            fadeCoroutine = null;
        }

        void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    cooldownTimer = 0f;
                    if (!isScanning && statusText != null && statusText.text == "Đang nạp...")
                    {
                        statusText.text = "";
                    }
                }
            }

            // Cập nhật độ mờ của các blip đang hiện
            UpdateActiveBlips();
        }

        private void UpdateActiveBlips()
        {
            for (int i = activeBlips.Count - 1; i >= 0; i--)
            {
                var blip = activeBlips[i];
                if (!blip.triggered) continue;

                float age = Time.time - blip.spawnTime;
                if (age >= blipFadeDuration)
                {
                    if (blip.image != null) blip.image.gameObject.SetActive(false);
                    activeBlips.RemoveAt(i);
                }
                else
                {
                    float alpha = 1f - (age / blipFadeDuration);
                    if (blip.image != null)
                    {
                        blip.image.color = new Color(0.2f, 1f, 0.2f, alpha);
                    }
                }
            }
        }

        /// <summary>
        /// Kích hoạt lượt quét radar (360 độ trong 1.5s)
        /// </summary>
        public bool TriggerScan(System.Action<bool> onGhostDetected = null)
        {
            if (!CanScan) return false;

            if (scanCoroutine != null) StopCoroutine(scanCoroutine);
            scanCoroutine = StartCoroutine(PerformScanRoutine(onGhostDetected));
            return true;
        }

        private IEnumerator PerformScanRoutine(System.Action<bool> onGhostDetected)
        {
            isScanning = true;
            cooldownTimer = scanCooldown;

            if (statusText != null) statusText.text = "";

            // Phát âm thanh bắt đầu quét
            if (audioSource != null && scanSound != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(scanSound, 0.85f);
            }

            // 1. Thu thập danh sách ma MỘT LẦN duy nhất khi bắt đầu quét
            GhostAI[] ghosts = Object.FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
            activeBlips.Clear();

            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.001f) camForward = Vector3.forward;
            camForward.Normalize();

            int blipIndex = 0;
            int ghostsInRange = 0;

            if (ghosts != null && blipPool != null)
            {
                foreach (var g in ghosts)
                {
                    if (g == null || !g.gameObject.activeInHierarchy) continue;
                    if (blipIndex >= blipPool.Length) break;

                    Vector3 diff = g.transform.position - playerPos;
                    diff.y = 0f;
                    float dist = diff.magnitude;

                    if (dist <= scanRange)
                    {
                        ghostsInRange++;
                        float normalizedDist = Mathf.Clamp01(dist / scanRange);

                        // Tính góc tương đối so với hướng nhìn (0 độ = thẳng trước mặt, tăng theo chiều kim đồng hồ 0 -> 360)
                        float signedAngle = Vector3.SignedAngle(camForward, diff.normalized, Vector3.up);
                        float clockAngle = (signedAngle + 360f) % 360f;

                        // Tọa độ trên UI radar (2D)
                        // Phía trên là góc 0 độ: x = sin(angle), y = cos(angle)
                        float rad = clockAngle * Mathf.Deg2Rad;
                        Vector2 uiPos = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * (normalizedDist * radarRadiusPixels);

                        Image img = blipPool[blipIndex++];
                        if (img != null)
                        {
                            img.rectTransform.anchoredPosition = uiPos;
                            img.gameObject.SetActive(false); // Chưa hiện cho đến khi tia quét đi qua
                            img.color = new Color(0.2f, 1f, 0.2f, 0f);

                            activeBlips.Add(new ActiveBlip
                            {
                                image = img,
                                normalizedDist = normalizedDist,
                                angleDeg = clockAngle,
                                triggered = false
                            });
                        }
                    }
                }
            }

            // 2. Tia quét quay 360 độ trong sweepDuration
            float elapsed = 0f;
            float previousAngle = 0f;

            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / sweepDuration);
                float currentAngle = progress * 360f; // 0 -> 360 độ

                // Xoay kim quét
                if (sweepTransform != null)
                {
                    sweepTransform.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
                }

                // Kiểm tra các blip có góc nằm giữa previousAngle và currentAngle
                foreach (var blip in activeBlips)
                {
                    if (!blip.triggered && blip.angleDeg >= previousAngle && blip.angleDeg <= currentAngle)
                    {
                        blip.triggered = true;
                        blip.spawnTime = Time.time;
                        if (blip.image != null)
                        {
                            blip.image.gameObject.SetActive(true);
                            blip.image.color = new Color(0.2f, 1f, 0.2f, 1f);
                        }

                        // Phát âm thanh bíp (càng gần càng cao)
                        if (audioSource != null && blipSound != null)
                        {
                            // Pitch từ 0.9 (xa 35m) đến 1.5 (gần 0m)
                            float pitch = Mathf.Lerp(1.5f, 0.9f, blip.normalizedDist);
                            audioSource.pitch = pitch;
                            audioSource.PlayOneShot(blipSound, 0.9f);
                        }

                        onGhostDetected?.Invoke(true);
                    }
                }

                previousAngle = currentAngle;
                yield return null;
            }

            // Kết thúc tia quét
            if (sweepTransform != null)
            {
                sweepTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }

            isScanning = false;
            scanCoroutine = null;

            // Nếu không có ma trong tầm, hiện chữ "Không phát hiện" 1.5s
            if (ghostsInRange == 0)
            {
                onGhostDetected?.Invoke(false);
                ShowStatusMessage("Không phát hiện", 1.5f);
            }
            else
            {
                ShowStatusMessage("Đang nạp...", cooldownTimer);
            }
        }

        public void ShowStatusMessage(string message, float duration)
        {
            if (statusCoroutine != null) StopCoroutine(statusCoroutine);
            statusCoroutine = StartCoroutine(StatusMessageRoutine(message, duration));
        }

        private IEnumerator StatusMessageRoutine(string message, float duration)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            yield return new WaitForSeconds(duration);
            if (statusText != null && statusText.text == message)
            {
                // Nếu vẫn đang trong thời gian nạp thì hiện Đang nạp...
                if (cooldownTimer > 0f)
                {
                    statusText.text = "Đang nạp...";
                }
                else
                {
                    statusText.text = "";
                }
            }
            statusCoroutine = null;
        }

        public void CancelScan()
        {
            if (scanCoroutine != null)
            {
                StopCoroutine(scanCoroutine);
                scanCoroutine = null;
            }
            isScanning = false;
            if (sweepTransform != null) sweepTransform.localRotation = Quaternion.identity;
            foreach (var b in activeBlips)
            {
                if (b.image != null) b.image.gameObject.SetActive(false);
            }
            activeBlips.Clear();
            if (statusText != null) statusText.text = "";
        }

        public void ResetAll()
        {
            CancelScan();
            cooldownTimer = 0f;
            if (statusCoroutine != null) StopCoroutine(statusCoroutine);
            statusCoroutine = null;
            if (statusText != null) statusText.text = "";
            ShowHUD(false);
        }
    }
}
