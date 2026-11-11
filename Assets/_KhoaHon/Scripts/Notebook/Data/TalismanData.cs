using System;
using System.Collections.Generic;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Notebook
{
    /// <summary>Trang bùa của một hành. Bài chú được Vy tự chép vào sổ.</summary>
    [CreateAssetMenu(menuName = "KhoaHon/Talisman", fileName = "TAL_")]
    public class TalismanData : NotebookSource
    {
        public Hanh hanh;
        [Tooltip("Các câu chú theo đúng thứ tự (Kết 2 đọc ngược)")]
        public List<string> chantLines = new List<string>();
        public List<string> chantLinesEn = new List<string>();
        public List<string> ChantLines => Loc.IsEnglish && chantLinesEn != null && chantLinesEn.Count > 0 ? chantLinesEn : chantLines;
        public Sprite symbol;
        public override NotebookTab Tab => NotebookTab.Talismans;
    }
}
