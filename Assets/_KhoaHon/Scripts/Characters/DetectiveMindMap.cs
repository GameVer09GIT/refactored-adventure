using System;
using System.Collections.Generic;
using KhoaHon.Core;
using KhoaHon.Notebook;
using KhoaHon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Sơ đồ trinh thám của Evelyn (phím M): bảng ghim các manh mối đã thu thập, sợi chỉ đỏ nối hai manh mối
    /// khi đã có đủ cả hai, kèm dòng suy luận. Manh mối chưa tìm thấy hiện dạng "?".
    /// </summary>
    public class DetectiveMindMap : MonoBehaviour
    {
        [Serializable]
        public class Node
        {
            public string id;
            [Tooltip("Vị trí chuẩn hóa trên bảng (0..1)")] public Vector2 pos;
        }

        [Serializable]
        public class Link
        {
            public string from, to;
            [Tooltip("Key TextTable dòng suy luận")] public string captionKey;
        }

        [SerializeField] private Node[] nodes = new Node[0];
        [SerializeField] private Link[] links = new Link[0];

        private GameObject ui;

        public void Configure(Node[] n, Link[] l) { nodes = n; links = l; }

        private bool Allowed => GameManager.Instance != null && GameManager.Instance.Character == PlayerCharacter.Evelyn;

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (ui != null)
            {
                if (k.mKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame) Close();
                return;
            }
            if (k.mKey.wasPressedThisFrame && Allowed && GameFlow.IsPlaying && !ModalUI.IsAnyOpen && !PlayerState.IsHiding) Open();
        }

        private void Open()
        {
            ModalUI.Open(this);
            var bg = UIFactory.Panel("KH_MindMap", UIKit.TopLayer, new Color(0.11f, 0.08f, 0.06f, 0.97f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ui = bg.gameObject;
            var title = UIKit.Label(bg.transform, T.Get("mind.title"), 46, UIKit.Ink, TextAlignmentOptions.TopLeft, true, true);
            title.rectTransform.offsetMin = new Vector2(80, 0); title.rectTransform.offsetMax = new Vector2(-80, -40);
            var hint = UIKit.Label(bg.transform, T.Get("mind.hint"), 20, UIKit.Dim, TextAlignmentOptions.BottomLeft);
            hint.rectTransform.offsetMin = new Vector2(80, 30);
            var board = UIFactory.Rect("Board", bg.transform, Vector2.zero, Vector2.one, new Vector2(80, 90), new Vector2(-80, -120));

            var nm = NotebookManager.Instance;
            bool Has(string id) => nm != null && nm.Contains(id);
            var db = GameManager.Instance != null ? GameManager.Instance.Database : null;
            var pos = new Dictionary<string, Vector2>();
            foreach (var n in nodes) pos[n.id] = n.pos;

            // Chỉ đỏ trước (nằm dưới các mảnh giấy), chú thích vẽ sau cùng để không bị che
            int found = 0;
            var captions = new List<(Vector2, Vector2, string)>();
            foreach (var l in links)
            {
                if (!pos.ContainsKey(l.from) || !pos.ContainsKey(l.to)) continue;
                bool both = Has(l.from) && Has(l.to);
                if (both) found++;
                DrawString(board, pos[l.from], pos[l.to], both, null);
                if (both) captions.Add((pos[l.from], pos[l.to], T.Get(l.captionKey)));
            }
            // Mảnh giấy ghim
            foreach (var n in nodes)
            {
                bool has = Has(n.id);
                var src = has && db != null ? db.Find(n.id) : null;
                Card(board, n.pos, has ? (src != null ? src.Title : n.id) : "?", has);
            }
            foreach (var (a, b, cap) in captions) DrawCaption(board, a, b, cap);
            var progress = UIKit.Label(bg.transform, T.Format("mind.progress", found, links.Length), 22, new Color(0.85f, 0.3f, 0.25f), TextAlignmentOptions.TopRight, false, true);
            progress.rectTransform.offsetMin = new Vector2(80, 0); progress.rectTransform.offsetMax = new Vector2(-80, -52);
        }

        private void Close()
        {
            if (ui != null) Destroy(ui);
            ui = null;
            ModalUI.Close(this);
        }

        private static void Card(RectTransform board, Vector2 p, string text, bool found)
        {
            var c = UIFactory.Panel("Card", board, found ? new Color(0.86f, 0.80f, 0.66f, 1f) : new Color(0.3f, 0.26f, 0.22f, 0.85f),
                p, p, new Vector2(-120, -38), new Vector2(120, 38));
            c.transform.localRotation = Quaternion.Euler(0, 0, (p.x * 37f + p.y * 53f) % 6f - 3f);
            var t = UIKit.Label(c.transform, text, found ? 19 : 34, found ? new Color(0.12f, 0.08f, 0.05f) : new Color(0.75f, 0.7f, 0.62f), TextAlignmentOptions.Center, true, found);
            t.rectTransform.offsetMin = new Vector2(10, 4); t.rectTransform.offsetMax = new Vector2(-10, -4);
            UIFactory.Panel("Pin", c.transform, new Color(0.7f, 0.08f, 0.06f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-6, -12), new Vector2(6, 0));
        }

        private static void DrawString(RectTransform board, Vector2 a, Vector2 b, bool solid, string caption)
        {
            var size = board.rect.size;
            if (size.x < 10f) size = new Vector2(1760, 870);
            var pa = new Vector2(a.x * size.x, a.y * size.y);
            var pb = new Vector2(b.x * size.x, b.y * size.y);
            var mid = (pa + pb) * 0.5f;
            float len = Vector2.Distance(pa, pb);
            float ang = Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg;
            var line = UIFactory.Panel("String", board, solid ? new Color(0.72f, 0.08f, 0.06f, 0.95f) : new Color(0.5f, 0.45f, 0.4f, 0.25f),
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var rt = line.rectTransform;
            rt.sizeDelta = new Vector2(len, solid ? 4f : 2f);
            rt.anchoredPosition = mid;
            rt.localRotation = Quaternion.Euler(0, 0, ang);
            if (!string.IsNullOrEmpty(caption)) DrawCaption(board, a, b, caption);
        }

        private static void DrawCaption(RectTransform board, Vector2 a, Vector2 b, string caption)
        {
            var size = board.rect.size;
            if (size.x < 10f) size = new Vector2(1760, 870);
            var pa = new Vector2(a.x * size.x, a.y * size.y);
            var pb = new Vector2(b.x * size.x, b.y * size.y);
            var mid = (pa + pb) * 0.5f;
            float len = Vector2.Distance(pa, pb);
            {
                var cap = UIFactory.Panel("Caption", board, new Color(0.05f, 0.03f, 0.02f, 0.85f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                cap.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(len * 0.7f, 220f, 380f), 54f);
                cap.rectTransform.anchoredPosition = mid;
                var t = UIKit.Label(cap.transform, caption, 16, new Color(0.95f, 0.8f, 0.7f), TextAlignmentOptions.Center);
                t.rectTransform.offsetMin = new Vector2(8, 2); t.rectTransform.offsetMax = new Vector2(-8, -2);
            }
        }
    }
}
