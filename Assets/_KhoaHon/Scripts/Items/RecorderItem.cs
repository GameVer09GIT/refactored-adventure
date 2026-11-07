using HorrorGame.Inventory;
using KhoaHon.AI;
using KhoaHon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KhoaHon.Items
{
    /// <summary>
    /// Máy ghi âm cassette định hướng của Evelyn (slot 2). Chuột trái bật/tắt nghe (LED, hao pin).
    /// Khi nghe: bắt tiếng chìa khóa/bước chân của Mrs. Peake trong tầm evpRange, phát qua tai nghe stereo
    /// (pan trái/phải theo hướng, to dần khi gần) và hiện la bàn EVP trên HUD. Thỉnh thoảng bắt được giọng EVP.
    /// </summary>
    public class RecorderItem : HeldItem
    {
        [SerializeField] private Renderer ledRenderer;
        [SerializeField] private Color ledOn = new Color(1.6f, 0.1f, 0.05f);
        [SerializeField] private AudioClip toggleClip;
        [SerializeField] private AudioClip evpLoop;
        [SerializeField] private AudioClip staticLoop;

        private bool listening;
        private Material ledMat;
        private AudioSource evpSource, staticSource;
        private RectTransform compass, marker;
        private Image markerImg;
        private float nextEvpLine;

        public bool IsListening => listening && isEquipped;

        public void ConfigureAudio(AudioClip toggle, AudioClip evp, AudioClip stat) { toggleClip = toggle; evpLoop = evp; staticLoop = stat; }

        protected override void Awake()
        {
            base.Awake();
            if (ledRenderer != null)
            {
                ledMat = ledRenderer.material;
                ledMat.EnableKeyword("_EMISSION");
                ApplyLed();
            }
            evpSource = gameObject.AddComponent<AudioSource>(); evpSource.loop = true; evpSource.spatialBlend = 0f; evpSource.playOnAwake = false;
            staticSource = gameObject.AddComponent<AudioSource>(); staticSource.loop = true; staticSource.spatialBlend = 0f; staticSource.playOnAwake = false;
        }

        public override void OnPrimaryUse()
        {
            if (!listening && BatteryPool.Instance != null && BatteryPool.Instance.IsEmpty) return;
            SetListening(!listening);
            PlayOneShotSound(toggleClip, 0.8f);
        }

        public override void OnUnequip()
        {
            SetListening(false);
            base.OnUnequip();
        }

        private void SetListening(bool on)
        {
            listening = on;
            ApplyLed();
            if (on)
            {
                if (evpLoop != null) { evpSource.clip = evpLoop; evpSource.volume = 0f; evpSource.Play(); }
                if (staticLoop != null) { staticSource.clip = staticLoop; staticSource.volume = 0.12f * AudioMix.Sfx; staticSource.Play(); }
                nextEvpLine = Time.time + Random.Range(8f, 16f);
            }
            else { evpSource.Stop(); staticSource.Stop(); }
            EnsureCompass();
            if (compass != null) compass.gameObject.SetActive(on);
        }

        private void Update()
        {
            if (!IsListening) { if (compass != null && compass.gameObject.activeSelf) compass.gameObject.SetActive(false); return; }
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            var pool = BatteryPool.Instance;
            if (cfg != null && pool != null)
            {
                pool.Drain(cfg.recorderDrainPerSecond * Time.deltaTime);
                if (pool.IsEmpty) { SetListening(false); return; }
            }
            TrackHousekeeper(cfg != null ? cfg.evpRange : 30f);
        }

        /// <summary>Hướng & khoảng cách tới Mrs. Peake → pan stereo, âm lượng, vị trí điểm đỏ trên la bàn.</summary>
        private void TrackHousekeeper(float range)
        {
            var hk = HousekeeperAI.Instance;
            var cam = player != null ? player.playerCamera : null;
            if (hk == null || cam == null || !hk.gameObject.activeInHierarchy) { evpSource.volume = 0f; if (marker != null) marker.gameObject.SetActive(false); return; }
            var to = hk.transform.position - cam.position;
            float dist = to.magnitude;
            float strength = Mathf.Clamp01(1f - dist / range);
            var flat = new Vector3(to.x, 0f, to.z);
            var fwd = new Vector3(cam.forward.x, 0f, cam.forward.z);
            float angle = Vector3.SignedAngle(fwd, flat, Vector3.up); // + = bên phải
            evpSource.panStereo = Mathf.Clamp(Mathf.Sin(angle * Mathf.Deg2Rad), -1f, 1f);
            evpSource.volume = Mathf.MoveTowards(evpSource.volume, strength * strength * 0.9f * AudioMix.Sfx, Time.deltaTime * 2f);
            // Nhiễu rè to hơn khi bà ta ở gần (thiết bị bị nhiễu)
            staticSource.volume = (0.1f + strength * 0.35f) * AudioMix.Sfx;

            if (marker != null)
            {
                marker.gameObject.SetActive(strength > 0f);
                float r = 58f;
                marker.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * r, Mathf.Cos(angle * Mathf.Deg2Rad) * r);
                markerImg.color = new Color(0.9f, 0.12f, 0.08f, 0.35f + strength * 0.65f);
                marker.localScale = Vector3.one * (0.7f + strength * 0.8f);
            }

            // Thỉnh thoảng bắt được giọng EVP (rõ hơn khi lý trí thấp)
            if (Time.time >= nextEvpLine && T.Table != null)
            {
                nextEvpLine = Time.time + Random.Range(12f, 26f);
                int n = 0; while (T.Table.Has("evp." + (n + 1))) n++;
                if (n > 0) KhoaHonHUD.Subtitle("<color=#8C9EB0><i>EVP: " + T.Get("evp." + Random.Range(1, n + 1)) + "</i></color>", 3f);
            }
        }

        private void EnsureCompass()
        {
            if (compass != null || KhoaHonHUD.Instance == null) return;
            compass = UIFactory.Rect("KH_EvpCompass", KhoaHonHUD.Instance.Root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-200, 150), new Vector2(-50, 300));
            var ring = compass.gameObject.AddComponent<Image>(); ring.color = new Color(0.05f, 0.06f, 0.07f, 0.55f);
            var lbl = UIKit_Label(compass, "EVP", 16);
            var center = UIFactory.Panel("Center", compass, new Color(0.8f, 0.78f, 0.7f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, -4), new Vector2(4, 4));
            var front = UIFactory.Panel("Front", compass, new Color(0.8f, 0.78f, 0.7f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-1, -14), new Vector2(1, -4));
            markerImg = UIFactory.Panel("Marker", compass, Color.red, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7, -7), new Vector2(7, 7));
            marker = markerImg.rectTransform;
            marker.localRotation = Quaternion.Euler(0, 0, 45);
            compass.gameObject.SetActive(false);
        }

        private static TMPro.TextMeshProUGUI UIKit_Label(Transform parent, string text, float size)
        {
            var style = KhoaHonHUD.Instance != null ? KhoaHonHUD.Instance.Style : null;
            var t = UIFactory.Text("Label", parent, style, false, size, new Color(0.6f, 0.58f, 0.55f), TMPro.TextAlignmentOptions.Bottom);
            t.text = text; t.rectTransform.offsetMin = new Vector2(0, 4);
            return t;
        }

        private void ApplyLed()
        {
            if (ledMat != null) ledMat.SetColor("_EmissionColor", listening ? ledOn : Color.black);
        }

        private void OnDestroy() { if (compass != null) Destroy(compass.gameObject); }
    }
}
