using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KhoaHon.Core
{
    /// <summary>Hàm tiện ích dựng UI bằng code (uGUI + TMP) với font tiếng Việt.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, float inset = 0f) =>
            Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));

        public static Image Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, UIStyle style, bool serif, float size, Color color, TextAlignmentOptions align)
        {
            var rt = Stretch(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null) t.font = serif ? style.serifFont : style.sansFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, UIStyle style, string label, float size, Color bg, Color fg)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.92f, 0.8f, 1f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.6f, 1f);
            btn.colors = colors;
            var t = Text("Label", rt, style, false, size, fg, TextAlignmentOptions.Center);
            t.text = label;
            return btn;
        }

        /// <summary>Tạo Canvas overlay riêng với CanvasScaler 1920×1080.</summary>
        public static Canvas Canvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }
    }
}
