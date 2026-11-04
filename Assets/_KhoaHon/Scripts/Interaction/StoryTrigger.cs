using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Interaction
{
    /// <summary>
    /// Vùng trigger kịch bản: khi Vy đi vào lần đầu → bật cờ, đổi giai đoạn, đóng + khóa cửa, hiện phụ đề.
    /// Ví dụ: cửa chính sập khóa sau lưng khi vào Sảnh.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class StoryTrigger : MonoBehaviour, ISaveParticipant
    {
        [SerializeField] private StoryFlag[] setFlags = new StoryFlag[0];
        [Tooltip("Chỉ kích hoạt khi chưa có cờ này (None = luôn)")]
        [SerializeField] private StoryFlag onlyIfMissing = StoryFlag.None;
        [SerializeField] private bool changeStage = false;
        [SerializeField] private GameStage stage = GameStage.Stage1_GroundFloor;
        [SerializeField] private DoorInteractable closeAndLockDoor;
        [Tooltip("Key TextTable của phụ đề (tùy chọn)")]
        [SerializeField] private string subtitleKey = "";
        [SerializeField] private float subtitleDuration = 4f;

        public void Configure(StoryFlag[] flags, StoryFlag guard, bool setStage, GameStage newStage, DoorInteractable door, string subKey)
        {
            setFlags = flags; onlyIfMissing = guard; changeStage = setStage; stage = newStage;
            closeAndLockDoor = door; subtitleKey = subKey;
        }

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (onlyIfMissing != StoryFlag.None && gm.Has(onlyIfMissing)) return;

            foreach (var f in setFlags) gm.SetFlag(f);
            if (changeStage && gm.Stage < stage) gm.SetStage(stage);
            if (closeAndLockDoor != null) closeAndLockDoor.CloseAndLock();
            if (!string.IsNullOrEmpty(subtitleKey)) KhoaHonHUD.Subtitle(T.Get(subtitleKey), subtitleDuration);
        }

        public void Capture(SaveData data) { }

        /// <summary>Sau khi nạp game: nếu trigger đã từng chạy (đủ cờ) thì khóa lại cửa tương ứng.</summary>
        public void Restore(SaveData data)
        {
            var gm = GameManager.Instance;
            if (gm == null || closeAndLockDoor == null || setFlags.Length == 0) return;
            foreach (var f in setFlags) if (!gm.Has(f)) return;
            closeAndLockDoor.CloseAndLock();
        }
    }
}
