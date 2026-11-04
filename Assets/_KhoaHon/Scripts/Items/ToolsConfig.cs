using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>Thông số pin, phim, cooldown — chỉnh ở Inspector, không cần sửa code.</summary>
    [CreateAssetMenu(menuName = "KhoaHon/Config/Tools", fileName = "KH_ToolsConfig")]
    public class ToolsConfig : ScriptableObject
    {
        [Header("Pin (dùng chung đèn pin + máy ghi âm)")]
        public float batteryMax = 100f;
        public float batteryStart = 100f;
        [Tooltip("% pin mất mỗi giây khi đèn pin bật (1% / 3 s)")]
        public float flashlightDrainPerSecond = 1f / 3f;
        [Tooltip("% pin mất mỗi giây khi máy ghi âm đang nghe (1% / 2 s)")]
        public float recorderDrainPerSecond = 0.5f;
        [Tooltip("Dưới mức này đèn pin chập chờn")]
        public float lowBatteryThreshold = 15f;
        [Tooltip("% pin mỗi viên pin tiểu")]
        public float batteryPickupAmount = 50f;

        [Header("Phim")]
        public int filmStart = 8;
        public int filmPerRoll = 6;
        [Tooltip("Cooldown giữa 2 lần chụp (giây)")]
        public float flashCooldown = 1.5f;
    }
}
