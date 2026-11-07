using System.Collections.Generic;
using KhoaHon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KhoaHon.Notebook
{
    /// <summary>
    /// Sổ tay điều tra (Tab / J). 5 tab: Tài liệu · Băng ghi âm · Ảnh · Bùa chú · Kỷ vật.
    /// Bản đầu là danh sách theo mốc Biên niên sử. Mặc định KHÔNG dừng game.
    /// </summary>
    public class NotebookUI : MonoBehaviour
    {
        public static NotebookUI Instance { get; private set; }

        [Tooltip("Dừng thời gian khi mở Sổ tay (mặc định tắt để tăng căng thẳng)")]
        [SerializeField] private bool pauseGameWhenOpen = false;

        private static readonly string[] TabKeys =
        {
            "notebook.tab.documents", "notebook.tab.tapes", "notebook.tab.photos", "notebook.tab.talismans", "notebook.tab.relics",
        };

        private GameObject root;
        private readonly List<TextMeshProUGUI> tabLabels = new List<TextMeshProUGUI>();
        private readonly List<Image> tabBgs = new List<Image>();
        private RectTransform listContent;
        private TextMeshProUGUI detailTitle, detailYear, detailBody, emptyText;
        private NotebookTab currentTab = NotebookTab.Documents;
        private NotebookSource selected;
        private UIStyle style;
        private float prevTimeScale = 1f;

        public bool IsOpen => root != null && root.activeSelf;
        public NotebookTab CurrentTab => currentTab;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            Build();
            if (NotebookManager.Instance != null) NotebookManager.Instance.Changed += OnNotebookChanged;
        }

        private void OnDisable()
        {
            if (NotebookManager.Instance != null) NotebookManager.Instance.Changed -= OnNotebookChanged;
        }

        private void OnNotebookChanged()
        {
            if (IsOpen) Refresh();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            bool toggle = Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame;
            if (IsOpen)
            {
                if (toggle || Keyboard.current.escapeKey.wasPressedThisFrame) Close();
                else if (Keyboard.current.qKey.wasPressedThisFrame) SelectTab((NotebookTab)(((int)currentTab + 4) % 5));
                else if (Keyboard.current.eKey.wasPressedThisFrame) SelectTab((NotebookTab)(((int)currentTab + 1) % 5));
                else if (selected is TapeData tape && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                {
                    // Phát băng đang chọn rồi đóng sổ để vừa đi vừa nghe
                    Close();
                    Narrative.TapePlayer.Instance?.Play(tape);
                }
            }
            else if (toggle && !ModalUI.IsAnyOpen)
            {
                Open();
            }
        }

        public void Open(NotebookTab? tab = null)
        {
            if (root == null) Build();
            if (tab.HasValue) currentTab = tab.Value;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            ModalUI.Open(this);
            if (pauseGameWhenOpen) { prevTimeScale = Time.timeScale; Time.timeScale = 0f; }
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            if (pauseGameWhenOpen) Time.timeScale = prevTimeScale;
            ModalUI.Close(this);
        }

        public void SelectTab(NotebookTab tab)
        {
            currentTab = tab;
            selected = null;
            Refresh();
        }

        // ---------------- Dựng UI ----------------
        private void Build()
        {
            if (root != null) return;
            var hud = KhoaHonHUD.Instance;
            style = hud != null ? hud.Style : null;
            var parent = hud != null ? hud.Root : transform;
            var paperColor = style != null ? style.paperColor : new Color(0.86f, 0.8f, 0.66f);
            var ink = style != null ? style.inkColor : Color.black;

            var dim = UIFactory.Panel("KH_Notebook", parent, new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = dim.gameObject;
            var book = UIFactory.Panel("Book", dim.transform, paperColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-760, -450), new Vector2(760, 450));

            var titleRect = UIFactory.Rect("Title", book.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -80), new Vector2(-50, -20));
            var title = UIFactory.Text("Text", titleRect, style, true, 40, ink, TextAlignmentOptions.Left);
            if (style != null && style.serifBoldFont != null) title.font = style.serifBoldFont;
            title.text = T.Get("notebook.title");

            var hintRect = UIFactory.Rect("Hint", book.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -80), new Vector2(-50, -20));
            var hint = UIFactory.Text("Text", hintRect, style, false, 20, new Color(ink.r, ink.g, ink.b, 0.55f), TextAlignmentOptions.Right);
            hint.text = T.Get("notebook.hint");

            // Hàng tab
            var tabsRect = UIFactory.Rect("Tabs", book.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -95));
            var hl = tabsRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var b = UIFactory.Button("Tab" + i, tabsRect, style, T.Get(TabKeys[i]), 24, new Color(0.3f, 0.22f, 0.16f, 0.25f), ink);
                b.onClick.AddListener(() => SelectTab((NotebookTab)idx));
                tabBgs.Add(b.GetComponent<Image>());
                tabLabels.Add(b.GetComponentInChildren<TextMeshProUGUI>());
            }

            // Cột danh sách (trái)
            var listPanel = UIFactory.Panel("List", book.transform, new Color(0.3f, 0.22f, 0.16f, 0.12f), new Vector2(0, 0), new Vector2(0.38f, 1), new Vector2(40, 40), new Vector2(-10, -170));
            var scroll = listPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 30f; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = UIFactory.Stretch("Viewport", listPanel.transform, 8);
            viewport.gameObject.AddComponent<RectMask2D>();
            listContent = UIFactory.Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            listContent.pivot = new Vector2(0.5f, 1f);
            var vl = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 6; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            var fit = listContent.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = listContent;

            var emptyRect = UIFactory.Rect("Empty", listPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -80), new Vector2(-20, -20));
            emptyText = UIFactory.Text("Text", emptyRect, style, false, 22, new Color(ink.r, ink.g, ink.b, 0.5f), TextAlignmentOptions.TopLeft);
            emptyText.text = T.Get("notebook.empty");

            // Cột chi tiết (phải)
            var detail = UIFactory.Rect("Detail", book.transform, new Vector2(0.38f, 0), new Vector2(1, 1), new Vector2(20, 40), new Vector2(-50, -170));
            var dyRect = UIFactory.Rect("Year", detail, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -40), new Vector2(0, 0));
            detailYear = UIFactory.Text("Text", dyRect, style, true, 22, new Color(ink.r, ink.g, ink.b, 0.6f), TextAlignmentOptions.TopRight);
            var dtRect = UIFactory.Rect("Title", detail, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(-140, 0));
            detailTitle = UIFactory.Text("Text", dtRect, style, true, 34, ink, TextAlignmentOptions.TopLeft);
            if (style != null && style.serifBoldFont != null) detailTitle.font = style.serifBoldFont;
            var dbRect = UIFactory.Rect("Body", detail, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -115));
            detailBody = UIFactory.Text("Text", dbRect, style, true, 25, ink, TextAlignmentOptions.TopLeft);
            detailBody.lineSpacing = 6;
            detailBody.overflowMode = TextOverflowModes.Ellipsis;

            root.SetActive(false);
        }

        private void Refresh()
        {
            var nm = NotebookManager.Instance;
            var ink = style != null ? style.inkColor : Color.black;
            for (int i = 0; i < 5; i++)
            {
                int unseen = nm != null ? nm.UnseenCount((NotebookTab)i) : 0;
                tabLabels[i].text = T.Get(TabKeys[i]) + (unseen > 0 ? $" <color=#8B1A1A>({unseen})</color>" : "");
                tabBgs[i].color = i == (int)currentTab ? new Color(0.3f, 0.22f, 0.16f, 0.45f) : new Color(0.3f, 0.22f, 0.16f, 0.15f);
            }

            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);

            var items = nm != null ? nm.EntriesFor(currentTab) : new List<NotebookSource>();
            emptyText.gameObject.SetActive(items.Count == 0);
            if (selected != null && !items.Contains(selected)) selected = null;
            if (selected == null && items.Count > 0) selected = items[0];

            ChronoYear? lastYear = null;
            foreach (var s in items)
            {
                if (lastYear != s.Year)
                {
                    lastYear = s.Year;
                    var hdr = new GameObject("Year_" + s.Year, typeof(RectTransform), typeof(LayoutElement));
                    hdr.transform.SetParent(listContent, false);
                    hdr.GetComponent<LayoutElement>().preferredHeight = 34;
                    var ht = UIFactory.Text("Text", hdr.transform, style, true, 22, new Color(0.55f, 0.1f, 0.08f, 1f), TextAlignmentOptions.BottomLeft);
                    ht.text = "— " + HanhUtil.YearLabel(s.Year) + " —";
                }
                var row = new GameObject("Entry_" + s.Id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                row.transform.SetParent(listContent, false);
                row.GetComponent<LayoutElement>().preferredHeight = 48;
                row.GetComponent<Image>().color = s == selected ? new Color(0.3f, 0.22f, 0.16f, 0.35f) : new Color(0, 0, 0, 0.03f);
                var src = s;
                row.GetComponent<Button>().onClick.AddListener(() => { selected = src; Refresh(); });
                var rt = UIFactory.Text("Text", row.transform, style, false, 22, ink, TextAlignmentOptions.MidlineLeft);
                rt.margin = new Vector4(12, 0, 8, 0);
                bool isNew = nm != null && !nm.IsSeen(s);
                rt.text = s.Title + (isNew ? $"  <color=#8B1A1A><size=70%>— {T.Get("notebook.new")}</size></color>" : "");
            }

            ShowDetail(selected);
        }

        private void ShowDetail(NotebookSource s)
        {
            if (s == null)
            {
                detailTitle.text = ""; detailYear.text = ""; detailBody.text = "";
                return;
            }
            detailTitle.text = s.Title;
            detailYear.text = HanhUtil.YearLabel(s.Year);
            string body = s.Body;
            if (s is TalismanData t && t.ChantLines != null && t.ChantLines.Count > 0)
                body += "\n\n<i>" + string.Join("\n", t.ChantLines) + "</i>";
            if (s is TapeData tape && tape.lines != null && tape.lines.Count > 0)
                body += "\n\n<size=85%>" + T.Get("notebook.tape_hint") + "</size>";
            detailBody.text = body;
            if (style != null) detailBody.font = s.Serif ? style.serifFont : style.sansFont;
            var nm = NotebookManager.Instance;
            if (nm != null && !nm.IsSeen(s))
            {
                // Đánh dấu đã xem mà không dựng lại cả danh sách ngay (tránh đệ quy)
                nm.Changed -= OnNotebookChanged;
                nm.MarkSeen(s);
                nm.Changed += OnNotebookChanged;
            }
        }
    }
}
