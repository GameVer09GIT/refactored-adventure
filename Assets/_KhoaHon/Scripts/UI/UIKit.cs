using System;
using KhoaHon.Core;
using KhoaHon.Horror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KhoaHon.UI
{
    /// <summary>Bộ khối UI kiểu game kinh dị thương mại: nút chữ có vạch đỏ khi hover, slider mảnh, bộ chọn ‹ giá trị ›.</summary>
    public static class UIKit
    {
        private static Canvas top;
        /// <summary>Canvas trên cùng (Cài đặt, hộp xác nhận) — luôn bật, không phụ thuộc HUD hay menu.</summary>
        public static Transform TopLayer
        {
            get
            {
                if (top == null) top = UIFactory.Canvas("KH_TopCanvas", 95);
                return top.transform;
            }
        }

        public static readonly Color Ink = new Color(0.86f, 0.83f, 0.78f);
        public static readonly Color Dim = new Color(0.55f, 0.52f, 0.48f);
        public static readonly Color Blood = new Color(0.66f, 0.05f, 0.04f);
        public static readonly Color Panel = new Color(0.02f, 0.015f, 0.015f, 0.86f);
        public static AudioClip Hover, Click, Back;

        public static UIStyle Style => KhoaHonHUD.Instance != null ? KhoaHonHUD.Instance.Style : null;

        public static TextMeshProUGUI Label(Transform parent, string text, float size, Color c, TextAlignmentOptions a, bool serif = false, bool bold = false)
        {
            var t = UIFactory.Text("Label", parent, Style, serif, size, c, a);
            if (bold && Style != null) t.font = serif ? (Style.serifBoldFont ?? Style.serifFont) : (Style.sansBoldFont ?? Style.sansFont);
            t.text = text;
            return t;
        }

        public static RectTransform Row(Transform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return (RectTransform)go.transform;
        }

        public static RectTransform Column(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax, float spacing)
        {
            var rt = UIFactory.Rect(name, parent, anchorMin, anchorMax, offMin, offMax);
            var vl = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false; vl.childAlignment = TextAnchor.UpperLeft;
            return rt;
        }

        /// <summary>Nút menu chính: chữ lớn, hover → đỏ + trượt phải + vạch.</summary>
        public static Button MenuButton(Transform parent, string text, Action onClick, float size = 40f, float height = 64f)
        {
            var row = Row(parent, height);
            var img = row.gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0);
            var btn = row.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
            var bar = UIFactory.Panel("Bar", row, Blood, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -2), new Vector2(0, 2));
            var lbl = Label(row, text, size, Ink, TextAlignmentOptions.MidlineLeft, true);
            lbl.margin = new Vector4(0, 0, 0, 0);
            var fx = row.gameObject.AddComponent<KHButtonFX>(); fx.Init(lbl, bar.rectTransform, btn);
            btn.onClick.AddListener(() => { AmbientScares.Play2D(Click, 0.6f); onClick?.Invoke(); });
            return btn;
        }

        /// <summary>Nút nhỏ (Áp dụng / Mặc định / Quay lại).</summary>
        public static Button SmallButton(Transform parent, string text, Action onClick, float width = 220f)
        {
            var go = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredWidth = width; go.GetComponent<LayoutElement>().preferredHeight = 52;
            go.GetComponent<Image>().color = new Color(0.12f, 0.03f, 0.03f, 0.75f);
            var btn = go.GetComponent<Button>(); btn.transition = Selectable.Transition.None;
            var lbl = Label(go.transform, text, 24, Ink, TextAlignmentOptions.Center, false, true);
            var fx = go.AddComponent<KHButtonFX>(); fx.Init(lbl, null, btn, false);
            btn.onClick.AddListener(() => { AmbientScares.Play2D(Click, 0.6f); onClick?.Invoke(); });
            return btn;
        }

        /// <summary>Hàng thanh trượt: nhãn · thanh · giá trị.</summary>
        public static Slider SliderRow(Transform parent, string label, float min, float max, float value, Func<float, string> fmt, Action<float> onChange)
        {
            var row = Row(parent, 54);
            var l = Label(row, label, 26, Ink, TextAlignmentOptions.MidlineLeft);
            l.rectTransform.anchorMax = new Vector2(0.45f, 1);
            var area = UIFactory.Rect("Slider", row, new Vector2(0.48f, 0.5f), new Vector2(0.86f, 0.5f), new Vector2(0, -12), new Vector2(0, 12));
            var bg = UIFactory.Panel("Bg", area, new Color(1, 1, 1, 0.12f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -2), new Vector2(0, 2));
            var fillArea = UIFactory.Rect("FillArea", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = UIFactory.Panel("Fill", fillArea, Blood, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -2), new Vector2(0, 2));
            var handleArea = UIFactory.Rect("HandleArea", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handle = UIFactory.Panel("Handle", handleArea, Ink, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-5, 0), new Vector2(5, 0));
            var s = area.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.value = value; s.direction = Slider.Direction.LeftToRight;
            var v = Label(row, fmt(value), 24, Dim, TextAlignmentOptions.MidlineRight);
            v.rectTransform.anchorMin = new Vector2(0.88f, 0);
            s.onValueChanged.AddListener(x => { v.text = fmt(x); onChange?.Invoke(x); });
            return s;
        }

        /// <summary>Hàng bộ chọn: nhãn · ‹ giá trị ›.</summary>
        public static Action<int> SelectorRow(Transform parent, string label, string[] options, int index, Action<int> onChange)
        {
            var row = Row(parent, 54);
            var l = Label(row, label, 26, Ink, TextAlignmentOptions.MidlineLeft);
            l.rectTransform.anchorMax = new Vector2(0.45f, 1);
            int cur = Mathf.Clamp(index, 0, Mathf.Max(0, options.Length - 1));
            var valRect = UIFactory.Rect("Value", row, new Vector2(0.48f, 0), new Vector2(1f, 1), Vector2.zero, Vector2.zero);
            var val = Label(valRect, options.Length > 0 ? options[cur] : "", 26, Ink, TextAlignmentOptions.Center);
            void Set(int i) { cur = (i % options.Length + options.Length) % options.Length; val.text = options[cur]; onChange?.Invoke(cur); }
            Arrow(valRect, "‹", new Vector2(0, 0), new Vector2(0.14f, 1), () => { AmbientScares.Play2D(Click, 0.5f); Set(cur - 1); });
            Arrow(valRect, "›", new Vector2(0.86f, 0), new Vector2(1f, 1), () => { AmbientScares.Play2D(Click, 0.5f); Set(cur + 1); });
            return i => { cur = Mathf.Clamp(i, 0, options.Length - 1); val.text = options[cur]; };
        }

        static void Arrow(Transform parent, string glyph, Vector2 aMin, Vector2 aMax, Action onClick)
        {
            var rt = UIFactory.Rect("Arrow", parent, aMin, aMax, Vector2.zero, Vector2.zero);
            rt.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            var t = Label(rt, glyph, 40, Dim, TextAlignmentOptions.Center);
            var fx = rt.gameObject.AddComponent<KHButtonFX>(); fx.Init(t, null, b, false);
            b.onClick.AddListener(() => onClick());
        }

        public static TextMeshProUGUI Header(Transform parent, string text)
        {
            var row = Row(parent, 48);
            var t = Label(row, text.ToUpperInvariant(), 20, Blood, TextAlignmentOptions.BottomLeft, false, true);
            t.characterSpacing = 8f;
            UIFactory.Panel("Line", row, new Color(Blood.r, Blood.g, Blood.b, 0.4f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1));
            return t;
        }
    }

    /// <summary>Hiệu ứng hover nút: đổi màu, trượt, vạch đỏ, âm thanh.</summary>
    public class KHButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshProUGUI label; private RectTransform bar; private Button btn; private bool slide = true;
        private float h, target;
        private Vector2 baseOff;

        public void Init(TextMeshProUGUI l, RectTransform b, Button button, bool slideText = true)
        {
            label = l; bar = b; btn = button; slide = slideText;
            baseOff = label.rectTransform.offsetMin;
        }

        public void OnPointerEnter(PointerEventData e) { if (btn != null && !btn.interactable) return; target = 1; AmbientScares.Play2D(UIKit.Hover, 0.35f); }
        public void OnPointerExit(PointerEventData e) => target = 0;
        private void OnDisable() { h = target = 0; Apply(); }

        private void Update()
        {
            h = Mathf.MoveTowards(h, target, Time.unscaledDeltaTime * 7f);
            Apply();
        }

        private void Apply()
        {
            if (label == null) return;
            bool on = btn == null || btn.interactable;
            label.color = on ? Color.Lerp(UIKit.Ink, new Color(0.95f, 0.25f, 0.2f), h) : new Color(0.35f, 0.33f, 0.31f);
            if (slide) label.rectTransform.offsetMin = baseOff + new Vector2(28f * h, 0);
            if (bar != null) bar.sizeDelta = new Vector2(22f * h, bar.sizeDelta.y);
        }
    }
}
