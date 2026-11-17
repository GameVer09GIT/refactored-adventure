using UnityEngine;

namespace Blackwood.Items
{
    /// <summary>Thông số pin, phim, cooldown — chỉnh ở Inspector, không cần sửa code.</summary>
    [CreateAssetMenu(menuName = "Blackwood/Config/Tools", fileName = "BW_ToolsConfig")]
    public class ToolsConfig : ScriptableObject
    {
        [Header("Pin — mỗi dụng cụ một loại (đèn pin: D · máy ghi âm: AA · điện thoại: sạc dự phòng · kính nhìn đêm: pin máy quay)")]
        public float batteryMax = 100f;
        [Tooltip("% điện của viên đang lắp khi bắt đầu")]
        public float batteryStart = 100f;
        [Tooltip("% pin D mất mỗi giây khi đèn pin bật (0,167 ≈ 10 phút một viên)")]
        public float flashlightDrainPerSecond = 1f / 6f;
        [Tooltip("% pin AA mất mỗi giây khi máy ghi âm đang nghe (0,2 ≈ 8 phút)")]
        public float recorderDrainPerSecond = 0.2f;
        [Tooltip("Dưới mức này đèn pin chập chờn / HUD báo đỏ")]
        public float lowBatteryThreshold = 15f;
        [Tooltip("(cũ) % pin mỗi viên — nay mỗi vật nhặt là một viên dự phòng đầy")]
        public float batteryPickupAmount = 50f;
        [Tooltip("Số viên dự phòng mang theo lúc đầu")]
        public int startSparesD = 1, startSparesAA = 1, startSparesPhone = 1, startSparesCam = 1;
        [Tooltip("Thời gian thay pin (giây) — trong lúc thay dụng cụ tắt")]
        public float batterySwapTime = 1.1f;
        [Tooltip("% sạc dự phòng mất mỗi giây khi đang cầm điện thoại (màn hình livestream)")]
        public float phoneIdleDrainPerSecond = 0.06f;

        [Header("Phim")]
        public int filmStart = 8;
        public int filmPerRoll = 6;
        [Tooltip("Cooldown giữa 2 lần chụp (giây)")]
        public float flashCooldown = 1.5f;

        [Header("Flash làm choáng (máy ảnh Evelyn / điện thoại Alex)")]
        [Tooltip("Tầm flash làm choáng Mrs. Peake (m)")]
        public float flashStunRange = 9f;
        [Tooltip("Nửa góc nón flash (độ)")]
        public float flashStunHalfAngle = 30f;
        [Tooltip("% pin mỗi lần flash điện thoại")]
        public float phoneFlashBatteryCost = 4f;

        [Header("Kính nhìn đêm (Alex)")]
        [Tooltip("% pin máy quay tiêu hao mỗi giây khi bật (0,42 ≈ 4 phút)")]
        public float nightVisionDrainPerSecond = 0.42f;

        [Header("EVP định hướng (Evelyn)")]
        [Tooltip("Tầm máy ghi âm bắt được bước chân Mrs. Peake (m)")]
        public float evpRange = 30f;
    }
}
