using HorrorGame.Inventory;
using KhoaHon.AI;
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
        private Image glare; private float glareAlpha; // vòng 'chói mắt' quanh tâm ngắm khi rọi đèn vào mặt Mrs. Peake
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
            var gr = UIFactory.Rect("Glare", center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-26, -26), new Vector2(26, 26));
            glare = gr.gameObject.AddComponent<Image>();
            glare.sprite = RingSprite(); glare.type = Image.Type.Filled; glare.fillMethod = Image.FillMethod.Radial360;
            glare.fillOrigin = (int)Image.Origin360.Top; glare.fillClockwise = true; glare.raycastTarget = false;
            glare.color = new Color(1, 1, 1, 0);
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

        /// <summary>Vòng tròn mảnh (viền mềm) dựng bằng code — font/sprite có sẵn không có hình này.</summary>
        private static Sprite RingSprite()
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[S * S];
            float c = (S - 1) * 0.5f, rOut = S * 0.47f, rIn = S * 0.40f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(rOut - d) * Mathf.Clamp01(d - rIn);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Đầy dần khi tia đèn trúng mặt bà; đỏ nhấp nháy khi bà đang nhờn đèn (rọi vô dụng).</summary>
        private void UpdateGlare()
        {
            if (glare == null) return;
            var hk = HousekeeperAI.Instance;
            float fill = 0f, alpha = 0f; Color col = new Color(1f, 0.95f, 0.8f);
            if (hk != null && GameFlow.IsPlaying)
            {
                if (hk.BlindedStun) { fill = 1f; alpha = 0.9f; col = Color.white; }
                else if (hk.BeingLit && hk.LightImmune) { fill = 1f; alpha = 0.35f + 0.3f * Mathf.Sin(Time.unscaledTime * 10f); col = UIKit.Blood; }
                else if (hk.LightExposure01 > 0.001f) { fill = hk.LightExposure01; alpha = hk.BeingLit ? 0.85f : 0.4f; }
            }
            glareAlpha = Mathf.MoveTowards(glareAlpha, alpha, Time.unscaledDeltaTime * 4f);
            if (alpha > 0f) glare.fillAmount = fill;
            col.a = glareAlpha;
            glare.color = col;
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

            UpdateGlare();

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
