using HorrorGame.Inventory;

namespace KhoaHon.Items
{
    /// <summary>
    /// Máy ảnh film có flash (Slot 3). Phase 1: chỉ cầm được.
    /// Phase 3 bổ sung chụp flash, layer HiddenInk, lưu ảnh vào Sổ tay, làm choáng bà Quản Gia.
    /// </summary>
    public class FilmCameraItem : HeldItem
    {
        public override void OnPrimaryUse()
        {
            // Chức năng chụp làm ở Phase 3
        }
    }
}
