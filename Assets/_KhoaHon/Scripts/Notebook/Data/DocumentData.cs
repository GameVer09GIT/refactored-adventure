using System;
using System.Collections.Generic;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Notebook
{
    /// <summary>Tài liệu đọc được (thư, báo, bảng đồng, sổ…).</summary>
    [CreateAssetMenu(menuName = "KhoaHon/Document", fileName = "DOC_")]
    public class DocumentData : NotebookSource
    {
        [Tooltip("Cờ bật khi đọc lần đầu (tùy chọn)")]
        public StoryFlag flagOnRead = StoryFlag.None;
        public override NotebookTab Tab => NotebookTab.Documents;
    }
}
