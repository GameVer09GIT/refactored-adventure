using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Inventory
{
    /// <summary>
    /// Giao diện Radar 3D gắn trực tiếp lên màn hình máy Radar trong không gian thế giới (Diegetic World Space UI)
    /// Tự động tạo và điều khiển kim quét, vòng tròn đồng tâm và chấm blip hiển thị ma.
    /// </summary>
    public class Radar3DScreen : MonoBehaviour
    {
        [Header("Tham chiếu Màn hình")]
        [Tooltip("Transform bề mặt màn hình radar")]
        [SerializeField] private Transform screenMesh;

        [Header("Cấu hình Quét")]
        [Tooltip("Phạm vi quét tối đa (mét)")]
        [SerializeField] private float scanRange = 35.0f;

        [Tooltip("Thời gian quét 360 độ (giây)")]
        [SerializeField] private float sweepDuration = 1.5f;

        [Tooltip("Thời gian tồn tại của chấm blip (giây)")]
        [SerializeField] private float blipFadeDuration = 3.5f;

        [Header("Âm thanh")]
        [SerializeField] private AudioClip scanSound;
        [SerializeField] private AudioClip blipSound;

        [Header("Sprites giao diện")]
        [SerializeField] private Sprite dialSprite;
        [SerializeField] private Sprite sweepSprite;
        [SerializeField] private Sprite blipSprite;

        private AudioSource audioSource;
        private Canvas worldCanvas;
        private RectTransform sweepHand;
        private List<Image> blipPool = new List<Image>();
        private Coroutine scanRoutine;
        private bool isScanning = false;
        private float cooldownTimer = 0f;

        public bool CanScan => !isScanning && cooldownTimer <= 0f;

        void Awake()
        {
            if (screenMesh == null)
            {
                screenMesh = transform.Find("Body/Screen") ?? transform.Find("Screen");
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 0.5f;
                audioSource.maxDistance = 10f;
                audioSource.playOnAwake = false;
            }

            SetupWorldCanvas();
        }

        private void SetupWorldCanvas()
        {
            if (screenMesh == null)
            {
                screenMesh = transform.Find("Body/Screen") ?? transform.Find("Screen");
            }
            if (screenMesh == null) return;

#if UNITY_EDITOR
            if (dialSprite == null)
                dialSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Textures/radar_dial.png");
            if (sweepSprite == null)
                sweepSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Textures/radar_sweep.png");
            if (blipSprite == null)
                blipSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Textures/radar_blip.png");
#endif

            // Cấu hình vật liệu màn hình nền CRT màu xanh sẫm
            var mr = screenMesh.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material.SetColor("_Color", new Color(0.015f, 0.05f, 0.02f, 1f));
                mr.material.SetColor("_EmissionColor", new Color(0.01f, 0.15f, 0.03f, 1f));
            }

            // Xóa canvas cũ nếu có để tạo mới chính xác
            Transform oldCanvas = screenMesh.Find("Radar3DCanvas") ?? (screenMesh.parent != null ? screenMesh.parent.Find("Radar3DCanvas") : null);
            if (oldCanvas != null)
            {
                DestroyImmediate(oldCanvas.gameObject);
            }

            GameObject canvasGo = new GameObject("Radar3DCanvas");
            canvasGo.transform.position = screenMesh.position + screenMesh.forward * 0.002f;
            canvasGo.transform.rotation = Quaternion.LookRotation(screenMesh.forward, screenMesh.up);
            canvasGo.transform.localScale = Vector3.one * 0.00056f;
            canvasGo.transform.SetParent(screenMesh, true);

            worldCanvas = canvasGo.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 100f);

            // 1. RadarDial (Vòng tròn lưới Radar màu xanh lá)
            GameObject dialGo = new GameObject("RadarDial");
            dialGo.transform.SetParent(canvasGo.transform, false);
            RectTransform dialRect = dialGo.AddComponent<RectTransform>();
            dialRect.sizeDelta = new Vector2(95f, 95f);
            dialRect.anchoredPosition = Vector2.zero;
            Image dialImg = dialGo.AddComponent<Image>();
            dialImg.sprite = dialSprite;
            dialImg.color = new Color(0.3f, 1.0f, 0.4f, 1.0f);

            // 2. Kim quét / quạt quét (SweepHand)
            GameObject sweepGo = new GameObject("SweepHand");
            sweepGo.transform.SetParent(canvasGo.transform, false);
            sweepHand = sweepGo.AddComponent<RectTransform>();
            sweepHand.sizeDelta = new Vector2(95f, 95f);
            sweepHand.pivot = new Vector2(0.5f, 0.5f);
            sweepHand.anchoredPosition = Vector2.zero;
            Image sweepImg = sweepGo.AddComponent<Image>();
            sweepImg.sprite = sweepSprite;
            sweepImg.color = new Color(0.4f, 1.0f, 0.5f, 0.75f);
            sweepGo.SetActive(true);

            // 3. Pool 4 chấm blip hiển thị ma
            blipPool.Clear();
            for (int i = 0; i < 4; i++)
            {
                GameObject blipGo = new GameObject($"Blip_{i}");
                blipGo.transform.SetParent(canvasGo.transform, false);
                RectTransform bRect = blipGo.AddComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(10f, 10f);
                Image bImg = blipGo.AddComponent<Image>();
                bImg.sprite = blipSprite;
                bImg.color = new Color(0.3f, 1.0f, 0.4f, 0f);
                blipGo.SetActive(false);
                blipPool.Add(bImg);
            }
        }

        public bool TriggerScan(Transform playerCamera, System.Action<bool> onGhostDetected = null)
        {
            if (!CanScan) return false;

            if (scanRoutine != null) StopCoroutine(scanRoutine);
            scanRoutine = StartCoroutine(ScanRoutine(playerCamera, onGhostDetected));
            return true;
        }

        private IEnumerator ScanRoutine(Transform playerCamera, System.Action<bool> onGhostDetected)
        {
            isScanning = true;
            cooldownTimer = 4.0f;

            if (sweepHand != null)
            {
                sweepHand.gameObject.SetActive(true);
            }

            if (audioSource != null && scanSound != null)
            {
                audioSource.PlayOneShot(scanSound, 0.75f);
            }

            // Tìm kiếm ma trong phạm vi
            GhostAI[] ghosts = Object.FindObjectsByType<GhostAI>(FindObjectsSortMode.None);
            Vector3 camPos = playerCamera != null ? playerCamera.position : transform.position;
            Vector3 camFwd = playerCamera != null ? playerCamera.forward : transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 0.001f) camFwd = Vector3.forward;
            camFwd.Normalize();

            List<Vector2> detectedBlipCoords = new List<Vector2>();
            bool foundAnyGhost = false;

            if (ghosts != null)
            {
                foreach (var g in ghosts)
                {
                    if (g == null || !g.gameObject.activeInHierarchy) continue;
                    Vector3 diff = g.transform.position - camPos;
                    diff.y = 0f;
                    float dist = diff.magnitude;
                    if (dist <= scanRange)
                    {
                        foundAnyGhost = true;
                        // Góc so với camFwd (-180..180)
                        float angle = Vector3.SignedAngle(camFwd, diff.normalized, Vector3.up);
                        float normalizedDist = dist / scanRange;
                        float radius = 40f * normalizedDist;
                        float rad = -angle * Mathf.Deg2Rad;
                        Vector2 coord = new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius);
                        detectedBlipCoords.Add(coord);
                    }
                }
            }

            onGhostDetected?.Invoke(foundAnyGhost);

            // Kim quét quay 360 độ
            float elapsed = 0f;
            int blipIdx = 0;
            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / sweepDuration;
                float currentAngle = progress * 360f;

                if (sweepHand != null)
                {
                    sweepHand.localRotation = Quaternion.Euler(0f, 0f, -currentAngle);
                }

                // Kích hoạt blip khi kim quét đi qua góc tương ứng
                if (blipIdx < detectedBlipCoords.Count && progress > 0.4f)
                {
                    if (blipIdx < blipPool.Count)
                    {
                        StartCoroutine(FadeBlip(blipPool[blipIdx], detectedBlipCoords[blipIdx]));
                    }
                    if (audioSource != null && blipSound != null)
                    {
                        audioSource.PlayOneShot(blipSound, 0.8f);
                    }
                    blipIdx++;
                }

                yield return null;
            }

            // Kết thúc quét: giữ sweepHand luôn hiển thị để tiếp tục xoay kim quét chờ
            isScanning = false;
            scanRoutine = null;
        }

        private IEnumerator FadeBlip(Image blip, Vector2 localPos)
        {
            if (blip == null) yield break;
            blip.gameObject.SetActive(true);
            blip.rectTransform.anchoredPosition = localPos;

            float elapsed = 0f;
            while (elapsed < blipFadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = 1f - (elapsed / blipFadeDuration);
                blip.color = new Color(0.2f, 1f, 0.3f, a);
                yield return null;
            }

            blip.gameObject.SetActive(false);
        }

        void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }

            // Hiệu ứng kim quét (Sweep line/wedge màu xanh lá xoay tròn) mượt mà liên tục trên Canvas 3D
            if (sweepHand != null && !isScanning)
            {
                float angle = (Time.time * 120f) % 360f;
                sweepHand.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }
    }
}
