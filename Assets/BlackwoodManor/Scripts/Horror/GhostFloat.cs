using UnityEngine;

namespace Blackwood.Horror
{
    /// <summary>
    /// Bóng ma lơ lửng: nhấp nhô chậm, nghiêng nhẹ như bị gió lạnh lay, độ sáng phập phồng (hệ số nhân của đèn hào quang).
    /// Chỉ đổi transform của chính nó quanh vị trí ban đầu — không đụng logic cảnh cắt.
    /// </summary>
    public class GhostFloat : MonoBehaviour
    {
        [Tooltip("Biên độ nhấp nhô (m)")]
        [SerializeField] private float bob = 0.035f;
        [Tooltip("Chu kỳ nhấp nhô (giây)")]
        [SerializeField] private float period = 4.2f;
        [Tooltip("Góc nghiêng tối đa (độ)")]
        [SerializeField] private float sway = 1.6f;
        [SerializeField] private Light aura;

        private Vector3 basePos;
        private Quaternion baseRot;
        private float auraBase, seed;

        public void Configure(Light auraLight) { aura = auraLight; }

        private void Awake()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            if (aura != null) auraBase = aura.intensity;
            seed = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time + seed;
            float w = 2f * Mathf.PI / Mathf.Max(0.5f, period);
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * w) * bob + bob);
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * w * 0.63f) * sway, 0f, Mathf.Sin(t * w * 0.41f + 1.3f) * sway);
            if (aura != null) aura.intensity = auraBase * (0.85f + 0.15f * Mathf.PerlinNoise(t * 0.7f, 0.3f));
        }
    }
}
