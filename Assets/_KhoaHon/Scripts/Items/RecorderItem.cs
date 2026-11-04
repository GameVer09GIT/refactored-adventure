using HorrorGame.Inventory;
using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>
    /// Máy ghi âm cassette (Slot 2). Phase 1: cầm được, chuột trái bật/tắt chế độ nghe (đèn LED + hao pin).
    /// Phase 3 bổ sung EVP định hướng, lời thì thầm và phát băng.
    /// </summary>
    public class RecorderItem : HeldItem
    {
        [SerializeField] private Renderer ledRenderer;
        [SerializeField] private Color ledOn = new Color(1.6f, 0.1f, 0.05f);
        [SerializeField] private AudioClip toggleClip;

        private bool listening;
        private Material ledMat;

        public bool IsListening => listening && isEquipped;

        protected override void Awake()
        {
            base.Awake();
            if (ledRenderer != null)
            {
                ledMat = ledRenderer.material;
                ledMat.EnableKeyword("_EMISSION");
                ApplyLed();
            }
        }

        public override void OnPrimaryUse()
        {
            if (!listening && BatteryPool.Instance != null && BatteryPool.Instance.IsEmpty) return;
            listening = !listening;
            PlayOneShotSound(toggleClip, 0.8f);
            ApplyLed();
        }

        public override void OnUnequip()
        {
            listening = false;
            ApplyLed();
            base.OnUnequip();
        }

        private void Update()
        {
            if (!IsListening) return;
            var cfg = KhoaHon.Core.GameManager.Instance != null ? KhoaHon.Core.GameManager.Instance.Tools : null;
            var pool = BatteryPool.Instance;
            if (cfg == null || pool == null) return;
            pool.Drain(cfg.recorderDrainPerSecond * Time.deltaTime);
            if (pool.IsEmpty) { listening = false; ApplyLed(); }
        }

        private void ApplyLed()
        {
            if (ledMat != null) ledMat.SetColor("_EmissionColor", listening ? ledOn : Color.black);
        }
    }
}
