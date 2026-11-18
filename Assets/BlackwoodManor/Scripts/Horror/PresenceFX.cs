using Blackwood.Inventory;
using Blackwood.AI;
using Blackwood.Core;
using Blackwood.Items;
using UnityEngine;

namespace Blackwood.Horror
{
    /// <summary>
    /// "Hiện tượng" báo Mrs. Peake đang ở gần mà chưa thấy bạn: đèn pin nhiễu chập chờn kèm tiếng rè điện, hơi thở người chơi
    /// hóa khói lạnh. Càng gần càng dày. Tắt khi bà đang truy đuổi / đang bị rọi đèn (không phá cơ chế tự vệ bằng đèn pin).
    /// Chỉ dùng API chập chờn của đèn pin (SetFlicker — không đổi trạng thái bật/tắt) — không sửa đèn, IK hay tư thế cầm.
    /// </summary>
    public class PresenceFX : MonoBehaviour
    {
        [Tooltip("Bán kính bắt đầu có hiện tượng (m)")]
        [SerializeField] private float radius = 9f;
        [SerializeField] private AudioClip buzzClip;
        [Tooltip("Vật liệu hạt mềm có nhận sáng (khói hơi thở)")]
        [SerializeField] private Material vaporMaterial;

        /// <summary>Mức hiện diện 0..1 (HUD / âm nhạc đọc được).</summary>
        public static float Level { get; private set; }

        private PlayerController player;
        private Transform cam;
        private FlashlightItem flashlight;
        private AudioSource buzz;
        private ParticleSystem vapor;
        private float nextFlick, restoreAt = -1f, nextBreath;

        public void Configure(AudioClip buzzSfx, Material vaporMat) { buzzClip = buzzSfx; vaporMaterial = vaporMat; }

        private void Start()
        {
            buzz = gameObject.AddComponent<AudioSource>();
            buzz.playOnAwake = false; buzz.spatialBlend = 0f;
        }

        private void OnDisable() { Level = 0f; }

        private void Update()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                cam = player.playerCamera;
                flashlight = player.GetComponentInChildren<FlashlightItem>(true);
                BuildVapor();
            }

            // Trả đèn về trạng thái bật sau một nhịp chập chờn
            if (restoreAt > 0f && Time.time >= restoreAt)
            {
                restoreAt = -1f;
                if (flashlight != null) flashlight.SetFlicker(false);
            }

            float target = 0f;
            var hk = HousekeeperAI.Instance;
            if (hk != null && GameFlow.IsPlaying && cam != null && !PlayerState.IsHiding)
            {
                var st = hk.State;
                bool quiet = st == HousekeeperState.Patrol || st == HousekeeperState.Suspicious || st == HousekeeperState.Inspect;
                if (quiet && !hk.BeingLit)
                {
                    var d = hk.transform.position - cam.position;
                    float dist = new Vector2(d.x, d.z).magnitude + Mathf.Abs(d.y) * 1.2f; // khác tầng thì yếu đi
                    target = Mathf.Clamp01(1f - dist / radius);
                }
            }
            Level = Mathf.MoveTowards(Level, target, Time.deltaTime * (target > Level ? 0.8f : 0.4f));
            if (Level < 0.12f || !GameFlow.IsPlaying) return;

            // Đèn pin nhiễu: tắt 40–140 ms rồi bật lại
            if (restoreAt < 0f && Time.time >= nextFlick && flashlight != null && flashlight.IsOn && flashlight.IsEquipped)
            {
                nextFlick = Time.time + Mathf.Lerp(3.2f, 0.4f, Level) * Random.Range(0.6f, 1.5f);
                flashlight.SetFlicker(true);
                restoreAt = Time.time + Random.Range(0.04f, 0.14f) * (Random.value < 0.2f ? 3f : 1f);
                if (buzzClip != null) { buzz.pitch = Random.Range(0.85f, 1.2f); buzz.PlayOneShot(buzzClip, 0.4f * Level * AudioMix.Sfx); }
            }

            // Hơi thở hóa khói (lạnh đột ngột)
            if (vapor != null && Level > 0.3f && Time.time >= nextBreath)
            {
                nextBreath = Time.time + Random.Range(2.6f, 3.6f);
                vapor.Emit(Mathf.RoundToInt(Mathf.Lerp(5f, 12f, Level)));
            }
        }

        private void BuildVapor()
        {
            if (cam == null || vaporMaterial == null || vapor != null) return;
            var go = new GameObject("BW_BreathVapor");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, -0.11f, 0.22f);
            vapor = go.AddComponent<ParticleSystem>();
            vapor.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = vapor.main;
            main.playOnAwake = false; main.loop = false; main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(0.85f, 0.9f, 1f, 0.22f);
            main.maxParticles = 60; main.gravityModifier = -0.02f;
            var em = vapor.emission; em.enabled = false;
            var sh = vapor.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 0.015f;
            var sz = vapor.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3.2f));
            var col = vapor.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var lim = vapor.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.12f; lim.limit = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = vaporMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
