using UnityEngine;

namespace Blackwood.Horror
{
    /// <summary>
    /// Âm thanh bị tường che khuất: bắn tia từ nguồn tới tai người chơi, đếm số vật cản rồi hạ tần số cắt của
    /// AudioLowPassFilter (nghe đục, xa hơn) — phân biệt được "bà ta ở phòng bên" với "bà ta ở ngay hành lang này".
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioOcclusion : MonoBehaviour
    {
        [Tooltip("Tần số cắt khi không bị che (Hz)")] [SerializeField] private float openCutoff = 22000f;
        [Tooltip("Tần số cắt khi bị 1 vật cản che (Hz)")] [SerializeField] private float oneWallCutoff = 2600f;
        [Tooltip("Tần số cắt khi bị từ 2 vật cản trở lên (Hz)")] [SerializeField] private float blockedCutoff = 900f;
        [SerializeField] private float checkInterval = 0.18f;
        [Tooltip("Bỏ qua collider thuộc gốc này (chính nguồn phát)")] [SerializeField] private Transform ignoreRoot;

        private AudioLowPassFilter lpf;
        private Transform listener;
        private float target, next;
        private readonly RaycastHit[] hits = new RaycastHit[12];

        public void SetIgnoreRoot(Transform t) => ignoreRoot = t;

        private void Awake()
        {
            lpf = GetComponent<AudioLowPassFilter>();
            if (lpf == null) lpf = gameObject.AddComponent<AudioLowPassFilter>();
            lpf.cutoffFrequency = openCutoff; lpf.lowpassResonanceQ = 1f;
            target = openCutoff;
            next = Random.value * checkInterval;
        }

        private void Update()
        {
            if (Time.unscaledTime >= next)
            {
                next = Time.unscaledTime + checkInterval;
                if (listener == null || !listener.gameObject.activeInHierarchy)
                {
                    foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                        if (l.isActiveAndEnabled) { listener = l.transform; break; }
                }
                if (listener != null) target = Probe();
            }
            // Mở ra nhanh, đục lại chậm hơn một chút (tránh nháy khi tia lướt mép cửa)
            float k = target > lpf.cutoffFrequency ? 9f : 5f;
            lpf.cutoffFrequency = Mathf.Lerp(lpf.cutoffFrequency, target, 1f - Mathf.Exp(-k * Time.unscaledDeltaTime));
        }

        private float Probe()
        {
            var from = transform.position; var to = listener.position;
            var dir = to - from; float dist = dir.magnitude;
            if (dist < 0.5f) return openCutoff;
            int n = Physics.RaycastNonAlloc(from, dir / dist, hits, dist, ~0, QueryTriggerInteraction.Ignore);
            int walls = 0;
            var playerRoot = listener.root;
            for (int i = 0; i < n; i++)
            {
                var t = hits[i].collider.transform;
                if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue;
                if (t.IsChildOf(playerRoot)) continue;
                if (hits[i].collider.attachedRigidbody != null && !hits[i].collider.attachedRigidbody.isKinematic) continue; // đồ vật nhỏ rơi vãi
                walls++;
            }
            return walls == 0 ? openCutoff : (walls == 1 ? oneWallCutoff : blockedCutoff);
        }
    }
}
