using System;
using KhoaHon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KhoaHon.AI
{
    /// <summary>
    /// Mini-game nín thở khi bà Quản Gia đứng sát chỗ trốn: giữ Space, thanh hơi cạn dần.
    /// Thả sớm hoặc cạn hơi → thất bại (bị lôi ra). Giữ tới khi bà bỏ đi → thoát.
    /// </summary>
    public class BreathHold : MonoBehaviour
    {
        public static BreathHold Instance { get; private set; }

        [Tooltip("Thời gian ân hạn để bắt đầu giữ Space (s)")]
        [SerializeField] private float graceTime = 0.9f;
        [SerializeField] private AudioClip gaspClip;

        public bool Active { get; private set; }
        public float Breath01 => breath;

        private float duration = 7f, breath = 1f, started;
        private bool holding;
        private Action onFail;
        private GameObject ui; private Image fill; private CanvasGroup group;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start() => Build();

        private void Build()
        {
            var hud = KhoaHonHUD.Instance;
            if (hud == null) return;
            var back = UIFactory.Panel("KH_BreathHold", hud.Root, new Color(0, 0, 0, 0.45f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-180, 110), new Vector2(180, 122));
            ui = back.gameObject;
            group = ui.AddComponent<CanvasGroup>();
            fill = UIFactory.Panel("Fill", back.transform, new Color(0.75f, 0.78f, 0.82f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ui.SetActive(false);
        }

        public void Begin(float seconds, Action failCallback)
        {
            if (Active) return;
            duration = Mathf.Max(1f, seconds);
            breath = 1f; holding = false; started = Time.time;
            onFail = failCallback;
            Active = true;
            PlayerState.IsHoldingBreath = true;
            if (ui != null) ui.SetActive(true);
            KhoaHonHUD.Prompt(T.Get("prompt.hold_breath"));
        }

        /// <summary>Bà đã bỏ đi: kết thúc thành công.</summary>
        public void End()
        {
            if (!Active) return;
            Active = false;
            PlayerState.IsHoldingBreath = false;
            if (ui != null) ui.SetActive(false);
            KhoaHonHUD.Prompt(PlayerState.IsHiding ? T.Get("prompt.hide_exit") : "");
        }

        private void Update()
        {
            if (!Active) return;
            bool down = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            if (down) holding = true;
            if (holding)
            {
                if (!down) { Fail(); return; }
                breath -= Time.deltaTime / duration;
                if (breath <= 0f) { Fail(); return; }
            }
            else if (Time.time - started > graceTime) { Fail(); return; }
            if (fill != null) fill.rectTransform.localScale = new Vector3(Mathf.Clamp01(breath), 1f, 1f);
            if (group != null) group.alpha = 0.35f + 0.65f * (1f - breath);
        }

        private void Fail()
        {
            var cb = onFail;
            End();
            if (gaspClip != null) AudioSource.PlayClipAtPoint(gaspClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 1f);
            cb?.Invoke();
        }
    }
}
