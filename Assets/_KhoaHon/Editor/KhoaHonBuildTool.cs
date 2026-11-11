using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Build bản Windows 64-bit của Blackwood Manor: chỉ gồm KH_Main (menu nằm trong scene),
    /// xuất ra Builds/BlackwoodManor/ (thư mục đã bị .gitignore). Không đổi Build Settings của dự án.
    /// </summary>
    public static class KhoaHonBuildTool
    {
        public const string OutDir = "Builds/BlackwoodManor";
        public const string ExeName = "BlackwoodManor.exe";

        [MenuItem("KhoaHon/Build/5. Windows Build (Blackwood Manor)")]
        public static void Menu() => Debug.Log(Build(false));

        public static string Build(bool development)
        {
            if (EditorApplication.isPlaying) return "Đang Play.";
            Directory.CreateDirectory(OutDir);
            // Mở lại KH_Main từ đĩa (bản converter đã lưu) để không bật hộp thoại "Scene đã thay đổi"
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_KhoaHon/Scenes/KH_Main.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            PlayerSettings.productName = "Blackwood Manor";
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_KhoaHon/Scenes/KH_Main.unity" },
                locationPathName = Path.Combine(OutDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            string msg = $"[KhoaHonBuild] {s.result} · {s.totalSize / (1024f * 1024f):0.0} MB · {s.totalTime.TotalMinutes:0.0} phút · lỗi {s.totalErrors} · cảnh báo {s.totalWarnings} → {opts.locationPathName}";
            File.WriteAllText(Path.Combine(OutDir, "build_report.txt"), msg);
            return msg;
        }
    }
}
