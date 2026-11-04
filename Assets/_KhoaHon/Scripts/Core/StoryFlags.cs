using System;
using System.Collections.Generic;

namespace KhoaHon.Core
{
    /// <summary>
    /// Tập cờ cốt truyện (C# thuần, test được ở EditMode).
    /// </summary>
    public class StoryFlags
    {
        private readonly HashSet<StoryFlag> flags = new HashSet<StoryFlag>();

        /// <summary>Gọi khi một cờ thay đổi trạng thái (cờ, giá trị mới).</summary>
        public event Action<StoryFlag, bool> Changed;

        public int Count => flags.Count;

        public bool Has(StoryFlag flag) => flag != StoryFlag.None && flags.Contains(flag);

        /// <summary>Bật cờ. Trả về true nếu cờ vừa được bật lần đầu.</summary>
        public bool Set(StoryFlag flag)
        {
            if (flag == StoryFlag.None || !flags.Add(flag)) return false;
            Changed?.Invoke(flag, true);
            return true;
        }

        public bool Clear(StoryFlag flag)
        {
            if (!flags.Remove(flag)) return false;
            Changed?.Invoke(flag, false);
            return true;
        }

        public void ClearAll() => flags.Clear();

        public bool HasAll(params StoryFlag[] required)
        {
            foreach (var f in required) if (!Has(f)) return false;
            return true;
        }

        /// <summary>Danh sách tên cờ (lưu bằng tên để bền vững khi enum đổi thứ tự).</summary>
        public List<string> ToNames()
        {
            var list = new List<string>(flags.Count);
            foreach (var f in flags) list.Add(f.ToString());
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        /// <summary>Nạp lại từ danh sách tên, bỏ qua tên không còn tồn tại. Không phát sự kiện.</summary>
        public void LoadNames(IEnumerable<string> names)
        {
            flags.Clear();
            if (names == null) return;
            foreach (var n in names)
            {
                if (Enum.TryParse(n, out StoryFlag f) && f != StoryFlag.None) flags.Add(f);
            }
        }
    }
}
