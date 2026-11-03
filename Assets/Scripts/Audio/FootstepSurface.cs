using UnityEngine;

namespace HorrorAudio
{
    public enum SurfaceType
    {
        Dirt = 0,
        Wood = 1
    }

    /// <summary>
    /// Đánh dấu loại bề mặt cho GameObject (Đất hoặc Gỗ) để hệ thống bước chân phát âm thanh tương ứng.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Horror Audio/Footstep Surface")]
    public class FootstepSurface : MonoBehaviour
    {
        [Header("Bề mặt bước chân")]
        [Tooltip("Loại bề mặt tương ứng (Đất hoặc Gỗ)")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Wood;

        public SurfaceType Surface
        {
            get
            {
                // Nếu GameObject là sàn nhà, cầu thang, đường gỗ, bến tàu: luôn luôn là Wood
                string n = gameObject.name;
                if (n.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("plank", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("stair", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("docking", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("villa", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SurfaceType.Wood;
                }
                return surfaceType;
            }
        }

        public void SetSurface(SurfaceType type)
        {
            surfaceType = type;
        }

        void Reset()
        {
            surfaceType = SurfaceType.Wood;
        }
    }
}
