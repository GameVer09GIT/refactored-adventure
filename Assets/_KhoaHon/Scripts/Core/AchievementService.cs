using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>Cổng thành tựu ngoài (Steamworks…). Gắn bằng <see cref="AchievementService.Backend"/> khi có AppID.</summary>
    public interface IAchievementBackend
    {
        void Unlock(string id);
    }

    /// <summary>
    /// Thành tựu nội bộ (PlayerPrefs "kh_ach_*") + thông báo trên màn hình. Tự nghe GameEvents để mở khóa.
    /// Danh sách id cố định — chữ hiển thị ở TextTable "ach.{id}.title" / "ach.{id}.desc".
    /// </summary>
    public static class AchievementService
    {
        public static readonly string[] All =
        {
            "end_release", "end_master", "end_coward", "both_characters", "no_death",
            "safe_cracked", "found_julian", "first_flash", "all_relics", "cellar_open", "archivist",
        };

        public static IAchievementBackend Backend;
        private static bool initialized;

        public static bool IsUnlocked(string id) => PlayerPrefs.GetInt("kh_ach_" + id, 0) == 1;

        public static int UnlockedCount { get { int n = 0; foreach (var a in All) if (IsUnlocked(a)) n++; return n; } }

        public static void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || IsUnlocked(id)) return;
            PlayerPrefs.SetInt("kh_ach_" + id, 1);
            PlayerPrefs.Save();
            Backend?.Unlock(id);
            KhoaHonHUD.Toast(T.Format("ach.unlocked", T.Get("ach." + id + ".title")));
        }

        /// <summary>Gọi từ GameManager.Awake; đăng ký một lần cho cả phiên.</summary>
        public static void Init()
        {
            if (initialized) return;
            initialized = true;
            GameEvents.FlagSet += OnFlag;
        }

        private static void OnFlag(StoryFlag f)
        {
            switch (f)
            {
                case StoryFlag.SafeOpened: Unlock("safe_cracked"); break;
                case StoryFlag.FoundQuan: Unlock("found_julian"); break;
                case StoryFlag.CellarOpened: Unlock("cellar_open"); break;
            }
            var gm = GameManager.Instance;
            if (gm != null)
            {
                bool all = true;
                for (int i = 0; i < 5; i++) if (!gm.Has(HanhUtil.RelicFlag((Hanh)i))) all = false;
                if (all) Unlock("all_relics");
            }
        }

        /// <summary>Gọi khi một kết thúc hoàn tất.</summary>
        public static void OnEnding(int endingIndex, PlayerCharacter character, int deaths)
        {
            Unlock(endingIndex == 0 ? "end_release" : endingIndex == 1 ? "end_master" : "end_coward");
            PlayerPrefs.SetInt("kh_char_done_" + character, 1);
            if (PlayerPrefs.GetInt("kh_char_done_" + PlayerCharacter.Evelyn, 0) == 1 && PlayerPrefs.GetInt("kh_char_done_" + PlayerCharacter.Alex, 0) == 1)
                Unlock("both_characters");
            if (deaths == 0 && endingIndex != 2) Unlock("no_death");
        }

        public static void ResetAll()
        {
            foreach (var a in All) PlayerPrefs.DeleteKey("kh_ach_" + a);
        }
    }
}
