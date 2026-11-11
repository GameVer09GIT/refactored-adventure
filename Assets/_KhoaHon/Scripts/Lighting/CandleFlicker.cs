using UnityEngine;

namespace KhoaHon.Lighting
{
    /// <summary>Ánh nến leo lét: dao động cường độ + dịch nhẹ vị trí (Perlin noise), rẻ, không cần animation.</summary>
    [RequireComponent(typeof(Light))]
    public class CandleFlicker : MonoBehaviour
    {
        [SerializeField] private float intensityJitter = 0.35f;
        [SerializeField] private float speed = 7f;
        [SerializeField] private float positionJitter = 0.015f;

        private Light lt;
        private float baseIntensity;
        private Vector3 basePos;
        private float seed;

        private void Awake()
        {
            lt = GetComponent<Light>();
            baseIntensity = lt.intensity;
            basePos = transform.localPosition;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            float n = Mathf.PerlinNoise(t, seed) * 2f - 1f;
            lt.intensity = Mathf.Max(0f, baseIntensity * (1f + n * intensityJitter));
            transform.localPosition = basePos + new Vector3(
                (Mathf.PerlinNoise(t * 0.7f, 1.3f) - 0.5f) * positionJitter,
                (Mathf.PerlinNoise(2.1f, t * 0.9f) - 0.5f) * positionJitter,
                0f);
        }
    }
}
