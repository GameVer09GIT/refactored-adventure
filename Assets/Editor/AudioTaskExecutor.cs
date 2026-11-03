using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorAudio.Editor
{
    [InitializeOnLoad]
    public static class AudioTaskExecutor
    {
        private const string DoneMarkerFile = "Temp/AudioTaskExecutor_Done.txt";

        static AudioTaskExecutor()
        {
            EditorApplication.delayCall += ExecuteAllTasksOnce;
        }

        [MenuItem("Tools/Audio/Run Full Audio Fix and Verification", false, 5)]
        public static void RunManualExecution()
        {
            if (File.Exists(DoneMarkerFile)) File.Delete(DoneMarkerFile);
            ExecuteAllTasksOnce();
        }

        private static void ExecuteAllTasksOnce()
        {
            if (File.Exists(DoneMarkerFile))
            {
                return;
            }

            Debug.Log("<color=cyan><b>[AudioTaskExecutor] BẮT ĐẦU THI HÀNH CÁC BƯỚC SỬA ÂM THANH A VÀ B...</b></color>");

            try
            {
                // 1. Cấu hình AudioImporter cho tiengtho_loop.wav và tiengtho.mp3
                PlayerAudioSetupEditor.ConfigureAudioImporter("Assets/Audio/Breathing/tiengtho_loop.wav");
                PlayerAudioSetupEditor.ConfigureAudioImporter("Assets/Audio/Breathing/tiengtho.mp3");

                // 2. Chạy AutoSetupPlayerAudio (thiết lập slot clip sạch, gỡ FootstepSurface sai, tạo FootstepZones)
                PlayerAudioSetupEditor.AutoSetupPlayerAudio(false);

                // 3. Chạy kiểm tra Raycast Lưới 1m
                FootstepZoneManagerEditor.RunVerification();

                // 4. Ghi marker hoàn tất
                File.WriteAllText(DoneMarkerFile, $"Completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Debug.Log("<color=green><b>[AudioTaskExecutor] ĐÃ HOÀN TẤT TOÀN BỘ CÔNG VIỆC VÀ LƯU SCENE!</b></color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioTaskExecutor] LỖI TRONG QUÁ TRÌNH THỰC THI: {ex}");
            }
        }
    }
}
