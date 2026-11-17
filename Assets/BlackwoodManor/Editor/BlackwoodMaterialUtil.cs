using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Vật liệu URP tạo bằng script phải ở đúng trạng thái mà ShaderGUI của URP sẽ tính ra, nếu không Unity tự "kiểm tra lại"
    /// (khi nhập lại asset, khi mở Inspector, khi vào Play…) rồi ghi đè: tắt phát sáng, đổi blend, đồng bộ _Color.
    /// Trên Unity 6, keyword _EMISSION chỉ được giữ khi cờ GI có RealtimeEmissive/BakedEmissive — cờ None hay EmissiveIsBlack
    /// (mặc định của vật liệu mới) đều làm mất phát sáng. Đây là nguyên nhân lỗi cũ "vật liệu tự đổi sau khi Play"
    /// (M_LordGhost, M_HK_B_Eyes…), khiến mắt bà quản gia không đỏ/lóe được và bóng ma không phát sáng.
    /// </summary>
    public static class BlackwoodMaterialUtil
    {
        /// <summary>Bật phát sáng sao cho URP không tắt lại: màu, map (tùy chọn), cờ RealtimeEmissive, keyword.</summary>
        public static void Emissive(Material m, Color color, Texture map = null)
        {
            if (m == null) return;
            m.SetColor("_EmissionColor", color);
            if (map != null) m.SetTexture("_EmissionMap", map);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.EnableKeyword("_EMISSION");
        }

        /// <summary>Chạy bước kiểm tra của ShaderGUI ngay lúc dựng để thứ được lưu đúng là thứ hiển thị (không còn diff sau khi Play).</summary>
        public static void Validate(Material m)
        {
            if (m == null || m.shader == null) return;
            var ed = (MaterialEditor)Editor.CreateEditor(m);
            try
            {
                if (ed != null && ed.customShaderGUI != null) ed.customShaderGUI.ValidateMaterial(m);
            }
            finally { if (ed != null) Object.DestroyImmediate(ed); }
            EditorUtility.SetDirty(m);
        }
    }
}
