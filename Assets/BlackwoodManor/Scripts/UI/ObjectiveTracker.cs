using System.Collections;
using Blackwood.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackwood.UI
{
    /// <summary>
    /// Mục tiêu hiện tại (góc trên trái): tự hiện vài giây khi sang chương mới hoặc khi bấm [O], rồi mờ đi để giữ màn hình sạch.
    /// Lần đầu vào chơi của mỗi phiên còn hiện thẻ phím điều khiển. Chữ lấy từ TextTable ("obj.{chương}", "ctrl.*").
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private float showSeconds = 7f;

        private CanvasGroup group, controls;
        private TextMeshProUGUI head, body, controlsText;
        private Coroutine routine;
        private bool built, controlsShown;
        private int shownStage = -1;
        private static bool controlsShownThisSession;

        private void OnEnable()
        {
            GameEvents.StageChanged += OnStage;
            GameFlow.Changed += OnFlow;
        }

        private void OnDisable()
        {
            GameEvents.StageChanged -= OnStage;
            GameFlow.Changed -= OnFlow;
        }

        private void Build()
        {
            var hud = BlackwoodHUD.Instance;
            if (hud == null || built) return;
            built = true;
            var root = UIFactory.Rect("Objective", hud.Root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -190), new Vector2(760, -48));
            group = root.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;
            UIFactory.Panel("Edge", root, UIKit.Blood, new Vector2(0, 0.18f), new Vector2(0, 0.95f), new Vector2(0, 0), new Vector2(3, 0)).raycastTarget = false;
            head = UIKit.Label(root, "", 17, UIKit.Blood, TextAlignmentOptions.TopLeft, false, true);
            head.rectTransform.offsetMin = new Vector2(18, 0); head.rectTransform.offsetMax = new Vector2(0, -4);
            head.characterSpacing = 9f; head.raycastTarget = false;
            body = UIKit.Label(root, "", 25, UIKit.Ink, TextAlignmentOptions.TopLeft, true);
            body.rectTransform.offsetMin = new Vector2(18, 0); body.rectTransform.offsetMax = new Vector2(0, -34);
            body.raycastTarget = false; body.lineSpacing = 4f;
            body.outlineWidth = 0.14f; body.outlineColor = new Color(0, 0, 0, 0.85f);

            var cr = UIFactory.Rect("Controls", hud.Root, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 60), new Vector2(620, 330));
            controls = cr.gameObject.AddComponent<CanvasGroup>(); controls.alpha = 0f; controls.blocksRaycasts = false;
            UIFactory.Panel("Bg", cr, new Color(0, 0, 0, 0.42f), Vector2.zero, Vector2.one, new Vector2(-16, -12), new Vector2(8, 12)).raycastTarget = false;
            controlsText = UIKit.Label(cr, "", 21, UIKit.Ink, TextAlignmentOptions.BottomLeft);
            controlsText.raycastTarget = false; controlsText.lineSpacing = 10f;
        }

        private void OnStage(GameStage s) { if (GameFlow.IsPlaying) Show(); }

        private void OnFlow(FlowState s)
        {
            if (s != FlowState.Playing) return;
            Show();
            if (!controlsShownThisSession) { controlsShownThisSession = true; StartCoroutine(ShowControls()); }
        }

        private void Update()
        {
            if (!GameFlow.IsPlaying || ModalUI.IsAnyOpen) return;
            // Nạp file lưu đặt chương sau khi đã vào Playing (không bắn StageChanged) → dòng mục tiêu phải tự bắt kịp
            var gm = GameManager.Instance;
            if (gm != null && shownStage >= 0 && !gm.IsRestoring && (int)gm.Stage != shownStage) Show();
            if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame) Show();
        }

        public void Show()
        {
            Build();
            if (group == null) return;
            var gm = GameManager.Instance;
            int stage = gm != null ? (int)gm.Stage : 0;
            string key = "obj." + stage;
            if (T.Table == null || !T.Table.Has(key)) return;
            shownStage = stage;
            head.text = T.Get("obj.title");
            body.text = T.Get(key);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            while (group.alpha < 1f) { group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime * 2.5f); yield return null; }
            float t = 0f;
            while (t < showSeconds) { if (GameFlow.IsPlaying) t += Time.unscaledDeltaTime; yield return null; }
            while (group.alpha > 0f) { group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 0.8f); yield return null; }
            routine = null;
        }

        private IEnumerator ShowControls()
        {
            Build();
            if (controls == null || controlsShown) yield break;
            controlsShown = true;
            controlsText.text = T.Get("ctrl.card");
            yield return new WaitForSecondsRealtime(1.5f);
            while (controls.alpha < 1f) { controls.alpha = Mathf.MoveTowards(controls.alpha, 1f, Time.unscaledDeltaTime * 2f); yield return null; }
            float t = 0f;
            while (t < 11f) { if (GameFlow.IsPlaying) t += Time.unscaledDeltaTime; yield return null; }
            while (controls.alpha > 0f) { controls.alpha = Mathf.MoveTowards(controls.alpha, 0f, Time.unscaledDeltaTime * 0.7f); yield return null; }
        }
    }
}
