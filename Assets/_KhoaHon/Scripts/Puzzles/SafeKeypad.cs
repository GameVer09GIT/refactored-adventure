using System.Collections;
using KhoaHon.Core;
using KhoaHon.Horror;
using KhoaHon.Interaction;
using KhoaHon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KhoaHon.Puzzles
{
    /// <summary>
    /// Két sắt khóa số 4 vòng ở Phòng Làm Việc. Mã = năm khởi dựng dinh thự (1921, trên ảnh gia tộc).
    /// Mở được thì cửa két xoay ra; nhấn E lần nữa để lấy đồ bên trong (một Pickup không hình, không collider).
    /// Trạng thái suy ra từ cờ SafeOpened + trạng thái nhặt của Pickup.
    /// Điều khiển: A/D hoặc ←/→ chọn vòng, W/S hoặc ↑/↓ (cuộn chuột) đổi số, Enter thử, Esc/E đóng.
    /// </summary>
    public class SafeKeypad : MonoBehaviour, IInteractable, ISaveParticipant
    {
        [SerializeField] private string code = "1921";
        [Tooltip("Bản lề cửa két (xoay quanh trục Y khi mở)")]
        [SerializeField] private Transform door;
        [SerializeField] private float doorOpenAngle = -105f;
        [Tooltip("Đồ trong két: Pickup không có renderer/collider, két gọi Interact() khi người chơi lấy")]
        [SerializeField] private Pickup contents;
        [SerializeField] private AudioClip tickClip, openClip, wrongClip;

        private SafeLock safeLock;
        private GameObject ui;
        private readonly int[] digits = new int[4];
        private int cursor;
        private TextMeshProUGUI[] digitLabels;
        private Image[] digitFrames;
        private TextMeshProUGUI status;
        private float shake;
        private bool opened;

        public void Configure(string newCode, Transform doorHinge, Pickup inside, AudioClip tick, AudioClip open, AudioClip wrong)
        {
            code = newCode; door = doorHinge; contents = inside; tickClip = tick; openClip = open; wrongClip = wrong;
        }

        private void Awake()
        {
            safeLock = new SafeLock(code);
        }

        private bool ContentsTaken => contents == null || contents.Collected;

        public string Prompt => T.Get(opened ? "prompt.safe_take" : "prompt.safe");
        public bool CanInteract() => ui == null && (!opened || !ContentsTaken);

        public void Interact()
        {
            if (ui != null) return;
            if (!opened) { OpenUI(); return; }
            if (!ContentsTaken) contents.Interact();
        }

        // ------------------------------------------------------------------ UI
        private void OpenUI()
        {
            ModalUI.Open(this);
            var dim = UIFactory.Panel("KH_SafeUI", UIKit.TopLayer, new Color(0, 0, 0, 0.78f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ui = dim.gameObject;
            var box = UIFactory.Panel("Box", dim.transform, new Color(0.05f, 0.045f, 0.04f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-430, -250), new Vector2(430, 250));
            UIFactory.Panel("Edge", box.transform, new Color(0.55f, 0.42f, 0.22f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -3), Vector2.zero);
            var title = UIKit.Label(box.transform, T.Get("safe.title"), 40, UIKit.Ink, TextAlignmentOptions.Top, true, true);
            title.rectTransform.offsetMin = new Vector2(30, 0); title.rectTransform.offsetMax = new Vector2(-30, -30);

            digitLabels = new TextMeshProUGUI[4];
            digitFrames = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                float x = (i - 1.5f) * 150f;
                var frame = UIFactory.Panel("Dial" + i, box.transform, new Color(0.12f, 0.1f, 0.08f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 60, -70), new Vector2(x + 60, 90));
                digitFrames[i] = frame;
                var l = UIKit.Label(frame.transform, "0", 96, new Color(0.9f, 0.8f, 0.55f), TextAlignmentOptions.Center, true, true);
                digitLabels[i] = l;
                // Nút ▲ ▼ vẽ bằng ô bấm (font không có ký tự tam giác) — dùng ký tự ^ và v
                Btn(box.transform, "+", new Vector2(x, 130), () => { cursor = idx; Step(1); });
                Btn(box.transform, "-", new Vector2(x, -110), () => { cursor = idx; Step(-1); });
                var b = frame.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { cursor = idx; Refresh(); });
            }
            status = UIKit.Label(box.transform, T.Get("safe.hint"), 22, UIKit.Dim, TextAlignmentOptions.Bottom);
            status.rectTransform.offsetMin = new Vector2(30, 28); status.rectTransform.offsetMax = new Vector2(-30, 0);
            Refresh();
        }

        private void Btn(Transform parent, string glyph, Vector2 pos, System.Action onClick)
        {
            var rt = UIFactory.Rect("Btn" + glyph, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos - new Vector2(44, 22), pos + new Vector2(44, 22));
            rt.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.16f, 0.12f, 0.9f);
            var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.ColorTint;
            UIKit.Label(rt, glyph, 34, UIKit.Ink, TextAlignmentOptions.Center, false, true);
            b.onClick.AddListener(() => onClick());
        }

        private void CloseUI()
        {
            if (ui != null) Destroy(ui);
            ui = null;
            ModalUI.Close(this);
        }

        private void Update()
        {
            if (ui == null || Keyboard.current == null) return;
            var k = Keyboard.current;
            if (k.escapeKey.wasPressedThisFrame || (k.eKey.wasPressedThisFrame && Time.frameCount != ModalUI.LastClosedFrame)) { CloseUI(); return; }
            if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) { cursor = (cursor + 3) % 4; Refresh(); }
            if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) { cursor = (cursor + 1) % 4; Refresh(); }
            if (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) Step(1);
            if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) Step(-1);
            for (int d = 0; d <= 9; d++)
                if (k[(Key)((int)Key.Digit0 + d)].wasPressedThisFrame || k[(Key)((int)Key.Numpad0 + d)].wasPressedThisFrame)
                { digits[cursor] = d; cursor = Mathf.Min(3, cursor + 1); Tick(); Refresh(); }
            var m = Mouse.current;
            if (m != null && Mathf.Abs(m.scroll.ReadValue().y) > 0.1f) Step(m.scroll.ReadValue().y > 0 ? 1 : -1);
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) TryOpen();

            if (shake > 0f)
            {
                shake -= Time.unscaledDeltaTime;
                var box = ui.transform.GetChild(0) as RectTransform;
                if (box != null) box.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 80f) * 12f * shake, 0);
            }
        }

        private void Step(int dir)
        {
            digits[cursor] = (digits[cursor] + dir + 10) % 10;
            Tick();
            Refresh();
        }

        private void Tick() { if (tickClip != null) AmbientScares.Play2D(tickClip, 0.7f); }

        private void Refresh()
        {
            if (digitLabels == null) return;
            for (int i = 0; i < 4; i++)
            {
                digitLabels[i].text = digits[i].ToString();
                digitFrames[i].color = i == cursor ? new Color(0.32f, 0.22f, 0.12f, 1f) : new Color(0.12f, 0.1f, 0.08f, 1f);
            }
        }

        private void TryOpen()
        {
            if (safeLock.Try(SafeLock.FromDigits(digits)))
            {
                CloseUI();
                if (openClip != null) AudioSource.PlayClipAtPoint(openClip, transform.position, 1f);
                GameEvents.RaiseNoise(transform.position, 6f, NoiseType.Safe);
                SetOpened(true, animate: true);
                GameManager.Instance?.SetFlag(StoryFlag.SafeOpened);
                KhoaHonHUD.Subtitle(T.Get("safe.opened"), 4f);
            }
            else
            {
                if (wrongClip != null) AmbientScares.Play2D(wrongClip, 0.8f);
                shake = 0.4f;
                status.text = T.Get(safeLock.Attempts >= 3 ? "safe.wrong_hint" : "safe.wrong");
            }
        }

        private void SetOpened(bool value, bool animate)
        {
            opened = value;
            if (door == null) return;
            if (animate && value) StartCoroutine(SwingDoor());
            else door.localRotation = Quaternion.Euler(0, value ? doorOpenAngle : 0f, 0);
        }

        private IEnumerator SwingDoor()
        {
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float a = Mathf.SmoothStep(0f, doorOpenAngle, t / 1.4f);
                door.localRotation = Quaternion.Euler(0, a, 0);
                yield return null;
            }
        }

        public void Capture(SaveData data) { }

        public void Restore(SaveData data)
        {
            bool open = data.flags.Contains(StoryFlag.SafeOpened.ToString());
            if (open) safeLock.Try(code);
            SetOpened(open, animate: false);
        }
    }
}
