using System.Collections.Generic;
using System.Reflection;
using HorrorGame.Inventory;
using KhoaHon.Core;
using KhoaHon.Items;
using UnityEngine;

namespace KhoaHon.Characters
{
    /// <summary>
    /// Gán bộ công cụ theo nhân vật vào thanh đồ (slot 2–4; slot 1 luôn là đèn pin, không đụng).
    /// Chạy trước HotbarInventory.Awake vì thanh đồ tạo vật phẩm cầm tay ngay trong Awake.
    /// Dùng bản sao lúc chạy của ItemData để tên hiển thị theo ngôn ngữ (không ghi vào asset).
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class CharacterLoadout : MonoBehaviour
    {
        private static readonly FieldInfo NameField = typeof(ItemData).GetField("displayName", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly List<(int slot, ItemData clone)> clones = new List<(int, ItemData)>();
        private HotbarInventory hb;

        private void Awake()
        {
            var gm = GameManager.Instance;
            var prof = gm != null ? gm.Profile : null;
            hb = GetComponent<HotbarInventory>();
            if (prof == null || hb == null || prof.hotbar == null) return;
            var gate = GetComponent<HotbarGate>();
            for (int i = 0; i < prof.hotbar.Length && i < 3; i++)
            {
                var src = prof.hotbar[i];
                if (src == null) continue;
                var clone = Instantiate(src);
                clone.name = src.name;
                Rename(clone);
                hb.SetSlotData(i + 1, clone);
                if (gate != null) gate.ReplaceItem(src, clone);
                clones.Add((i + 1, clone));
            }
        }

        private void OnEnable() => Loc.Changed += Relabel;
        private void OnDisable() => Loc.Changed -= Relabel;

        private void Relabel()
        {
            var gate = GetComponent<HotbarGate>();
            foreach (var (slot, clone) in clones)
            {
                Rename(clone);
                if (gate == null || !gate.IsSlotLocked(slot)) hb.SetSlotData(slot, clone);
            }
        }

        private static void Rename(ItemData item)
        {
            if (NameField == null || item == null) return;
            string key;
            switch (item.ItemType)
            {
                case ItemType.Recorder: key = "item.recorder"; break;
                case ItemType.FilmCamera: key = "item.film_camera"; break;
                case ItemType.Crowbar: key = "item.crowbar"; break;
                case ItemType.Phone: key = "item.phone"; break;
                case ItemType.NightVision: key = "item.night_vision"; break;
                default: return;
            }
            NameField.SetValue(item, T.Get(key));
        }
    }
}
