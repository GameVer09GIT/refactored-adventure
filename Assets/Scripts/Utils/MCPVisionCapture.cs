using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MCPVisionCapture
{
    public static string Capture(string relativeOrFullPath = "Temp/mcp_view_check.png", int width = 1280, int height = 720)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindAnyObjectByType<Camera>();
        }
        if (cam == null)
        {
            Debug.LogError("[MCPVisionCapture] Không tìm thấy Camera nào trong scene!");
            return "ERROR: No camera found";
        }

        string fullPath = Path.IsPathRooted(relativeOrFullPath)
            ? relativeOrFullPath
            : Path.Combine(Directory.GetCurrentDirectory(), relativeOrFullPath);

        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevRT = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRT;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(fullPath, bytes);

        Debug.Log($"[MCPVisionCapture] Đã chụp ảnh thành công lưu tại: {fullPath}");
        return fullPath;
    }

#if UNITY_EDITOR
    [MenuItem("Tools/MCP/Capture View Check")]
    public static void MenuItemCapture()
    {
        Capture();
    }
#endif
}
