using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Horror
{
    /// <summary>
    /// Bụi lơ lửng trong không khí quanh người chơi. Hạt dùng shader có nhận sáng nên chỉ hiện lên khi lọt vào luồng đèn pin /
    /// vệt trăng qua cửa sổ — chi tiết nhỏ làm căn nhà bỏ hoang "thật" hẳn lên. Hệ hạt đi theo camera, mô phỏng trong World.
    /// </summary>
    public class DustMotes : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private int maxParticles = 320;
        [Tooltip("Bật/tắt từ Cài đặt đồ họa")]
        public static bool Enabled = true;

        private ParticleSystem ps;
        private Transform cam;

        public void Configure(Material m) => material = m;

        private void LateUpdate()
        {
            if (cam == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc == null || pc.playerCamera == null) return;
                cam = pc.playerCamera;
                Build();
            }
            if (ps == null) return;
            transform.position = cam.position + cam.forward * 1.5f;
            bool on = Enabled && GameFlow.State != FlowState.Menu && GameFlow.State != FlowState.Boot;
            var em = ps.emission;
            if (em.enabled != on) em.enabled = on;
        }

        private void Build()
        {
            if (material == null) return;
            var go = new GameObject("KH_DustMotes");
            go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.duration = 10f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.005f, 0.03f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.014f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.96f, 0.88f, 0.55f), new Color(0.85f, 0.9f, 1f, 0.9f));
            main.maxParticles = maxParticles; main.gravityModifier = 0.0015f;
            var em = ps.emission; em.rateOverTime = maxParticles / 10f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(6f, 3.2f, 6f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.035f; noise.frequency = 0.35f; noise.scrollSpeed = 0.08f; noise.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false; r.minParticleSize = 0f; r.maxParticleSize = 0.05f;
            ps.Play();
        }
    }
}
