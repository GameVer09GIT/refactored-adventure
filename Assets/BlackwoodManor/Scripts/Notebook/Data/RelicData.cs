using System;
using System.Collections.Generic;
using Blackwood.Core;
using UnityEngine;

namespace Blackwood.Notebook
{
    /// <summary>Kỷ vật của một Mắt Trận. Nhặt kỷ vật = nhận luôn trang bùa đi kèm.</summary>
    [CreateAssetMenu(menuName = "Blackwood/Relic", fileName = "REL_")]
    public class RelicData : NotebookSource
    {
        public Hanh hanh;
        public TalismanData talisman;
        [Tooltip("Lời thì thầm/phụ đề khi nhặt")]
        [TextArea(1, 3)] public string pickupLine;
        [TextArea(1, 3)] public string pickupLineEn;
        public string PickupLine => Loc.Pick(pickupLine, pickupLineEn);
        public override NotebookTab Tab => NotebookTab.Relics;
    }
}
