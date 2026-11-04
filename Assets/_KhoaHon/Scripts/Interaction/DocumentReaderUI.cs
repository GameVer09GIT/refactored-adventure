using System.Collections;
using KhoaHon.Core;
using KhoaHon.Notebook;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Màn hình đọc tài liệu: giấy cũ, chữ hiện dần, có nút đóng. Đọc lần đầu thì ghi vào Sổ tay.
    /// Cũng dùng để xem lại mục Sổ tay (không ghi lại).
    /// </summary>
    public class DocumentReaderUI : MonoBehaviour
    {
        public static DocumentReaderUI Instance { get; private set; }

        [Tooltip("Tốc độ hiện chữ (ký tự/giây). 0 = hiện ngay")]
        [SerializeField] private float charsPerSecond = 140f;

        private GameObject root;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI yearText;
        private TextMeshProUGUI bodyText;
        private Coroutine reveal;
        private int openedFrame;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start() => Build();

        private void Build()
        {
            var hud = KhoaHonHUD.Instance;
            var style = hud != null ? hud.Style : null;
            var parent = hud != null ? hud.Root : transform;

            var dim = UIFactory.Panel("KH_DocumentReader", parent, new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = dim.gameObject;

            var paperColor = style != null ? style.paperColor : new Color(0.86f, 0.8f, 0.66f);
            var ink = style != null ? style.inkColor : Color.black;
            var paper = UIFactory.Panel("Paper", dim.transform, paperColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520, -440), new Vector2(520, 440));

            var yRect = UIFactory.Rect("Year", paper.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -90), new Vector2(-60, -40));
            yearText = UIFactory.Text("Text", yRect, style, true, 26, new Color(ink.r, ink.g, ink.b, 0.6f), TextAlignmentOptions.TopRight);

            var tRect = UIFactory.Rect("Title", paper.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -150), new Vector2(-60, -60));
            titleText = UIFactory.Text("Text", tRect, style, true, 40, ink, TextAlignmentOptions.TopLeft);
            if (style != null && style.serifBoldFont != null) titleText.font = style.serifBoldFont;

            var bRect = UIFactory.Rect("Body", paper.transform, Vector2.zero, Vector2.one, new Vector2(60, 110), new Vector2(-60, -170));
            bodyText = UIFactory.Text("Text", bRect, style, true, 30, ink, TextAlignmentOptions.TopLeft);
            bodyText.lineSpacing = 8f;

            var closeRect = UIFactory.Rect("Close", paper.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-130, 30), new Vector2(130, 85));
            var btn = UIFactory.Button("Button", closeRect, style, T.Get("ui.close"), 26, new Color(0.25f, 0.18f, 0.14f, 0.9f), paperColor);
            btn.onClick.AddListener(Close);

            root.SetActive(false);
        }

        /// <summary>Mở tài liệu. recordInNotebook=false khi xem lại từ Sổ tay.</summary>
        public void Open(NotebookSource doc, bool recordInNotebook = true)
        {
            if (doc == null) return;
            if (root == null) Build();
            if (recordInNotebook)
            {
                bool added = NotebookManager.Instance != null && NotebookManager.Instance.Add(doc);
                if (doc is DocumentData dd && dd.flagOnRead != StoryFlag.None) GameManager.Instance?.SetFlag(dd.flagOnRead);
                if (added) KhoaHonHUD.Toast(T.Format("toast.notebook_added", doc.Title));
            }

            var style = KhoaHonHUD.Instance != null ? KhoaHonHUD.Instance.Style : null;
            if (style != null)
            {
                bodyText.font = doc.Serif ? style.serifFont : style.sansFont;
            }
            titleText.text = doc.Title;
            yearText.text = HanhUtil.YearLabel(doc.Year);
            bodyText.text = doc.Body;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            openedFrame = Time.frameCount;
            ModalUI.Open(this);

            if (reveal != null) StopCoroutine(reveal);
            reveal = charsPerSecond > 0f ? StartCoroutine(Reveal()) : null;
            if (reveal == null) bodyText.maxVisibleCharacters = int.MaxValue;
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (reveal != null) { StopCoroutine(reveal); reveal = null; }
            root.SetActive(false);
            ModalUI.Close(this);
        }

        private IEnumerator Reveal()
        {
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            float shown = 0f;
            bodyText.maxVisibleCharacters = 0;
            while (shown < total)
            {
                shown += charsPerSecond * Time.unscaledDeltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }
            bodyText.maxVisibleCharacters = int.MaxValue;
            reveal = null;
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame || Keyboard.current == null) return;
            bool pressed = Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame
                           || Keyboard.current.spaceKey.wasPressedThisFrame;
            if (!pressed) return;
            if (reveal != null)
            {
                StopCoroutine(reveal); reveal = null;
                bodyText.maxVisibleCharacters = int.MaxValue;
            }
            else Close();
        }
    }
}
