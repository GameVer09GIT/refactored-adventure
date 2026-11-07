using System;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>Ngôn ngữ hiển thị. Chỉ thêm giá trị mới ở cuối.</summary>
    public enum Language
    {
        Vi = 0,
        En = 1,
    }

    /// <summary>
    /// Ngôn ngữ hiện tại của game (Việt / Anh). Logic chọn chuỗi là thuần C# để test được;
    /// lưu/đọc PlayerPrefs chỉ khi gọi Load/Save.
    /// </summary>
    public static class Loc
    {
        private const string PrefKey = "kh_lang";

        public static Language Current { get; private set; } = Language.Vi;

        /// <summary>Phát khi đổi ngôn ngữ — UI dựng bằng code nghe sự kiện này để dựng lại.</summary>
        public static event Action Changed;

        public static bool IsEnglish => Current == Language.En;

        /// <summary>Chọn chuỗi theo ngôn ngữ hiện tại; thiếu bản tiếng Anh thì dùng tiếng Việt.</summary>
        public static string Pick(string vi, string en) => Pick(Current, vi, en);

        public static string Pick(Language lang, string vi, string en) =>
            lang == Language.En && !string.IsNullOrEmpty(en) ? en : vi;

        public static void Set(Language lang, bool persist = true)
        {
            bool changed = lang != Current;
            Current = lang;
            if (persist)
            {
                PlayerPrefs.SetInt(PrefKey, (int)lang);
                PlayerPrefs.Save();
            }
            if (changed) Changed?.Invoke();
        }

        public static void Load()
        {
            int v = PlayerPrefs.GetInt(PrefKey, (int)Language.Vi);
            Current = Enum.IsDefined(typeof(Language), v) ? (Language)v : Language.Vi;
        }

        public static readonly string[] DisplayNames = { "Tiếng Việt", "English" };
    }
}
