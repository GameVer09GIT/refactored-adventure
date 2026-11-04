using System;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>Kho pin dùng chung cho đèn pin và máy ghi âm (0–100%).</summary>
    public class BatteryPool : MonoBehaviour, ISaveParticipant
    {
        public static BatteryPool Instance { get; private set; }

        [SerializeField] private float current = 100f;

        public event Action<float> Changed;

        public float Current => current;
        public float Max => Config != null ? Config.batteryMax : 100f;
        public bool IsEmpty => current <= 0f;
        public bool IsLow => Config != null && current < Config.lowBatteryThreshold;
        private ToolsConfig Config => GameManager.Instance != null ? GameManager.Instance.Tools : null;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            if (Config != null) current = Mathf.Min(Config.batteryStart, Max);
        }

        public void Drain(float amount)
        {
            if (amount <= 0f || current <= 0f) return;
            current = Mathf.Max(0f, current - amount);
            Changed?.Invoke(current);
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            current = Mathf.Min(Max, current + amount);
            Changed?.Invoke(current);
        }

        public void SetValue(float v)
        {
            current = Mathf.Clamp(v, 0f, Max);
            Changed?.Invoke(current);
        }

        public void Capture(SaveData data) => data.battery = current;
        public void Restore(SaveData data) => SetValue(data.battery);
    }
}
