using System.Collections;
using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Horror
{
    /// <summary>
    /// Đêm giông: mưa trên mái + gió rít (stereo, đục đi khi ở trong nhà), thỉnh thoảng chớp lóe — ánh chớp dùng chính
    /// đèn trăng (đèn Directional duy nhất có bóng) nên hắt qua cửa sổ thành luồng sáng trong sương — rồi sấm rền sau vài nhịp.
    /// Chớp ngay lúc Mrs. Peake đứng trước cửa sổ là khoảnh khắc thấy rõ bóng bà ta.
    /// </summary>
    public class StormDirector : MonoBehaviour
    {
        [SerializeField] private AudioClip rainLoop;
        [SerializeField] private AudioClip windLoop;
        [Tooltip("Clip 0 = sấm gần (nổ xé), còn lại = sấm xa")]
        [SerializeField] private AudioClip[] thunder = new AudioClip[0];
        [Tooltip("Khoảng cách giữa hai lần chớp (giây)")]
        [SerializeField] private Vector2 interval = new Vector2(20f, 52f);
        [SerializeField] private float flashIntensity = 3.2f;

        private AudioSource rain, wind, thunderSrc;
        private AudioLowPassFilter rainLpf, windLpf;
        private Light moon; private float moonBase; private Color moonColor;
        private Color ambientBase;
        private float next, indoor = 1f, nextProbe, inK = 1f;
        private bool flashing;
        private Transform cam;

        public void Configure(AudioClip rainClip, AudioClip windClip, AudioClip[] thunderClips)
        {
            rainLoop = rainClip; windLoop = windClip; thunder = thunderClips;
        }

        private void Start()
        {
            rain = Loop("Rain", rainLoop, out rainLpf);
            wind = Loop("Wind", windLoop, out windLpf);
            var tg = new GameObject("Thunder"); tg.transform.SetParent(transform, false);
            thunderSrc = tg.AddComponent<AudioSource>(); thunderSrc.playOnAwake = false; thunderSrc.spatialBlend = 0f;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.isActiveAndEnabled && (moon == null || l.intensity > moon.intensity)) moon = l;
            if (moon != null) { moonBase = moon.intensity; moonColor = moon.color; }
            ambientBase = RenderSettings.ambientLight;
            next = Time.time + Random.Range(8f, 20f);
        }

        private AudioSource Loop(string n, AudioClip clip, out AudioLowPassFilter lpf)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f;
            lpf = go.AddComponent<AudioLowPassFilter>(); lpf.cutoffFrequency = 22000f;
            if (clip != null) s.Play();
            return s;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool menu = GameFlow.State == FlowState.Menu || GameFlow.State == FlowState.Boot;
            // Trong nhà hay ngoài trời: có mái che phía trên đầu không
            if (Time.unscaledTime >= nextProbe)
            {
                nextProbe = Time.unscaledTime + 0.4f;
                if (cam == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) cam = pc.playerCamera; }
                bool roof = cam != null && Physics.Raycast(cam.position, Vector3.up, 40f, ~0, QueryTriggerInteraction.Ignore);
                indoor = menu ? 0f : (roof ? 1f : 0f);
            }
            inK = Mathf.MoveTowards(inK, indoor, dt * 0.8f);
            rainLpf.cutoffFrequency = Mathf.Lerp(22000f, 1500f, inK); windLpf.cutoffFrequency = Mathf.Lerp(22000f, 2600f, inK);
            float amb = AudioMix.Ambience;
            float rainV = (menu ? 0.5f : Mathf.Lerp(0.85f, 0.42f, inK)) * amb;
            float gust = 0.6f + 0.4f * Mathf.PerlinNoise(Time.unscaledTime * 0.08f, 3.3f);
            float windV = (menu ? 0.35f : Mathf.Lerp(0.6f, 0.34f, inK)) * gust * amb;
            rain.volume = Mathf.MoveTowards(rain.volume, rainV, dt * 0.5f);
            wind.volume = Mathf.MoveTowards(wind.volume, windV, dt * 0.5f);

            if (!GameFlow.IsPlaying || flashing || moon == null) return;
            if (Time.time >= next)
            {
                next = Time.time + Random.Range(interval.x, interval.y);
                StartCoroutine(Lightning());
            }
        }

        private IEnumerator Lightning()
        {
            flashing = true;
            bool close = Random.value < 0.3f;
            float peak = flashIntensity * (close ? 1.25f : Random.Range(0.6f, 1f));
            var flashColor = new Color(0.82f, 0.88f, 1f);
            int pulses = Random.Range(2, 4);
            for (int p = 0; p < pulses; p++)
            {
                float up = Random.Range(0.03f, 0.06f), hold = Random.Range(0.03f, 0.09f), down = Random.Range(0.08f, 0.22f);
                float amp = peak * (p == 0 ? 1f : Random.Range(0.45f, 0.9f));
                yield return Ramp(moonBase, amp, up, flashColor);
                yield return new WaitForSeconds(hold);
                yield return Ramp(amp, moonBase, down, flashColor);
                if (p < pulses - 1) yield return new WaitForSeconds(Random.Range(0.04f, 0.18f));
            }
            SetMoon(moonBase, 0f, flashColor);
            // Sấm đến sau: gần thì nổ xé gần như tức thì, xa thì vài giây
            yield return new WaitForSeconds(close ? Random.Range(0.25f, 0.6f) : Random.Range(1.2f, 3.2f));
            if (thunder != null && thunder.Length > 0 && thunderSrc != null)
            {
                var clip = close ? thunder[0] : thunder[Random.Range(Mathf.Min(1, thunder.Length - 1), thunder.Length)];
                if (clip != null)
                {
                    thunderSrc.pitch = Random.Range(0.9f, 1.06f);
                    thunderSrc.panStereo = Random.Range(-0.5f, 0.5f);
                    thunderSrc.PlayOneShot(clip, (close ? 1f : 0.7f) * Mathf.Lerp(1f, 0.75f, indoor) * AudioMix.Ambience);
                    if (close) CameraShakeSafe(0.25f);
                }
            }
            flashing = false;
        }

        private IEnumerator Ramp(float from, float to, float time, Color flashColor)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(from, to, t / time);
                SetMoon(v, Mathf.InverseLerp(moonBase, flashIntensity, v), flashColor);
                yield return null;
            }
        }

        private void SetMoon(float intensity, float k, Color flashColor)
        {
            if (moon == null) return;
            moon.intensity = intensity;
            moon.color = Color.Lerp(moonColor, flashColor, k);
            RenderSettings.ambientLight = Color.Lerp(ambientBase, new Color(0.10f, 0.12f, 0.17f), k);
        }

        private static void CameraShakeSafe(float amount)
        {
            CameraShake.Shake(0.6f, amount * 0.04f);
        }

        private void OnDisable()
        {
            if (moon != null) { moon.intensity = moonBase; moon.color = moonColor; }
            RenderSettings.ambientLight = ambientBase;
        }
    }
}
