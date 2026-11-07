using System;
using System.Collections.Generic;
using KhoaHon.Core;
using UnityEngine;

namespace KhoaHon.Notebook
{
    /// <summary>Băng cassette của Quân: phụ đề theo thời gian + clip lồng tiếng (có thể trống).</summary>
    [CreateAssetMenu(menuName = "KhoaHon/Tape", fileName = "TAPE_")]
    public class TapeData : NotebookSource
    {
        [Serializable]
        public class Line
        {
            [Tooltip("Thời điểm hiện (giây)")] public float time;
            [Tooltip("Thời lượng hiện (giây)")] public float duration = 4f;
            [TextArea(1, 4)] public string text;
            [TextArea(1, 4)] public string textEn;
            public string Text => Loc.Pick(text, textEn);
        }

        public AudioClip clip;
        public List<Line> lines = new List<Line>();
        [Tooltip("Sanity hồi một lần khi nghe hết băng")]
        public float sanityRestore = 5f;
        public override NotebookTab Tab => NotebookTab.Tapes;
    }
}
