using HorrorGame.Inventory;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Items
{
    /// <summary>
    /// Khóa ô hotbar cho tới khi có đồ (ô 4 xà beng mở khi có cờ HasCrowbar), không sửa HotbarInventory:
    /// ẩn icon bằng SetSlotData(null) và chọn lại ô trước nếu người chơi chuyển vào ô bị khóa.
    /// </summary>
    public class HotbarGate : MonoBehaviour
    {
        [System.Serializable]
        public class GatedSlot
        {
            [Tooltip("Chỉ số ô (0–3)")] public int index = 3;
            public ItemData item;
            public StoryFlag unlockFlag = StoryFlag.HasCrowbar;
        }

        [SerializeField] private GatedSlot[] gated = { new GatedSlot() };

        private HotbarInventory hotbar;
        private int lastAllowed;

        private void Awake() => hotbar = GetComponent<HotbarInventory>();

        private void OnEnable()
        {
            if (hotbar != null) hotbar.OnSelectionChanged += OnSelectionChanged;
            GameEvents.FlagSet += OnFlagSet;
        }

        private void OnDisable()
        {
            if (hotbar != null) hotbar.OnSelectionChanged -= OnSelectionChanged;
            GameEvents.FlagSet -= OnFlagSet;
        }

        private void Start()
        {
            lastAllowed = hotbar != null ? hotbar.SelectedIndex : 0;
            Refresh();
        }

        /// <summary>Cập nhật icon các ô theo cờ hiện tại (gọi sau khi nạp game).</summary>
        public void Refresh(bool force = true)
        {
            if (hotbar == null) return;
            if (cached == null || cached.Length != gated.Length) { cached = new int[gated.Length]; force = true; }
            for (int i = 0; i < gated.Length; i++)
            {
                int state = IsUnlocked(gated[i]) ? 1 : 2;
                if (!force && cached[i] == state) continue;
                cached[i] = state;
                hotbar.SetSlotData(gated[i].index, state == 1 ? gated[i].item : null);
            }
        }

        private int[] cached;

        /// <summary>Thay ItemData của ô bị khóa (CharacterLoadout dùng bản sao có tên bản địa hóa).</summary>
        public void ReplaceItem(ItemData from, ItemData to)
        {
            foreach (var g in gated) if (g.item == from) g.item = to;
        }

        public bool IsSlotLocked(int index) => IsLocked(index);

        private bool IsUnlocked(GatedSlot g) =>
            g.unlockFlag == StoryFlag.None || (GameManager.Instance != null && GameManager.Instance.Has(g.unlockFlag));

        private bool IsLocked(int index)
        {
            foreach (var g in gated) if (g.index == index && !IsUnlocked(g)) return true;
            return false;
        }

        private void OnSelectionChanged(int index, ItemData data)
        {
            if (IsLocked(index))
            {
                hotbar.SelectSlot(lastAllowed);
                return;
            }
            lastAllowed = index;
        }

        private void OnFlagSet(StoryFlag f)
        {
            foreach (var g in gated) if (g.unlockFlag == f) Refresh();
        }

        private void LateUpdate()
        {
            // Sau khi nạp game cờ đổi mà không phát sự kiện → đồng bộ lại 1 lần mỗi giây
            if (Time.frameCount % 30 == 0) Refresh(false);
        }
    }
}
