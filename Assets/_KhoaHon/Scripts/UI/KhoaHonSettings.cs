using System.Collections.Generic;
using System.Linq;
using KhoaHon.Core;
using KhoaHon.Horror;
using UnityEngine;

namespace KhoaHon.UI
{
    /// <summary>Cài đặt người chơi (PlayerPrefs) + áp dụng vào game.</summary>
    public static class KhoaHonSettings
    {
        public static int ScreenMode = 1;        // 0 Toàn màn hình độc quyền, 1 Toàn màn hình không viền, 2 Cửa sổ
        public static int ResolutionIndex = -1;  // -1 = độ phân giải hiện tại
        public static bool VSync = true;
        public static int FpsCapIndex = 1;       // 30, 60, 120, 144, không giới hạn
        public static int Quality = -1;
        public static float Fov = 72f;
        public static float Brightness = 0f;
        public static bool FilmGrain = true, MotionBlur = true, ShowFps = false, Subtitles = true;
        public static float Master = 0.9f, Music = 0.8f, Sfx = 1f, Ambience = 0.85f, Voice = 1f;
        public static float Sensitivity = 15f;
        public static bool BrightnessCalibrated;

        public static readonly int[] FpsCaps = { 30, 60, 120, 144, -1 };
        static bool loaded;

        public static List<Resolution> Resolutions()
        {
            var list = Screen.resolutions.GroupBy(r => (r.width, r.height)).Select(g => g.Last()).OrderBy(r => r.width * r.height).ToList();
            if (list.Count == 0) list.Add(Screen.currentResolution);
            return list;
        }

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            ScreenMode = PlayerPrefs.GetInt("kh_screen", ScreenMode);
            ResolutionIndex = PlayerPrefs.GetInt("kh_res", ResolutionIndex);
            VSync = PlayerPrefs.GetInt("kh_vsync", VSync ? 1 : 0) == 1;
            FpsCapIndex = PlayerPrefs.GetInt("kh_fps", FpsCapIndex);
            Quality = PlayerPrefs.GetInt("kh_quality", QualitySettings.GetQualityLevel());
            Fov = PlayerPrefs.GetFloat("kh_fov", Fov);
            Brightness = PlayerPrefs.GetFloat("kh_bright", Brightness);
            FilmGrain = PlayerPrefs.GetInt("kh_grain", 1) == 1;
            MotionBlur = PlayerPrefs.GetInt("kh_blur", 1) == 1;
            ShowFps = PlayerPrefs.GetInt("kh_showfps", 0) == 1;
            Subtitles = PlayerPrefs.GetInt("kh_subs", 1) == 1;
            Master = PlayerPrefs.GetFloat("kh_vol_master", Master);
            Music = PlayerPrefs.GetFloat("kh_vol_music", Music);
            Sfx = PlayerPrefs.GetFloat("kh_vol_sfx", Sfx);
            Ambience = PlayerPrefs.GetFloat("kh_vol_amb", Ambience);
            Voice = PlayerPrefs.GetFloat("kh_vol_voice", Voice);
            Sensitivity = PlayerPrefs.GetFloat("kh_sens", Sensitivity);
            BrightnessCalibrated = PlayerPrefs.GetInt("kh_bright_set", 0) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("kh_screen", ScreenMode); PlayerPrefs.SetInt("kh_res", ResolutionIndex);
            PlayerPrefs.SetInt("kh_vsync", VSync ? 1 : 0); PlayerPrefs.SetInt("kh_fps", FpsCapIndex);
            PlayerPrefs.SetInt("kh_quality", Quality); PlayerPrefs.SetFloat("kh_fov", Fov); PlayerPrefs.SetFloat("kh_bright", Brightness);
            PlayerPrefs.SetInt("kh_grain", FilmGrain ? 1 : 0); PlayerPrefs.SetInt("kh_blur", MotionBlur ? 1 : 0);
            PlayerPrefs.SetInt("kh_showfps", ShowFps ? 1 : 0); PlayerPrefs.SetInt("kh_subs", Subtitles ? 1 : 0);
            PlayerPrefs.SetFloat("kh_vol_master", Master); PlayerPrefs.SetFloat("kh_vol_music", Music); PlayerPrefs.SetFloat("kh_vol_sfx", Sfx);
            PlayerPrefs.SetFloat("kh_vol_amb", Ambience); PlayerPrefs.SetFloat("kh_vol_voice", Voice);
            PlayerPrefs.SetFloat("kh_sens", Sensitivity); PlayerPrefs.SetInt("kh_bright_set", BrightnessCalibrated ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetDefaults()
        {
            ScreenMode = 1; ResolutionIndex = -1; VSync = true; FpsCapIndex = 1; Quality = QualitySettings.names.Length - 1;
            Fov = 72f; Brightness = 0f; FilmGrain = true; MotionBlur = true; ShowFps = false; Subtitles = true;
            Master = 0.9f; Music = 0.8f; Sfx = 1f; Ambience = 0.85f; Voice = 1f; Sensitivity = 15f;
        }

        /// <summary>Áp dụng cài đặt âm thanh/hiển thị nhẹ (gọi liên tục khi kéo slider).</summary>
        public static void ApplyLive()
        {
            AudioListener.volume = Master;
            AudioMix.Music = Music; AudioMix.Sfx = Sfx; AudioMix.Ambience = Ambience; AudioMix.Voice = Voice; AudioMix.Notify();
            var fx = HorrorPostFX.Instance;
            if (fx != null) { fx.Brightness = Brightness; fx.FilmGrainEnabled = FilmGrain; fx.MotionBlurEnabled = MotionBlur; }
            var pc = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                pc.mouseSensitivity = Sensitivity;
                var cam = pc.playerCamera != null ? pc.playerCamera.GetComponent<Camera>() : null;
                if (cam != null) cam.fieldOfView = Fov;
            }
            KhoaHonHUD.SubtitlesEnabled = Subtitles;
            HudExtras.ShowFps = ShowFps;
        }

        /// <summary>Áp dụng toàn bộ (cả màn hình/chất lượng — có thể gây nháy).</summary>
        public static void ApplyAll()
        {
            ApplyLive();
            if (Quality >= 0 && Quality < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != Quality) QualitySettings.SetQualityLevel(Quality, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FpsCaps[Mathf.Clamp(FpsCapIndex, 0, FpsCaps.Length - 1)];
#if !UNITY_EDITOR
            var mode = ScreenMode == 0 ? FullScreenMode.ExclusiveFullScreen : ScreenMode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            var res = Resolutions();
            var r = ResolutionIndex >= 0 && ResolutionIndex < res.Count ? res[ResolutionIndex] : Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, mode);
#endif
        }
    }
}
