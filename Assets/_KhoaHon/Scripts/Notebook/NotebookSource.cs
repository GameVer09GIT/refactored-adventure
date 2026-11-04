using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Notebook
{
    public enum NotebookTab
    {
        Documents = 0,
        Tapes = 1,
        Photos = 2,
        Talismans = 3,
        Relics = 4,
    }

    /// <summary>
    /// Lớp cơ sở cho mọi dữ liệu có thể xuất hiện trong Sổ tay (tài liệu, băng, bùa, kỷ vật, manh mối ảnh).
    /// </summary>
    public abstract class NotebookSource : ScriptableObject
    {
        [Tooltip("Id duy nhất, không đổi sau khi phát hành (dùng trong file lưu)")]
        [SerializeField] protected string id;
        [SerializeField] protected string title;
        [SerializeField] protected ChronoYear year = ChronoYear.Present;
        [TextArea(3, 20)]
        [SerializeField] protected string body;
        [Tooltip("Dùng font serif cổ (tài liệu, bùa, bảng đồng)")]
        [SerializeField] protected bool serif = true;

        public string Id => id;
        public string Title => title;
        public ChronoYear Year => year;
        public string Body => body;
        public bool Serif => serif;
        public abstract NotebookTab Tab { get; }

        public void SetBasic(string newId, string newTitle, ChronoYear newYear, string newBody, bool useSerif = true)
        {
            id = newId; title = newTitle; year = newYear; body = newBody; serif = useSerif;
        }
    }
}
