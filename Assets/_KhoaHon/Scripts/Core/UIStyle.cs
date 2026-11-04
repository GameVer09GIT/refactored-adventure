using TMPro;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>Font và màu dùng chung cho UI Khóa Hồn.</summary>
    [CreateAssetMenu(menuName = "KhoaHon/UI Style", fileName = "KH_UIStyle")]
    public class UIStyle : ScriptableObject
    {
        [Tooltip("Font sans cho UI (Be Vietnam Pro)")]
        public TMP_FontAsset sansFont;
        [Tooltip("Font sans đậm")]
        public TMP_FontAsset sansBoldFont;
        [Tooltip("Font serif cổ cho tài liệu, bùa, bảng đồng (Noto Serif)")]
        public TMP_FontAsset serifFont;
        public TMP_FontAsset serifBoldFont;

        public Color textColor = new Color(0.92f, 0.90f, 0.86f, 1f);
        public Color dimTextColor = new Color(0.65f, 0.62f, 0.58f, 1f);
        public Color accentColor = new Color(0.62f, 0.12f, 0.10f, 1f);
        [Tooltip("Màu giấy cũ của Sổ tay / tài liệu")]
        public Color paperColor = new Color(0.86f, 0.80f, 0.66f, 1f);
        public Color inkColor = new Color(0.16f, 0.11f, 0.08f, 1f);
        public Color panelColor = new Color(0.05f, 0.04f, 0.04f, 0.88f);
    }
}
