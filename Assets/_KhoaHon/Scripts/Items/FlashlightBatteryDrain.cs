using HorrorGame.Inventory;
using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>
    /// Hook tiêu hao pin cho đèn pin có sẵn (Slot 1) mà KHÔNG sửa FlashlightItem:
    /// chỉ đọc IsOn và gọi API public SetLightStateImmediate khi hết pin / chập chờn.
    /// Không đụng IK, pose, offset hay thông số đèn.
    /// </summary>
    public class FlashlightBatteryDrain : MonoBehaviour
    {
        [Tooltip("Khoảng thời gian giữa các lần chập chờn khi pin yếu (giây)")]
        [SerializeField] private Vector2 flickerInterval = new Vector2(0.6f, 2.2f);
        [SerializeField] private Vector2 flickerOffDuration = new Vector2(0.04f, 0.18f);

        private FlashlightItem flashlight;
        private float nextFlicker;
        private float flickerRestoreAt = -1f;

        private void Update()
        {
            if (flashlight == null)
            {
                flashlight = GetComponentInChildren<FlashlightItem>(true);
                if (flashlight == null) return;
            }

            var pool = BatteryPool.Instance;
            var cfg = KhoaHon.Core.GameManager.Instance != null ? KhoaHon.Core.GameManager.Instance.Tools : null;
            if (pool == null || cfg == null) return;

            // Đang tắt tạm vì chập chờn → bật lại đúng lúc
            if (flickerRestoreAt > 0f)
            {
                if (Time.time >= flickerRestoreAt)
                {
                    flickerRestoreAt = -1f;
                    if (!pool.IsEmpty && flashlight.IsEquipped) flashlight.SetLightStateImmediate(true);
                }
                return;
            }

            if (!flashlight.IsOn) return;

            if (pool.IsEmpty)
            {
                flashlight.SetLightStateImmediate(false);
                return;
            }

            pool.Drain(cfg.flashlightDrainPerSecond * Time.deltaTime);

            if (pool.IsLow && Time.time >= nextFlicker)
            {
                nextFlicker = Time.time + Random.Range(flickerInterval.x, flickerInterval.y);
                flashlight.SetLightStateImmediate(false);
                flickerRestoreAt = Time.time + Random.Range(flickerOffDuration.x, flickerOffDuration.y);
            }
        }
    }
}
