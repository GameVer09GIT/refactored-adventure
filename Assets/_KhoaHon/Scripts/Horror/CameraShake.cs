using System.Collections;
using UnityEngine;

namespace KhoaHon.Horror
{
    /// <summary>Rung camera người chơi (nhà rung, xích đứt, va chạm). Tự tạo khi cần; dịch localPosition rồi trả về.</summary>
    public class CameraShake : MonoBehaviour
    {
        private static CameraShake instance;
        /// <summary>Hệ số do Cài đặt (0 = tắt rung).</summary>
        public static float Strength = 1f;

        private Coroutine running;
        private Transform cam;
        private Vector3 basePos;

        public static void Shake(float duration, float amplitude)
        {
            if (Strength <= 0f) return;
            if (instance == null) instance = new GameObject("KH_CameraShake").AddComponent<CameraShake>();
            instance.Begin(duration, amplitude * Strength);
        }

        private void Begin(float duration, float amplitude)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null || pc.playerCamera == null) return;
            if (running != null) { StopCoroutine(running); if (cam != null) cam.localPosition = basePos; }
            cam = pc.playerCamera;
            basePos = cam.localPosition;
            running = StartCoroutine(Run(duration, amplitude));
        }

        private IEnumerator Run(float duration, float amplitude)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - t / duration;
                float n1 = Mathf.PerlinNoise(Time.time * 22f, 0.1f) - 0.5f, n2 = Mathf.PerlinNoise(0.7f, Time.time * 22f) - 0.5f;
                if (cam != null) cam.localPosition = basePos + new Vector3(n1, n2, 0f) * (2f * amplitude * k);
                yield return null;
            }
            if (cam != null) cam.localPosition = basePos;
            running = null;
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
