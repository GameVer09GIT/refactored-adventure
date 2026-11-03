using System.Collections.Generic;
using UnityEngine;

namespace HorrorAudio
{
    /// <summary>
    /// Vùng nhận diện bề mặt bước chân ưu tiên cao nhất (FootstepZone).
    /// Tự đăng ký vào danh sách tĩnh khi kích hoạt, kiểm tra tọa độ chân người chơi bằng toán học Bound/LocalBox,
    /// không phụ thuộc tên collider hay vật liệu, không tốn chi phí raycast hay cấp phát bộ nhớ.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class FootstepZone : MonoBehaviour
    {
        [Tooltip("Loại bề mặt áp dụng cho toàn bộ không gian trong vùng này")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Wood;

        [Tooltip("Cho phép tính toán theo góc xoay thực tế của vùng (Rotated OBB)")]
        [SerializeField] private bool useRotatedBox = true;

        private BoxCollider boxCollider;
        private Vector3 boxCenter;
        private Vector3 boxExtents;
        private Bounds cachedWorldBounds;

        private static readonly List<FootstepZone> activeZones = new List<FootstepZone>();

        public SurfaceType Surface => surfaceType;

        void Awake()
        {
            CacheColliderInfo();
        }

        void OnEnable()
        {
            CacheColliderInfo();
            if (!activeZones.Contains(this))
            {
                activeZones.Add(this);
            }
        }

        void OnDisable()
        {
            activeZones.Remove(this);
        }

        private void CacheColliderInfo()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
            }

            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
                boxCenter = boxCollider.center;
                boxExtents = boxCollider.size * 0.5f;
                cachedWorldBounds = boxCollider.bounds;
            }
        }

        /// <summary>
        /// Kiểm tra xem vị trí chân người chơi có nằm bên trong vùng này hay không
        /// </summary>
        public bool Contains(Vector3 worldPoint)
        {
            if (boxCollider == null) return false;

            // 1. Kiểm tra nhanh bằng AABB Bounds thế giới
            if (!cachedWorldBounds.Contains(worldPoint))
            {
                // Nếu transform di chuyển trong runtime, cập nhật lại bounds
                if (transform.hasChanged)
                {
                    cachedWorldBounds = boxCollider.bounds;
                    transform.hasChanged = false;
                    if (!cachedWorldBounds.Contains(worldPoint)) return false;
                }
                else
                {
                    return false;
                }
            }

            // 2. Nếu vùng có góc xoay, chuyển điểm về hệ tọa độ cục bộ (OBB) để độ chính xác tuyệt đối
            if (useRotatedBox)
            {
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint) - boxCenter;
                return Mathf.Abs(localPoint.x) <= boxExtents.x &&
                       Mathf.Abs(localPoint.y) <= boxExtents.y &&
                       Mathf.Abs(localPoint.z) <= boxExtents.z;
            }

            return true;
        }

        /// <summary>
        /// Tìm vùng phủ tại vị trí chỉ định. Trả về true nếu vị trí nằm trong bất kỳ FootstepZone nào.
        /// </summary>
        public static bool TryGetZoneSurface(Vector3 worldPosition, out SurfaceType detectedSurface, out FootstepZone matchedZone)
        {
#if UNITY_EDITOR
            if (activeZones.Count == 0)
            {
                FootstepZone[] zones = Object.FindObjectsByType<FootstepZone>(FindObjectsSortMode.None);
                foreach (var z in zones)
                {
                    if (z != null && z.enabled && z.gameObject.activeInHierarchy && !activeZones.Contains(z))
                    {
                        z.CacheColliderInfo();
                        activeZones.Add(z);
                    }
                }
            }
#endif
            for (int i = 0; i < activeZones.Count; i++)
            {
                FootstepZone zone = activeZones[i];
                if (zone != null && zone.Contains(worldPosition))
                {
                    detectedSurface = zone.surfaceType;
                    matchedZone = zone;
                    return true;
                }
            }

            detectedSurface = SurfaceType.Dirt;
            matchedZone = null;
            return false;
        }

        public static void ClearRegistry()
        {
            activeZones.Clear();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;

            Color fillColor = (surfaceType == SurfaceType.Wood)
                ? new Color(0.2f, 0.85f, 0.3f, 0.25f)
                : new Color(0.85f, 0.55f, 0.2f, 0.25f);

            Color wireColor = (surfaceType == SurfaceType.Wood)
                ? new Color(0.2f, 1f, 0.35f, 0.9f)
                : new Color(1f, 0.65f, 0.2f, 0.9f);

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = fillColor;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);

            Gizmos.matrix = oldMatrix;
        }
#endif
    }
}
