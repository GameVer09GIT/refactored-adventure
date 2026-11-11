using HorrorGame.Inventory;
using KhoaHon.Core;
using KhoaHon.Items;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Máy quay nhìn đêm của Alex (slot 3). Chuột trái bật/tắt: khuếch đại sáng + tông xanh + nhiễu hạt,
    /// đèn hồng ngoại rọi trước mặt, hiện các dấu vết <see cref="HiddenInk"/>. Tiêu hao pin dự phòng.
    /// </summary>
    public class NightVisionItem : HeldItem
    {
        [SerializeField] private AudioClip toggleClip;

        private bool on;
        private Volume volume;
        private Light irLight;

        public bool IsOn => on && isEquipped;

        public override void OnPrimaryUse()
        {
            if (!on && BatteryPool.Instance != null && BatteryPool.Instance.IsEmpty) { KhoaHonHUD.Toast(T.Get("toast.no_battery")); return; }
            Set(!on);
            PlayOneShotSound(toggleClip, 0.8f);
        }

        public override void OnUnequip()
        {
            Set(false);
            base.OnUnequip();
        }

        public override void OnPlayerDied()
        {
            Set(false);
            base.OnPlayerDied();
        }

        private void OnDisable() { if (on) Set(false); }

        private void Set(bool value)
        {
            on = value;
            EnsureFx();
            if (volume != null) volume.weight = on ? 1f : 0f;
            if (irLight != null) irLight.enabled = on;
            HiddenInk.SetAll(on);
        }

        private void Update()
        {
            if (!IsOn) return;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            var pool = BatteryPool.Instance;
            if (cfg != null && pool != null)
            {
                pool.Drain(cfg.nightVisionDrainPerSecond * Time.deltaTime);
                if (pool.IsEmpty) { Set(false); KhoaHonHUD.Toast(T.Get("toast.no_battery")); }
            }
        }

        private void EnsureFx()
        {
            if (volume == null)
            {
                var go = new GameObject("KH_NightVisionVolume");
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true; volume.priority = 120; volume.weight = 0f;
                var prof = ScriptableObject.CreateInstance<VolumeProfile>();
                var ca = prof.Add<ColorAdjustments>(true);
                ca.postExposure.value = 2.6f; ca.saturation.value = -100f; ca.contrast.value = 24f;
                // Nhuộm xanh sau bước khử bão hòa (Lift/Gamma/Gain chạy sau Saturation)
                var lgg = prof.Add<LiftGammaGain>(true);
                lgg.lift.value = new Vector4(0.2f, 0.45f, 0.22f, -0.05f);
                lgg.gamma.value = new Vector4(0.7f, 1.15f, 0.72f, 0f);
                lgg.gain.value = new Vector4(0.55f, 1.25f, 0.6f, 0f);
                var fg = prof.Add<FilmGrain>(true);
                fg.type.value = FilmGrainLookup.Thin2; fg.intensity.value = 0.85f; fg.response.value = 0.2f;
                var vg = prof.Add<Vignette>(true);
                vg.intensity.value = 0.55f; vg.smoothness.value = 0.35f; vg.color.value = Color.black;
                var bl = prof.Add<Bloom>(true);
                bl.intensity.value = 1.4f; bl.threshold.value = 0.6f;
                var chroma = prof.Add<ChromaticAberration>(true);
                chroma.intensity.value = 0.35f;
                volume.sharedProfile = prof;
            }
            if (irLight == null && player != null && player.playerCamera != null)
            {
                var go = new GameObject("KH_IRLight");
                go.transform.SetParent(player.playerCamera, false);
                irLight = go.AddComponent<Light>();
                irLight.type = LightType.Spot; irLight.spotAngle = 95f; irLight.innerSpotAngle = 40f;
                irLight.range = 14f; irLight.intensity = 1.6f; irLight.color = Color.white; irLight.shadows = LightShadows.None;
                irLight.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (volume != null) Destroy(volume.gameObject);
            if (irLight != null) Destroy(irLight.gameObject);
            HiddenInk.SetAll(false);
        }
    }
}
