using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Điểm lưu. Vy đi vào vùng trigger → CheckpointManager ghi nhận và tự lưu.
    /// Hướng forward của Transform là hướng Vy nhìn khi hồi sinh.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Id: CP_Gate, CP_Hall, CP_Floor2Landing, CP_Shrine, CP_CellarStairs, CP_RitualHall")]
        [SerializeField] private string id = "CP_";
        [Tooltip("Có kích hoạt bằng cách đi vào trigger không (CP_Shrine kích hoạt bằng code)")]
        [SerializeField] private bool activateOnEnter = true;
        [Tooltip("Đổi giai đoạn khi đến checkpoint (tùy chọn)")]
        [SerializeField] private bool setStage = false;
        [SerializeField] private GameStage stage = GameStage.Stage1_GroundFloor;

        public string Id => id;

        public void Configure(string newId, bool onEnter, bool changeStage, GameStage newStage)
        {
            id = newId; activateOnEnter = onEnter; setStage = changeStage; stage = newStage;
        }

        private void Reset()
        {
            var bc = GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(3f, 2.5f, 3f);
            bc.center = new Vector3(0f, 1.25f, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!activateOnEnter || !other.CompareTag("Player")) return;
            Activate();
        }

        public void Activate()
        {
            if (setStage && GameManager.Instance != null && GameManager.Instance.Stage < stage)
                GameManager.Instance.SetStage(stage);
            CheckpointManager.Instance?.Reach(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, 1.25f, 0), new Vector3(3f, 2.5f, 3f));
            Gizmos.DrawLine(new Vector3(0, 1f, 0), new Vector3(0, 1f, 1.5f));
        }
    }
}
