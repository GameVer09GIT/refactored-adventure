using System;
using System.Collections.Generic;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Notebook
{
    /// <summary>
    /// Nội dung Sổ tay điều tra: danh sách mục đã thu thập + trạng thái "đã xem".
    /// </summary>
    public class NotebookManager : MonoBehaviour, ISaveParticipant
    {
        public static NotebookManager Instance { get; private set; }

        [Tooltip("Mục có sẵn từ đầu game (ví dụ: ảnh Quân gửi)")]
        [SerializeField] private List<NotebookSource> startingEntries = new List<NotebookSource>();

        private readonly List<NotebookSource> entries = new List<NotebookSource>();
        private readonly HashSet<string> seen = new HashSet<string>();

        public event Action Changed;

        public IReadOnlyList<NotebookSource> Entries => entries;

        private void Awake()
        {
            Instance = this;
            foreach (var s in startingEntries) AddInternal(s);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool Contains(string id) => entries.Exists(e => e.Id == id);

        /// <summary>Thêm mục. Trả về true nếu là mục mới.</summary>
        public bool Add(NotebookSource s)
        {
            if (s != null && entries.Count >= 22) AchievementService.Unlock("archivist");
            if (!AddInternal(s)) return false;
            Changed?.Invoke();
            return true;
        }

        private bool AddInternal(NotebookSource s)
        {
            if (s == null || string.IsNullOrEmpty(s.Id) || Contains(s.Id)) return false;
            entries.Add(s);
            return true;
        }

        public bool IsSeen(NotebookSource s) => s != null && seen.Contains(s.Id);

        public void MarkSeen(NotebookSource s)
        {
            if (s != null && seen.Add(s.Id)) Changed?.Invoke();
        }

        /// <summary>
        /// Tab hiển thị thực tế: trang bùa đã đặt lên đài sen rời tab Bùa chú
        /// nhưng bài chú đã chép vẫn còn ở tab Tài liệu.
        /// </summary>
        public NotebookTab EffectiveTab(NotebookSource s)
        {
            if (s is TalismanData t && GameManager.Instance != null && GameManager.Instance.Has(HanhUtil.TalismanPlacedFlag(t.hanh)))
                return NotebookTab.Documents;
            return s.Tab;
        }

        /// <summary>Các mục của 1 tab, sắp theo mốc Biên niên sử rồi theo thứ tự thu thập.</summary>
        public List<NotebookSource> EntriesFor(NotebookTab tab)
        {
            var list = new List<NotebookSource>();
            foreach (var e in entries) if (EffectiveTab(e) == tab) list.Add(e);
            // Sắp xếp ổn định theo năm
            var indexed = new List<KeyValuePair<int, NotebookSource>>();
            for (int i = 0; i < list.Count; i++) indexed.Add(new KeyValuePair<int, NotebookSource>(i, list[i]));
            indexed.Sort((a, b) =>
            {
                int c = a.Value.Year.CompareTo(b.Value.Year);
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            list.Clear();
            foreach (var kv in indexed) list.Add(kv.Value);
            return list;
        }

        public int UnseenCount(NotebookTab tab)
        {
            int n = 0;
            foreach (var e in entries) if (EffectiveTab(e) == tab && !seen.Contains(e.Id)) n++;
            return n;
        }

        public int TotalUnseen
        {
            get { int n = 0; foreach (var e in entries) if (!seen.Contains(e.Id)) n++; return n; }
        }

        public void Capture(SaveData data)
        {
            data.notebookEntries = new List<string>();
            foreach (var e in entries) data.notebookEntries.Add(e.Id);
            data.notebookSeen = new List<string>(seen);
        }

        public void Restore(SaveData data)
        {
            entries.Clear();
            seen.Clear();
            var db = GameManager.Instance != null ? GameManager.Instance.Database : null;
            foreach (var id in data.notebookEntries)
            {
                var s = db != null ? db.Find(id) : null;
                if (s != null) AddInternal(s);
                else Debug.LogWarning($"[Notebook] Không tìm thấy mục '{id}' trong Database.");
            }
            foreach (var id in data.notebookSeen) seen.Add(id);
            Changed?.Invoke();
        }
    }
}
