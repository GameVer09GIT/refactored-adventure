using System;
using System.Collections.Generic;
using System.Linq;
using KhoaHon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KhoaHon.UI
{
    /// <summary>Màn Cài đặt 4 tab (Hình ảnh · Âm thanh · Điều khiển · Hiển thị), dùng chung cho menu chính và tạm dừng.</summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        public static SettingsMenuUI Instance { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;

        private GameObject root;
        private readonly List<GameObject> pages = new List<GameObject>();
        private readonly List<TextMeshProUGUI> tabLabels = new List<TextMeshProUGUI>();
        private Action onClose;
        private int tab;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Open(Action closed)
        {
            onClose = closed;
            if (root != null) Destroy(root);
            Build();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (root == null) return;
            KhoaHonSettings.Save();
            Destroy(root); root = null;
            onClose?.Invoke();
        }

        private void Build()
        {
            KhoaHonSettings.Load();
            var bg = UIFactory.Panel("KH_Settings", UIKit.TopLayer, new Color(0, 0, 0, 0.82f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = bg.gameObject;
            var title = UIKit.Label(bg.transform, T.Get("ui.settings"), 64, UIKit.Ink, TextAlignmentOptions.TopLeft, true, true);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.offsetMin = new Vector2(140, -150); title.rectTransform.offsetMax = new Vector2(-140, -60);

            // Tab
            var tabs = UIFactory.Rect("Tabs", bg.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(140, -215), new Vector2(-140, -160));
            var hl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 40; hl.childForceExpandWidth = false; hl.childControlWidth = true;
            string[] names = { T.Get("ui.tab.graphics"), T.Get("ui.tab.audio"), T.Get("ui.tab.controls"), T.Get("ui.tab.display") };
            tabLabels.Clear(); pages.Clear();
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var go = new GameObject("Tab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(tabs, false); go.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                go.GetComponent<LayoutElement>().preferredWidth = 230;
                var l = UIKit.Label(go.transform, names[i].ToUpperInvariant(), 26, UIKit.Dim, TextAlignmentOptions.MidlineLeft, false, true);
                l.characterSpacing = 6f;
                go.GetComponent<Button>().onClick.AddListener(() => { AmbientScaresClick(); ShowTab(idx); });
                tabLabels.Add(l);
            }

            var body = UIFactory.Rect("Body", bg.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(140, 140), new Vector2(-140, -240));
            pages.Add(BuildGraphics(body)); pages.Add(BuildAudio(body)); pages.Add(BuildControls(body)); pages.Add(BuildDisplay(body));

            // Nút dưới
            var bottom = UIFactory.Rect("Bottom", bg.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(140, 50), new Vector2(-140, 110));
            var bl = bottom.gameObject.AddComponent<HorizontalLayoutGroup>(); bl.spacing = 24; bl.childAlignment = TextAnchor.MiddleRight; bl.childForceExpandWidth = false;
            UIKit.SmallButton(bottom, T.Get("ui.defaults"), () => { KhoaHonSettings.ResetDefaults(); KhoaHonSettings.ApplyAll(); Open(onClose); });
            UIKit.SmallButton(bottom, T.Get("ui.apply"), () => { KhoaHonSettings.ApplyAll(); KhoaHonSettings.Save(); KhoaHonHUD.Toast(T.Get("ui.applied")); });
            UIKit.SmallButton(bottom, T.Get("ui.back"), () => { Horror.AmbientScares.Play2D(UIKit.Back, 0.6f); KhoaHonSettings.ApplyAll(); Close(); });
            ShowTab(tab);
        }

        private static void AmbientScaresClick() => Horror.AmbientScares.Play2D(UIKit.Click, 0.5f);

        private void ShowTab(int i)
        {
            tab = i;
            for (int k = 0; k < pages.Count; k++) pages[k].SetActive(k == i);
            for (int k = 0; k < tabLabels.Count; k++) tabLabels[k].color = k == i ? new Color(0.95f, 0.25f, 0.2f) : UIKit.Dim;
        }

        private RectTransform Page(Transform body, string name)
        {
            var col = UIKit.Column(body, name, new Vector2(0, 0), new Vector2(0.62f, 1), Vector2.zero, Vector2.zero, 6);
            return col;
        }

        private GameObject BuildGraphics(Transform body)
        {
            var p = Page(body, "Graphics");
            var S = typeof(KhoaHonSettings);
            UIKit.Header(p, T.Get("ui.h.screen"));
            UIKit.SelectorRow(p, T.Get("ui.screen_mode"), new[] { T.Get("ui.fullscreen"), T.Get("ui.borderless"), T.Get("ui.windowed") }, KhoaHonSettings.ScreenMode, v => KhoaHonSettings.ScreenMode = v);
            var res = KhoaHonSettings.Resolutions();
            int ri = KhoaHonSettings.ResolutionIndex >= 0 ? KhoaHonSettings.ResolutionIndex : Mathf.Max(0, res.FindIndex(r => r.width == Screen.width && r.height == Screen.height));
            UIKit.SelectorRow(p, T.Get("ui.resolution"), res.Select(r => r.width + " × " + r.height).ToArray(), ri, v => KhoaHonSettings.ResolutionIndex = v);
            UIKit.SelectorRow(p, T.Get("ui.vsync"), OnOff(), KhoaHonSettings.VSync ? 0 : 1, v => KhoaHonSettings.VSync = v == 0);
            UIKit.SelectorRow(p, T.Get("ui.fps_cap"), new[] { "30", "60", "120", "144", T.Get("ui.unlimited") }, KhoaHonSettings.FpsCapIndex, v => KhoaHonSettings.FpsCapIndex = v);
            UIKit.Header(p, T.Get("ui.h.quality"));
            var qn = QualitySettings.names;
            UIKit.SelectorRow(p, T.Get("ui.quality"), qn, Mathf.Clamp(KhoaHonSettings.Quality, 0, qn.Length - 1), v => KhoaHonSettings.Quality = v);
            UIKit.SliderRow(p, T.Get("ui.fov"), 60, 100, KhoaHonSettings.Fov, x => Mathf.RoundToInt(x) + "°", x => { KhoaHonSettings.Fov = x; KhoaHonSettings.ApplyLive(); });
            UIKit.SelectorRow(p, T.Get("ui.motion_blur"), OnOff(), KhoaHonSettings.MotionBlur ? 0 : 1, v => { KhoaHonSettings.MotionBlur = v == 0; KhoaHonSettings.ApplyLive(); });
            UIKit.SelectorRow(p, T.Get("ui.film_grain"), OnOff(), KhoaHonSettings.FilmGrain ? 0 : 1, v => { KhoaHonSettings.FilmGrain = v == 0; KhoaHonSettings.ApplyLive(); });
            return p.gameObject;
        }

        private GameObject BuildAudio(Transform body)
        {
            var p = Page(body, "Audio");
            Func<float, string> pct = x => Mathf.RoundToInt(x * 100f) + "%";
            UIKit.Header(p, T.Get("ui.h.volume"));
            UIKit.SliderRow(p, T.Get("ui.vol_master"), 0, 1, KhoaHonSettings.Master, pct, x => { KhoaHonSettings.Master = x; KhoaHonSettings.ApplyLive(); });
            UIKit.SliderRow(p, T.Get("ui.vol_music"), 0, 1, KhoaHonSettings.Music, pct, x => { KhoaHonSettings.Music = x; KhoaHonSettings.ApplyLive(); });
            UIKit.SliderRow(p, T.Get("ui.vol_sfx"), 0, 1, KhoaHonSettings.Sfx, pct, x => { KhoaHonSettings.Sfx = x; KhoaHonSettings.ApplyLive(); });
            UIKit.SliderRow(p, T.Get("ui.vol_amb"), 0, 1, KhoaHonSettings.Ambience, pct, x => { KhoaHonSettings.Ambience = x; KhoaHonSettings.ApplyLive(); });
            UIKit.SliderRow(p, T.Get("ui.vol_voice"), 0, 1, KhoaHonSettings.Voice, pct, x => { KhoaHonSettings.Voice = x; KhoaHonSettings.ApplyLive(); });
            UIKit.Header(p, T.Get("ui.h.subtitles"));
            UIKit.SelectorRow(p, T.Get("ui.subtitles"), OnOff(), KhoaHonSettings.Subtitles ? 0 : 1, v => { KhoaHonSettings.Subtitles = v == 0; KhoaHonSettings.ApplyLive(); });
            var hint = UIKit.Label(UIKit.Row(p, 60), T.Get("ui.headphones"), 20, UIKit.Dim, TextAlignmentOptions.MidlineLeft);
            return p.gameObject;
        }

        private GameObject BuildControls(Transform body)
        {
            var p = Page(body, "Controls");
            UIKit.Header(p, T.Get("ui.h.mouse"));
            UIKit.SliderRow(p, T.Get("ui.sensitivity"), 2, 40, KhoaHonSettings.Sensitivity, x => (x / 15f).ToString("0.00"), x => { KhoaHonSettings.Sensitivity = x; KhoaHonSettings.ApplyLive(); });
            UIKit.Header(p, T.Get("ui.h.keys"));
            string[,] keys =
            {
                { "ui.key.move", "W A S D" }, { "ui.key.run", "Shift" }, { "ui.key.interact", "E" }, { "ui.key.notebook", "Tab / J" },
                { "ui.key.hotbar", T.Get("ui.keyname.hotbar") }, { "ui.key.use", T.Get("ui.keyname.lmb") }, { "ui.key.breath", T.Get("ui.keyname.hold_space") }, { "ui.key.pause", "Esc" },
            };
            for (int i = 0; i < keys.GetLength(0); i++)
            {
                var row = UIKit.Row(p, 44);
                var a = UIKit.Label(row, T.Get(keys[i, 0]), 24, UIKit.Ink, TextAlignmentOptions.MidlineLeft); a.rectTransform.anchorMax = new Vector2(0.5f, 1);
                var b = UIKit.Label(row, keys[i, 1], 24, UIKit.Dim, TextAlignmentOptions.MidlineRight); b.rectTransform.anchorMin = new Vector2(0.5f, 0);
                b.font = UIKit.Style != null ? UIKit.Style.sansBoldFont ?? UIKit.Style.sansFont : b.font;
            }
            return p.gameObject;
        }

        private GameObject BuildDisplay(Transform body)
        {
            var p = Page(body, "Display");
            UIKit.Header(p, T.Get("ui.h.brightness"));
            UIKit.SliderRow(p, T.Get("ui.brightness"), -1.5f, 1.5f, KhoaHonSettings.Brightness, x => (x >= 0 ? "+" : "") + x.ToString("0.0"), x => { KhoaHonSettings.Brightness = x; KhoaHonSettings.ApplyLive(); });
            UIKit.Header(p, T.Get("ui.h.hud"));
            UIKit.SelectorRow(p, T.Get("ui.show_fps"), OnOff(), KhoaHonSettings.ShowFps ? 0 : 1, v => { KhoaHonSettings.ShowFps = v == 0; KhoaHonSettings.ApplyLive(); });
            // Đổi ngôn ngữ: áp dụng ngay và dựng lại bảng Cài đặt bằng ngôn ngữ mới
            UIKit.SelectorRow(p, T.Get("ui.language"), Loc.DisplayNames, (int)Loc.Current, v =>
            {
                Loc.Set((Language)v);
                Open(onClose);
            });
            return p.gameObject;
        }

        private static string[] OnOff() => new[] { T.Get("ui.on"), T.Get("ui.off") };
    }
}
