using System.Collections;
using KhoaHon.AI;
using KhoaHon.Core;
using KhoaHon.Horror;
using UnityEngine;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Đèn flash dùng chung (máy ảnh film của Evelyn, điện thoại của Alex): chớp sáng ngắn, làm choáng Mrs. Peake
    /// nếu bà ở trong nón flash và không bị che khuất.
    /// </summary>
    public static class FlashUtil
    {
        public static bool Fire(Transform cam, MonoBehaviour host)
        {
            if (cam == null) return false;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            float range = cfg != null ? cfg.flashStunRange : 9f;
            float half = cfg != null ? cfg.flashStunHalfAngle : 30f;
            if (host != null) host.StartCoroutine(Burst(cam));
            HorrorPostFX.Instance?.Flash(0.15f);
            GameEvents.RaiseNoise(cam.position, 5f, NoiseType.Other);

            var hk = HousekeeperAI.Instance;
            if (hk == null) return false;
            var target = hk.transform.position + Vector3.up * 1.5f;
            var to = target - cam.position;
            if (to.magnitude > range || Vector3.Angle(cam.forward, to) > half) return false;
            if (Physics.Raycast(cam.position, to.normalized, out var hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && hit.transform.root != hk.transform.root) return false;
            hk.OnFlashed();
            AchievementService.Unlock("first_flash");
            return true;
        }

        private static IEnumerator Burst(Transform cam)
        {
            var go = new GameObject("KH_FlashBurst");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.spotAngle = 80f; l.range = 14f; l.intensity = 60f; l.color = new Color(0.95f, 0.97f, 1f);
            l.shadows = LightShadows.Soft;
            float t = 0f;
            while (t < 0.14f) { t += Time.deltaTime; l.intensity = Mathf.Lerp(60f, 0f, t / 0.14f); yield return null; }
            Object.Destroy(go);
        }
    }
}
