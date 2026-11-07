using HorrorGame.Inventory;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Gán bộ công cụ theo nhân vật vào thanh đồ (slot 2–4; slot 1 luôn là đèn pin, không đụng).
    /// Chạy trước HotbarInventory.Awake vì thanh đồ tạo vật phẩm cầm tay ngay trong Awake.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class CharacterLoadout : MonoBehaviour
    {
        private void Awake()
        {
            var gm = GameManager.Instance;
            var prof = gm != null ? gm.Profile : null;
            var hb = GetComponent<HotbarInventory>();
            if (prof == null || hb == null || prof.hotbar == null) return;
            for (int i = 0; i < prof.hotbar.Length && i < 3; i++)
                if (prof.hotbar[i] != null) hb.SetSlotData(i + 1, prof.hotbar[i]);
        }
    }
}
