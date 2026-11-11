using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.AI
{
    /// <summary>
    /// Gắn lên Player: chạy (Shift) phát tiếng động bán kính 8 m mỗi 0.5 s, đi bộ 2 m (PROMPT 5.1).
    /// </summary>
    public class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField] private float runRadius = 8f;
        [SerializeField] private float walkRadius = 2f;
        [SerializeField] private float interval = 0.5f;
        [SerializeField] private float minMoveSpeed = 0.5f;

        private PlayerController pc;
        private float next;

        private void Awake() => pc = GetComponent<PlayerController>();

        private void Update()
        {
            if (pc == null || !pc.enabled || PlayerState.IsHiding || Time.time < next) return;
            if (pc.HorizontalSpeed < minMoveSpeed) return;
            next = Time.time + interval;
            bool run = pc.IsRunning;
            GameEvents.RaiseNoise(transform.position, run ? runRadius : walkRadius, run ? NoiseType.Run : NoiseType.Footstep);
        }
    }
}
