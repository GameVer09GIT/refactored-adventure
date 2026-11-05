using KhoaHon.AI;
using KhoaHon.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Hậu kỳ động: màu u ám, nhiễu phim, viền tối; khi bà Quản Gia lại gần → quang sai, méo ống kính, nhiễu dày;
    /// khi bị truy đuổi → ửng đỏ theo nhịp tim; lý trí thấp → giảm bão hòa. Độ sáng/nhiễu/blur lấy từ cài đặt.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class HorrorPostFX : MonoBehaviour
    {
        public static HorrorPostFX Instance { get; private set; }

        [SerializeField] private float nearDistance = 14f;
        [SerializeField] private AudioSource heartbeat;

        public float Brightness { get; set; }
        public bool FilmGrainEnabled { get; set; } = true;
        public bool MotionBlurEnabled { get; set; } = true;

        private Volume vol;
        private Vignette vignette; private ChromaticAberration chroma; private FilmGrain grain;
        private LensDistortion lens; private ColorAdjustments color; private MotionBlur blur;
        private float prox, hunt, flash;
        private Transform cam;

        private void Awake()
        {
            Instance = this;
            vol = GetComponent<Volume>();
            var p = vol.profile; // bản sao runtime, không sửa asset
            p.TryGet(out vignette); p.TryGet(out chroma); p.TryGet(out grain);
            p.TryGet(out lens); p.TryGet(out color); p.TryGet(out blur);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Chớp đỏ ngắn (jumpscare, bị phát hiện).</summary>
        public void Flash(float amount) => flash = Mathf.Max(flash, amount);

        private void Update()
        {
            if (cam == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) cam = pc.playerCamera; }
            var hk = HousekeeperAI.Instance;
            float targetProx = 0f, targetHunt = 0f;
            if (hk != null && cam != null && GameFlow.IsPlaying)
            {
                var d = hk.transform.position - cam.position;
                float dist = d.magnitude + Mathf.Abs(d.y) * 1.5f;
                targetProx = Mathf.Clamp01(1f - dist / nearDistance);
                if (hk.State == HousekeeperState.Hunt || hk.State == HousekeeperState.Kill) targetHunt = 1f;
            }
            prox = Mathf.MoveTowards(prox, targetProx, Time.unscaledDeltaTime * 0.9f);
            hunt = Mathf.MoveTowards(hunt, targetHunt, Time.unscaledDeltaTime * (targetHunt > hunt ? 2f : 0.5f));
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.2f);
            float sanity01 = GameManager.Instance != null ? GameManager.Instance.Sanity / 100f : 1f;
            float pulse = hunt * (0.5f + 0.5f * Mathf.Sin(Time.time * 15.5f));
            float jitter = prox * prox * (Mathf.PerlinNoise(Time.time * 9f, 0.3f) - 0.5f);

            if (vignette != null) { vignette.intensity.value = 0.32f + prox * 0.22f + hunt * 0.08f + flash * 0.2f; vignette.color.value = Color.Lerp(Color.black, new Color(0.35f, 0f, 0f), hunt * 0.6f + flash); }
            if (chroma != null) chroma.intensity.value = 0.12f + prox * 0.75f + flash * 0.5f;
            if (grain != null) { grain.active = FilmGrainEnabled; grain.intensity.value = 0.32f + prox * 0.5f + (1f - sanity01) * 0.3f; }
            if (lens != null) lens.intensity.value = -0.06f - prox * 0.3f + jitter * 0.25f - flash * 0.2f;
            // Cảnh nền menu: camera đứng xa trong đêm → sáng hơn, không nhòe chuyển động
            bool menu = GameFlow.State == FlowState.Menu || GameFlow.State == FlowState.Boot;
            if (color != null)
            {
                color.postExposure.value = Brightness - 0.15f + (menu ? 1.1f : 0f);
                color.saturation.value = -16f - prox * 25f - (1f - sanity01) * 25f; // giữ màu đỏ của máu
                color.contrast.value = 18f + prox * 12f;
                color.colorFilter.value = Color.Lerp(new Color(0.86f, 0.93f, 0.95f), new Color(1f, 0.55f, 0.52f), Mathf.Clamp01(pulse * 0.55f + flash));
            }
            if (blur != null) blur.active = MotionBlurEnabled && !menu;
            if (heartbeat != null)
            {
                float hv = Mathf.Clamp01(prox * 1.3f + hunt * 0.4f) * AudioMix.Sfx;
                heartbeat.volume = Mathf.MoveTowards(heartbeat.volume, hv, Time.unscaledDeltaTime);
                heartbeat.pitch = 0.9f + hunt * 0.35f;
                if (heartbeat.volume > 0.01f && !heartbeat.isPlaying) heartbeat.Play();
            }
        }
    }
}
