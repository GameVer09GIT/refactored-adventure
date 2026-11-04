using System;
using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Bảng chuỗi hiển thị (key → text tiếng Việt). Mọi chuỗi UI chung đặt ở đây, không hardcode trong script.
    /// </summary>
    [CreateAssetMenu(menuName = "KhoaHon/Text Table", fileName = "KH_TextTable")]
    public class TextTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            [TextArea(1, 4)] public string text;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, string> cache;

        public List<Entry> Entries => entries;

        public string Get(string key)
        {
            if (cache == null || cache.Count != entries.Count) Rebuild();
            return cache.TryGetValue(key, out var v) ? v : key;
        }

        public string Format(string key, params object[] args)
        {
            var fmt = Get(key);
            try { return string.Format(fmt, args); }
            catch (FormatException) { return fmt; }
        }

        public void Set(string key, string text)
        {
            var e = entries.Find(x => x.key == key);
            if (e == null) entries.Add(new Entry { key = key, text = text });
            else e.text = text;
            cache = null;
        }

        private void Rebuild()
        {
            cache = new Dictionary<string, string>();
            foreach (var e in entries) if (!string.IsNullOrEmpty(e.key)) cache[e.key] = e.text;
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
