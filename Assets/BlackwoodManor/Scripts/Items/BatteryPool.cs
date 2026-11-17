using System;
using Blackwood.Core;
using Blackwood.Inventory;
using UnityEngine;

namespace Blackwood.Items
{
    /// <summary>
    /// Kho pin của người chơi: mỗi dụng cụ một loại pin riêng (<see cref="CellType"/>) — viên đang lắp + viên dự phòng.
    /// Đèn pin: pin đại (D) · Máy ghi âm: pin tiểu (AA) · Điện thoại: sạc dự phòng · Kính nhìn đêm: pin máy quay.
    /// API cũ (Current/Drain/Add/SetValue/IsEmpty/IsLow) giữ nguyên nghĩa "pin đèn pin" để code cũ không đổi hành vi.
    /// Logic nằm ở <see cref="PowerCells"/> (thuần, có test).
    /// </summary>
    public class BatteryPool : MonoBehaviour, ISaveParticipant
    {
        public static BatteryPool Instance { get; private set; }

        private PowerCells cells;

        /// <summary>Pin đèn pin đổi (giữ cho code cũ).</summary>
        public event Action<float> Changed;
        /// <summary>Một loại pin đổi (điện hoặc số dự phòng).</summary>
        public event Action<CellType> CellChanged;
        /// <summary>Viên đang lắp của một loại vừa cạn.</summary>
        public event Action<CellType> CellDepleted;

        private ToolsConfig Config => GameManager.Instance != null ? GameManager.Instance.Tools : null;
        private PowerCells Cells => cells ??= new PowerCells(Config != null ? Config.batteryMax : 100f);

        // ------------------------------------------------------------------ API cũ = pin D (đèn pin)
        public float Current => Cells.Charge(CellType.D);
        public float Max => Cells.Max;
        public bool IsEmpty => Cells.IsEmpty(CellType.D);
        public bool IsLow => IsLowCell(CellType.D);
        public void Drain(float amount) => Drain(CellType.D, amount);
        /// <summary>Kiểu cũ "cộng % pin" (vật nhặt cũ) → nay là một viên pin D dự phòng.</summary>
        public void Add(float amount) { if (amount > 0f) AddSpare(CellType.D, 1); }
        public void SetValue(float v) => SetCharge(CellType.D, v);

        // ------------------------------------------------------------------ API theo loại pin
        public float Charge(CellType t) => Cells.Charge(t);
        public float Fraction(CellType t) => Cells.Fraction(t);
        public int Spares(CellType t) => Cells.Spares(t);
        public bool IsEmptyCell(CellType t) => Cells.IsEmpty(t);
        public bool IsLowCell(CellType t) => Config != null && Cells.Charge(t) < Config.lowBatteryThreshold;
        public bool CanReplace(CellType t) => Cells.CanReplace(t);

        public void Drain(CellType t, float amount)
        {
            if (amount <= 0f) return;
            bool depleted = Cells.Drain(t, amount);
            Notify(t);
            if (depleted) CellDepleted?.Invoke(t);
        }

        public void SetCharge(CellType t, float v) { Cells.SetCharge(t, v); Notify(t); }
        public void AddSpare(CellType t, int n = 1) { Cells.AddSpare(t, n); Notify(t); }
        public void SetSpares(CellType t, int n) { Cells.SetSpares(t, n); Notify(t); }

        public bool Replace(CellType t)
        {
            if (!Cells.Replace(t)) return false;
            Notify(t);
            return true;
        }

        private void Notify(CellType t)
        {
            CellChanged?.Invoke(t);
            if (t == CellType.D) Changed?.Invoke(Cells.Charge(CellType.D));
        }

        /// <summary>Loại pin của một dụng cụ; false nếu dụng cụ không dùng pin.</summary>
        public static bool CellFor(ItemType type, out CellType cell)
        {
            switch (type)
            {
                case ItemType.Flashlight: cell = CellType.D; return true;
                case ItemType.Recorder: cell = CellType.AA; return true;
                case ItemType.Phone: cell = CellType.PhonePack; return true;
                case ItemType.NightVision: cell = CellType.CamPack; return true;
                default: cell = CellType.D; return false;
            }
        }

        /// <summary>Dụng cụ dùng loại pin này (để ẩn pin nhặt được của dụng cụ mà nhân vật không mang).</summary>
        public static ItemType ToolFor(CellType t)
        {
            switch (t)
            {
                case CellType.AA: return ItemType.Recorder;
                case CellType.PhonePack: return ItemType.Phone;
                case CellType.CamPack: return ItemType.NightVision;
                default: return ItemType.Flashlight;
            }
        }

        /// <summary>Nhân vật đang chơi có mang dụng cụ này không (đèn pin luôn có).</summary>
        public static bool PlayerHasTool(ItemType type)
        {
            if (type == ItemType.Flashlight) return true;
            var prof = GameManager.Instance != null ? GameManager.Instance.Profile : null;
            if (prof == null || prof.hotbar == null) return true;
            foreach (var it in prof.hotbar) if (it != null && it.ItemType == type) return true;
            return false;
        }

        // ------------------------------------------------------------------ vòng đời + lưu
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            var cfg = Config;
            cells = new PowerCells(cfg != null ? cfg.batteryMax : 100f);
            if (cfg != null)
            {
                for (int i = 0; i < PowerCells.Count; i++) cells.SetCharge((CellType)i, cfg.batteryStart);
                cells.SetSpares(CellType.D, cfg.startSparesD);
                cells.SetSpares(CellType.AA, cfg.startSparesAA);
                cells.SetSpares(CellType.PhonePack, cfg.startSparesPhone);
                cells.SetSpares(CellType.CamPack, cfg.startSparesCam);
            }
            for (int i = 0; i < PowerCells.Count; i++) Notify((CellType)i);
        }

        public void Capture(SaveData data)
        {
            data.battery = Cells.Charge(CellType.D);
            data.cellCharge = Cells.ChargeSnapshot();
            data.cellSpares = Cells.SparesSnapshot();
        }

        public void Restore(SaveData data)
        {
            // Save cũ (trước khi có pin riêng): chỉ có "battery" = pin đèn pin
            if (data.cellCharge == null || data.cellCharge.Length == 0) Cells.SetCharge(CellType.D, data.battery);
            else Cells.Load(data.cellCharge, data.cellSpares);
            for (int i = 0; i < PowerCells.Count; i++) Notify((CellType)i);
        }
    }
}
