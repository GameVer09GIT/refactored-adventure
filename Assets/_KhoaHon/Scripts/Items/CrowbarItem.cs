using HorrorGame.Inventory;

namespace KhoaHon.Items
{
    /// <summary>
    /// Xà beng (Slot 4). Không dùng để đánh. Việc nạy (cửa sổ sảnh, thùng gỗ) do Interactable
    /// kiểm tra cờ HasCrowbar, nên chuột trái không làm gì.
    /// </summary>
    public class CrowbarItem : HeldItem
    {
        public override void OnPrimaryUse() { }
    }
}
