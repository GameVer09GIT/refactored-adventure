using HorrorGame.Inventory;
using KhoaHon.Core;
using KhoaHon.Interaction;
using KhoaHon.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KhoaHon.UI
{
    /// <summary>HUD tối giản kiểu game kinh dị: chấm tâm (phóng to khi nhắm vật tương tác), pin/phim kín đáo, bộ đếm FPS.</summary>
    public class HudExtras : MonoBehaviour
    {
        public static bool ShowFps;

        private Image dot, ring; private TextMeshProUGUI fps, tool;
        private CanvasGroup toolGroup;
        private Image[] cells; // 5 ô pin (font không có ký tự khối nên vẽ bằng Image)
        private PlayerInteractor interactor;
        private float fpsAcc; private int fpsFrames; private float fpsT;

        private void Start()
        {
            var hud = KhoaHonHUD.Instance;
            if (hud == null) return;
            var center = UIFactory.Rect("Crosshair", hud.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-12, -12), new Vector2(12, 12));
            ring = UIFactory.Panel("Ring", center, new Color(1, 1, 1, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-9, -9), new Vector2(9, 9));
            dot = UIFactory.Panel("Dot", center, new Color(1, 1, 1, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-2, -2), new Vector2(2, 2));
            var fr = UIFactory.Rect("FPS", hud.Root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-220, -60), new Vector2(-20, -20));
            fps = UIKit.Label(fr, "", 20, new Color(0.6f, 0.9f, 0.6f, 0.8f), TextAlignmentOptions.TopRight);
            var tr = UIFactory.Rect("Tool", hud.Root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-420, 30), new Vector2(-40, 80));
            toolGroup = tr.gameObject.AddComponent<CanvasGroup>();
            tool = UIKit.Label(tr, "", 22, UIKit.Dim, TextAlignmentOptions.BottomRight);
            tool.rectTransform.offsetMax = new Vector2(-120, 0);
            cells = new Image[5];
            for (int i = 0; i < 5; i++)
                cells[i] = UIFactory.Panel("Cell" + i, tr, UIKit.Dim, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-110 + i * 22, 6), new Vector2(-94 + i * 22, 24));
        }

        private void Update()
        {
            if (dot == null) return;
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
            bool target = interactor != null && interactor.Current != null && GameFlow.IsPlaying;
            float s = target ? 4f : 2f;
            dot.rectTransform.offsetMin = new Vector2(-s, -s); dot.rectTransform.offsetMax = new Vector2(s, s);
            dot.color = new Color(1, 1, 1, PlayerState.IsHiding || !GameFlow.IsPlaying ? 0f : (target ? 0.9f : 0.4f));
            ring.color = new Color(0.9f, 0.2f, 0.15f, target ? 0.35f : 0f);

            fpsAcc += Time.unscaledDeltaTime; fpsFrames++;
            if (fpsAcc >= 0.5f) { fps.text = ShowFps ? Mathf.RoundToInt(fpsFrames / fpsAcc) + " FPS" : ""; fpsAcc = 0; fpsFrames = 0; }

            // Pin / phim: chỉ hiện khi đang cầm đồ dùng tài nguyên
            var hb = HotbarInventory.Instance;
            string txt = ""; int bars = -1;
            if (hb != null && hb.CurrentItem != null && GameFlow.IsPlaying)
            {
                var type = hb.CurrentItem.ItemType;
                if ((type == ItemType.Flashlight || type == ItemType.Recorder) && BatteryPool.Instance != null)
                {
                    float b = BatteryPool.Instance.Current;
                    bars = Mathf.CeilToInt(b / 20f);
                    txt = T.Get("hud.battery");
                }
                else if (type == ItemType.FilmCamera && FilmStock.Instance != null)
                    txt = T.Get("hud.film") + "  " + FilmStock.Instance.Current;
            }
            tool.text = txt;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].enabled = bars >= 0;
                cells[i].color = i < bars ? (bars <= 1 ? UIKit.Blood : UIKit.Ink) : new Color(0.2f, 0.19f, 0.18f, 0.8f);
            }
            toolGroup.alpha = Mathf.MoveTowards(toolGroup.alpha, string.IsNullOrEmpty(txt) ? 0f : 0.85f, Time.deltaTime * 3f);
        }
    }
}
