using System.Collections.Generic;
using HorrorGame.Inventory;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>Nhân vật người chơi chọn ở đầu game. Chỉ thêm giá trị mới ở cuối (lưu theo tên).</summary>
    public enum PlayerCharacter
    {
        Evelyn = 0,
        Alex = 1,
    }

    /// <summary>
    /// Hồ sơ một nhân vật chơi được: tên, vai trò, động cơ, bộ công cụ trên thanh đồ, chữ mở đầu.
    /// Chữ hiển thị lấy theo key trong TextTable (song ngữ).
    /// </summary>
    [CreateAssetMenu(menuName = "KhoaHon/Character Profile", fileName = "CHAR_")]
    public class CharacterProfile : ScriptableObject
    {
        public PlayerCharacter character;
        [Tooltip("Key tên hiển thị")] public string nameKey;
        [Tooltip("Key vai trò (dòng phụ dưới tên)")] public string roleKey;
        [Tooltip("Key đoạn mô tả động cơ")] public string motiveKey;
        [Tooltip("Key mô tả công cụ riêng")] public string toolsKey;
        [Tooltip("Các key chữ trắng trên nền đen khi bắt đầu")] public List<string> introKeys = new List<string>();
        public Sprite portrait;
        public Color accent = new Color(0.66f, 0.05f, 0.04f);
        [Tooltip("Vật phẩm slot 2–4 (slot 1 luôn là đèn pin)")]
        public ItemData[] hotbar = new ItemData[3];

        public string DisplayName => T.Get(nameKey);
        public string Role => T.Get(roleKey);
        public string Motive => T.Get(motiveKey);
        public string Tools => T.Get(toolsKey);
    }
}
