using System.Collections;
using TMPro;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// HUD tối giản: prompt tương tác (giữa-dưới), thông báo ngắn (trên), phụ đề (dưới).
    /// Tự dựng Canvas lúc chạy. Các UI khác (Sổ tay, đọc tài liệu) gắn vào <see cref="Root"/>.
    /// </summary>
    public class KhoaHonHUD : MonoBehaviour
    {
        public static KhoaHonHUD Instance { get; private set; }

        [SerializeField] private UIStyle style;
        [Tooltip("Thời gian hiện thông báo ngắn (giây)")]
        [SerializeField] private float toastDuration = 2.6f;

        private Canvas canvas;
        private TextMeshProUGUI promptText;
        private TextMeshProUGUI toastText;
        private TextMeshProUGUI subtitleText;
        private CanvasGroup toastGroup;
        private Coroutine toastRoutine;
        private Coroutine subtitleRoutine;

        public UIStyle Style => style;
        public Transform Root => canvas != null ? canvas.transform : transform;

        public void SetStyle(UIStyle s) => style = s;

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // DeathScreenManager (script cũ, không sửa) tự dựng chữ bằng font TMP mặc định thiếu dấu tiếng Việt
            // → đổi sang font của Khóa Hồn ở runtime để chữ hiển thị đúng và không ghi vào LiberationSans Fallback.
            if (style == null || style.sansFont == null) return;
            var dsm = FindFirstObjectByType<DeathScreenManager>();
            if (dsm == null) return;
            foreach (var t in dsm.GetComponentsInChildren<TMP_Text>(true)) t.font = style.sansFont;
        }

        private void Build()
        {
            canvas = UIFactory.Canvas("KH_HUDCanvas", 40);
            canvas.transform.SetParent(transform, false);

            var promptRect = UIFactory.Rect("Prompt", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-500, 150), new Vector2(500, 210));
            promptText = UIFactory.Text("Text", promptRect, style, false, 30, style ? style.textColor : Color.white, TextAlignmentOptions.Center);
            promptText.outlineWidth = 0.15f; promptText.outlineColor = new Color(0, 0, 0, 0.8f);
            promptText.text = "";

            var subRect = UIFactory.Rect("Subtitle", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-760, 230), new Vector2(760, 360));
            subtitleText = UIFactory.Text("Text", subRect, style, false, 32, style ? style.textColor : Color.white, TextAlignmentOptions.Bottom);
            subtitleText.outlineWidth = 0.18f; subtitleText.outlineColor = new Color(0, 0, 0, 0.9f);
            subtitleText.text = "";

            var toastRect = UIFactory.Rect("Toast", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-700, -170), new Vector2(700, -70));
            toastGroup = toastRect.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastText = UIFactory.Text("Text", toastRect, style, false, 28, style ? style.dimTextColor : Color.gray, TextAlignmentOptions.Center);
            toastText.outlineWidth = 0.15f; toastText.outlineColor = new Color(0, 0, 0, 0.8f);
        }

        private UnityEngine.UI.RawImage vignette;

        /// <summary>Viền tối quanh màn hình (0 = tắt). Dùng khi trốn trong tủ / gầm giường.</summary>
        public void SetVignette(float strength)
        {
            if (vignette == null)
            {
                var rt = UIFactory.Stretch("Vignette", canvas.transform);
                rt.SetAsFirstSibling();
                vignette = rt.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                vignette.raycastTarget = false;
                const int S = 256;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                        // Khe nhìn hẹp ngang (giống nhìn qua khe tủ)
                        float d = Mathf.Sqrt(dx * dx * 0.55f + dy * dy * 1.9f);
                        tex.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0.15f, 1.0f, d)));
                    }
                tex.Apply();
                vignette.texture = tex;
            }
            vignette.color = new Color(1, 1, 1, Mathf.Clamp01(strength));
            vignette.gameObject.SetActive(strength > 0.001f);
        }

        public void SetPrompt(string text)
        {
            if (promptText != null && promptText.text != text) promptText.text = text ?? "";
        }

        public void ShowToast(string text)
        {
            if (toastText == null || string.IsNullOrEmpty(text)) return;
            toastText.text = text;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        public void ShowSubtitle(string text, float duration)
        {
            if (subtitleText == null) return;
            if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
            subtitleRoutine = StartCoroutine(SubtitleRoutine(text, duration));
        }

        public void ClearSubtitle()
        {
            if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
            if (subtitleText != null) subtitleText.text = "";
        }

        private IEnumerator ToastRoutine()
        {
            float t = 0f;
            while (t < 0.2f) { t += Time.unscaledDeltaTime; toastGroup.alpha = t / 0.2f; yield return null; }
            toastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(toastDuration);
            t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; toastGroup.alpha = 1f - t / 0.5f; yield return null; }
            toastGroup.alpha = 0f;
        }

        private IEnumerator SubtitleRoutine(string text, float duration)
        {
            subtitleText.text = text;
            yield return new WaitForSeconds(duration);
            subtitleText.text = "";
        }

        // Truy cập tĩnh an toàn
        public static void Prompt(string text) { if (Instance != null) Instance.SetPrompt(text); }
        public static void Toast(string text) { if (Instance != null) Instance.ShowToast(text); }
        public static void Subtitle(string text, float duration) { if (Instance != null) Instance.ShowSubtitle(text, duration); }
    }
}
