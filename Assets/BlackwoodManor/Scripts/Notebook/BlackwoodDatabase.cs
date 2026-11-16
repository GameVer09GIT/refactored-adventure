using System.Collections.Generic;
using UnityEngine;

namespace Blackwood.Notebook
{
    /// <summary>
    /// Danh mục toàn bộ dữ liệu Sổ tay để tra id → dữ liệu khi nạp file lưu.
    /// </summary>
    [CreateAssetMenu(menuName = "Blackwood/Database", fileName = "BW_Database")]
    public class BlackwoodDatabase : ScriptableObject
    {
        public List<NotebookSource> sources = new List<NotebookSource>();

        private Dictionary<string, NotebookSource> map;

        public NotebookSource Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (map == null || map.Count != sources.Count)
            {
                map = new Dictionary<string, NotebookSource>();
                foreach (var s in sources) if (s != null && !string.IsNullOrEmpty(s.Id)) map[s.Id] = s;
            }
            return map.TryGetValue(id, out var r) ? r : null;
        }

        private void OnValidate() => map = null;
    }
}
