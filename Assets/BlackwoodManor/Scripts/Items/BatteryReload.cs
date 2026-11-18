using System.Collections;
using Blackwood.Core;
using Blackwood.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackwood.Items
{
    /// <summary>
    /// Thay pin cho dụng cụ đang cầm (phím R): tháo viên cũ (dụng cụ tắt ngay), sau <see cref="ToolsConfig.batterySwapTime"/>
    /// giây lắp một viên dự phòng đầy. Hết dự phòng thì báo phải đi tìm. Gắn trên Player.
    /// </summary>
    public class BatteryReload : MonoBehaviour
    {
        [SerializeField] private AudioClip ejectClip;
        [SerializeField] private AudioClip insertClip;

        public static BatteryReload Instance { get; private set; }
        /// <summary>Đang thay pin loại này (dụng cụ tắt, không báo "hết pin").</summary>
        public static bool Swapping { get; private set; }

        private static float nextAnnounce;
        private static string lastHint;
        private Coroutine routine;
        private CellType swappingCell;
        private bool relight; // đèn pin đang sáng lúc bấm R → lắp xong tự bật lại

        public void Configure(AudioClip eject, AudioClip insert) { ejectClip = eject; insertClip = insert; }

        private void Awake() => Instance = this;

        private void OnDisable()
        {
            // Bị ngắt giữa chừng (chết, nạp lại) thì hoàn tất luôn để không mất viên dự phòng đã tháo vỏ
            if (routine != null) { StopCoroutine(routine); routine = null; FinishSwap(); }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame) return;
            if (!GameFlow.IsPlaying || ModalUI.IsAnyOpen || PlayerState.IsHiding) return;
            TrySwapHeld();
        }

        /// <summary>Thay pin cho dụng cụ đang cầm. Trả về true nếu bắt đầu thay.</summary>
        public bool TrySwapHeld()
        {
            var hb = HotbarInventory.Instance;
            var item = hb != null ? hb.CurrentItem : null;
            if (item == null || !BatteryPool.CellFor(item.ItemType, out var cell)) return false;
            return TrySwap(cell);
        }

        public bool TrySwap(CellType cell)
        {
            var pool = BatteryPool.Instance;
            if (pool == null || routine != null) return false;
            string name = CellName(cell);
            if (pool.Spares(cell) <= 0) { BlackwoodHUD.Toast(T.Format("toast.no_spare", name)); return false; }
            if (!pool.CanReplace(cell)) { BlackwoodHUD.Toast(T.Get("toast.battery_full")); return false; }
            routine = StartCoroutine(Swap(cell));
            return true;
        }

        private IEnumerator Swap(CellType cell)
        {
            var pool = BatteryPool.Instance;
            var cfg = GameManager.Instance != null ? GameManager.Instance.Tools : null;
            Swapping = true;
            swappingCell = cell;
            BlackwoodHUD.ClearSubtitleIf(lastHint); // người chơi đã làm theo gợi ý "nhấn [R]"
            // Tháo viên cũ: dụng cụ mất điện ngay (đèn pin tắt, máy ghi âm/kính nhìn đêm tự ngắt)
            relight = false;
            if (cell == CellType.D)
            {
                var fl = GetComponentInChildren<FlashlightItem>(true);
                if (fl != null) { relight = fl.IsOn; fl.SetLightStateImmediate(false); }
            }
            pool.SetCharge(cell, 0f);
            if (ejectClip != null) AmbientPlay(ejectClip, 0.8f);
            yield return new WaitForSeconds(cfg != null ? cfg.batterySwapTime : 1.1f);
            routine = null;
            FinishSwap();
        }

        private void FinishSwap()
        {
            var pool = BatteryPool.Instance;
            if (pool != null && pool.Replace(swappingCell))
            {
                if (insertClip != null) AmbientPlay(insertClip, 0.85f);
                BlackwoodHUD.Toast(T.Format("toast.swapped", CellName(swappingCell), pool.Spares(swappingCell)));
                // Đèn đang sáng lúc thay (pin yếu) → bật lại, chỉ khi vẫn đang cầm đèn pin
                if (relight && swappingCell == CellType.D)
                {
                    var fl = GetComponentInChildren<FlashlightItem>(true);
                    var hb = HotbarInventory.Instance;
                    if (fl != null && !fl.IsOn && hb != null && hb.CurrentItem != null && hb.CurrentItem.ItemType == ItemType.Flashlight) fl.ToggleFlashlight();
                }
            }
            relight = false;
            Swapping = false;
        }

        private void AmbientPlay(AudioClip c, float v)
        {
            var cam = Camera.main;
            AudioSource.PlayClipAtPoint(c, cam != null ? cam.transform.position : transform.position, v * AudioMix.Sfx);
        }

        public static string CellName(CellType t) => T.Get("cell." + (int)t);

        /// <summary>Báo viên pin đã cạn: còn dự phòng thì gợi ý nhấn R, không thì bảo đi tìm (giới hạn tần suất).</summary>
        public static void AnnounceDead(CellType t)
        {
            if (Swapping || Time.unscaledTime < nextAnnounce) return;
            nextAnnounce = Time.unscaledTime + 4f;
            var pool = BatteryPool.Instance;
            bool hasSpare = pool != null && pool.Spares(t) > 0;
            lastHint = T.Format(hasSpare ? "hint.swap" : "hint.find", CellName(t));
            BlackwoodHUD.Subtitle(lastHint, 3.5f);
        }
    }
}
