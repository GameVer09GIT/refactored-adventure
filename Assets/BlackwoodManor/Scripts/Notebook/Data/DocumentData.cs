using System;
using System.Collections.Generic;
using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Notebook
{
    /// <summary>Tài liệu đọc được (thư, báo, bảng đồng, sổ…).</summary>
    [CreateAssetMenu(menuName = "Blackwood/Document", fileName = "DOC_")]
    public class DocumentData : NotebookSource
    {
        [Tooltip("Cờ bật khi đọc lần đầu (tùy chọn)")]
        public StoryFlag flagOnRead = StoryFlag.None;
        public override NotebookTab Tab => NotebookTab.Documents;
    }
}
