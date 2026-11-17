using Blackwood.Core;
using Blackwood.Inventory;
using UnityEngine;

namespace Blackwood.Items
{
    /// <summary>
    /// Pin của đèn pin (Slot 1, pin đại D): trừ điện khi đèn sáng, cạn thì TẮT hẳn (bấm lại chỉ nghe tiếng công tắc),
    /// pin yếu thì chập chờn. Chỉ dùng API bật/tắt của FlashlightItem — không đụng IK, tư thế, vị trí hay thông số đèn.
    /// </summary>
    public class FlashlightBatteryDrain : MonoBehaviour
    {
        [Tooltip("Khoảng thời gian giữa các lần chập chờn khi pin yếu (giây)")]
        [SerializeField] private Vector2 flickerInterval = new Vector2(0.6f, 2.2f);
        [SerializeField] private Vector2 flickerOffDuration = new Vector2(0.04f, 0.18f);

        private FlashlightItem flashlight;
        private float nextFlicker;
        private float flickerRestoreAt = -1f;

        private void OnEnable()
        {
            FlashlightItem.HasPower = () => BatteryPool.Instance == null || !BatteryPool.Instance.IsEmptyCell(CellType.D);
            FlashlightItem.PowerDenied = () => BatteryReload.AnnounceDead(CellType.D);
        }

        private void OnDisable()
        {
            FlashlightItem.HasPower = null;
            FlashlightItem.PowerDenied = null;
        }

        private void Update()
        {
            if (flashlight == null)
            {
                flashlight = GetComponentInChildren<FlashlightItem>(true);
                if (flashlight == null) return;
            }

            var pool = BatteryPool.Instance;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            if (pool == null || cfg == null) return;

            // Hết nhịp chập chờn → sáng lại (chỉ khi người chơi vẫn đang bật)
            if (flickerRestoreAt > 0f && Time.time >= flickerRestoreAt)
            {
                flickerRestoreAt = -1f;
                flashlight.SetFlicker(false);
            }

            if (!flashlight.IsOn) return;

            if (pool.IsEmptyCell(CellType.D))
            {
                flickerRestoreAt = -1f;
                flashlight.SetLightStateImmediate(false);
                BatteryReload.AnnounceDead(CellType.D);
                return;
            }

            if (GameFlow.IsPlaying) pool.Drain(CellType.D, cfg.flashlightDrainPerSecond * Time.deltaTime);

            if (pool.IsLowCell(CellType.D) && flickerRestoreAt < 0f && Time.time >= nextFlicker)
            {
                nextFlicker = Time.time + Random.Range(flickerInterval.x, flickerInterval.y);
                flashlight.SetFlicker(true);
                flickerRestoreAt = Time.time + Random.Range(flickerOffDuration.x, flickerOffDuration.y);
            }
        }
    }
}
