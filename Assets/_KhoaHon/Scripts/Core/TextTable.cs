using System;
using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Bảng chuỗi hiển thị (key → tiếng Việt / tiếng Anh). Mọi chuỗi UI chung đặt ở đây, không hardcode trong script.
    /// Trả về theo <see cref="Loc.Current"/>; thiếu bản tiếng Anh thì dùng tiếng Việt.
    /// </summary>
    [CreateAssetMenu(menuName = "KhoaHon/Text Table", fileName = "KH_TextTable")]
    public class TextTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            [Tooltip("Tiếng Việt")]
            [TextArea(1, 4)] public string text;
            [Tooltip("English")]
            [TextArea(1, 4)] public string en;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> cache;

        public List<Entry> Entries => entries;

        public string Get(string key) => Get(key, Loc.Current);

        public string Get(string key, Language lang)
        {
            if (cache == null || cache.Count != entries.Count) Rebuild();
            return cache.TryGetValue(key, out var e) ? Loc.Pick(lang, e.text, e.en) : key;
        }

        public bool Has(string key)
        {
            if (cache == null || cache.Count != entries.Count) Rebuild();
            return cache.ContainsKey(key);
        }

        public string Format(string key, params object[] args)
        {
            var fmt = Get(key);
            try { return string.Format(fmt, args); }
            catch (FormatException) { return fmt; }
        }

        public void Set(string key, string vi, string en = null)
        {
            var e = entries.Find(x => x.key == key);
            if (e == null) entries.Add(new Entry { key = key, text = vi, en = en });
            else { e.text = vi; e.en = en; }
            cache = null;
        }

        /// <summary>Xóa các key không còn trong danh sách (dùng khi DataBuilder dựng lại toàn bộ).</summary>
        public void RemoveAllExcept(HashSet<string> keep)
        {
            entries.RemoveAll(e => !keep.Contains(e.key));
            cache = null;
        }

        private void Rebuild()
        {
            cache = new Dictionary<string, Entry>();
            foreach (var e in entries) if (!string.IsNullOrEmpty(e.key)) cache[e.key] = e;
        }

        private void OnValidate() => cache = null;
    }

    /// <summary>Truy cập nhanh bảng chuỗi đang dùng (do GameManager gán).</summary>
    public static class T
    {
        public static TextTable Table;
        public static string Get(string key) => Table != null ? Table.Get(key) : key;
        public static string Format(string key, params object[] args) => Table != null ? Table.Format(key, args) : key;
    }
}
