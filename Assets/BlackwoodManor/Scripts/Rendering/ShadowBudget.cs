using System.Collections.Generic;
using UnityEngine;

namespace Blackwood.Rendering
{
    /// <summary>
    /// Giới hạn số đèn điểm/đèn chiếu cùng đổ bóng: mỗi đèn điểm tốn 6 shadow map, 17 đèn bật bóng cùng lúc làm URP phải
    /// bóp độ phân giải bóng mỗi khung (bóng vỡ hạt, tụt FPS, Console báo liên tục). Chỉ vài đèn gần camera nhất được giữ bóng,
    /// đèn xa tắt bóng (ánh sáng vẫn còn). Không đụng đèn thuộc người chơi (đèn pin, đèn tay) và đèn Directional.
    /// </summary>
    public class ShadowBudget : MonoBehaviour
    {
        // Atlas bóng 4096 với bóng mỗi đèn 1024 px = 16 ô. Đèn điểm 6 ô/đèn: 2 đèn điểm (12) + 2 đèn chiếu (2)
        // + đèn pin + chớp máy ảnh = 16 ô, vừa khít, URP không phải bóp độ phân giải.
        [Tooltip("Số đèn điểm được đổ bóng cùng lúc (mỗi đèn = 6 shadow map)")]
        [SerializeField] private int maxPointShadows = 2;
        [Tooltip("Số đèn chiếu (spot) được đổ bóng cùng lúc")]
        [SerializeField] private int maxSpotShadows = 2;
        [Tooltip("Chu kỳ chọn lại (giây)")]
        [SerializeField] private float interval = 0.3f;

        private class Entry { public Light light; public LightShadows original; public float dist; }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly HashSet<Light> known = new HashSet<Light>();
        private float nextCollect;
        private readonly List<Entry> points = new List<Entry>(), spots = new List<Entry>();
        private float next;

        private void Start() => Collect();

        /// <summary>Gom các đèn có bóng trong scene (gọi lại được nếu dựng thêm đèn).</summary>
        public void Collect()
        {
            entries.RemoveAll(e => e.light == null);
            var pc = FindFirstObjectByType<PlayerController>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                if (known.Contains(l) || l.shadows == LightShadows.None) continue; // đèn đã quản lý giữ nguyên bóng gốc đã lưu
                known.Add(l);
                if (pc != null && l.transform.IsChildOf(pc.transform)) continue;
                entries.Add(new Entry { light = l, original = l.shadows });
            }
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + interval;
            if (Time.unscaledTime > nextCollect) { nextCollect = Time.unscaledTime + 4f; Collect(); } // đèn sinh ra lúc chạy (chớp, cảnh cắt)
            var cam = ActiveCamera();
            if (cam == null) return;
            var eye = cam.transform.position;
            points.Clear(); spots.Clear();
            foreach (var e in entries)
            {
                if (e.light == null) continue;
                if (!e.light.isActiveAndEnabled) { e.light.shadows = LightShadows.None; continue; }
                e.dist = Vector3.Distance(eye, e.light.transform.position);
                // Đèn ở quá xa so với tầm chiếu thì bóng của nó không tới được chỗ người chơi nhìn
                if (e.dist > e.light.range + 25f) { e.light.shadows = LightShadows.None; continue; }
                (e.light.type == LightType.Point ? points : spots).Add(e);
            }
            Assign(points, maxPointShadows);
            Assign(spots, maxSpotShadows);
        }

        /// <summary>Camera đang hiển thị: Camera.main nếu có, nếu không (menu, cảnh cắt) lấy camera bật có depth cao nhất.</summary>
        private static Camera ActiveCamera()
        {
            var main = Camera.main;
            if (main != null && main.isActiveAndEnabled) return main;
            Camera best = null;
            foreach (var c in Camera.allCameras)
                if (c.targetTexture == null && (best == null || c.depth > best.depth)) best = c;
            return best;
        }

        private static void Assign(List<Entry> list, int budget)
        {
            list.Sort((a, b) => a.dist.CompareTo(b.dist));
            for (int i = 0; i < list.Count; i++) list[i].light.shadows = i < budget ? list[i].original : LightShadows.None;
        }

        private void OnDestroy()
        {
            foreach (var e in entries) if (e.light != null) e.light.shadows = e.original;
        }
    }
}
