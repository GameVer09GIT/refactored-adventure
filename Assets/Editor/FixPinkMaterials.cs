using UnityEngine;
using UnityEditor;

public class FixPinkMaterials
{
    [MenuItem("Tools/Fix Pink Trees (URP)")]
    public static void FixTreesAndGrass()
    {
        // Quét tìm tất cả Material trong thư mục Flooded_Grounds
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Flooded_Grounds" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // Chuyển đổi sang Shader cơ bản của URP
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");

            // Tự động bật Alpha Clipping cho lá cây và cỏ để hiển thị độ trong suốt
            string matName = mat.name.ToLower();
            if (matName.Contains("leaf") || matName.Contains("tree") || matName.Contains("grass") || matName.Contains("plant"))
            {
                mat.SetFloat("_AlphaClip", 1.0f);
            }

            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Đã sửa thành công " + count + " materials sang URP!");
    }
}