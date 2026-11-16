using System;
using System.Collections.Generic;
using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Notebook
{
    /// <summary>Manh mối chỉ thấy được qua ảnh chụp flash (Phase 3 dùng).</summary>
    [CreateAssetMenu(menuName = "Blackwood/Photo Clue", fileName = "CLUE_")]
    public class PhotoClueData : NotebookSource
    {
        public StoryFlag flagOnCapture = StoryFlag.None;
        public override NotebookTab Tab => NotebookTab.Photos;
    }
}
