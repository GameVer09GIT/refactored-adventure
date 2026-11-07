using System.IO;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// 3 ô lưu ở Application.persistentDataPath/khoahon_save_{0..2}.json. Các hàm Exists/Read/Write/Delete
    /// làm việc trên ô hiện tại (<see cref="CurrentSlot"/>, nhớ trong PlayerPrefs). File cũ khoahon_save.json
    /// (bản 1 ô) tự chuyển sang ô 0 lần đầu chạy.
    /// </summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;
        public const string LegacyFileName = "khoahon_save.json";
        private const string SlotPref = "kh_slot";
        private static bool migrated;

        /// <summary>Dữ liệu chờ áp dụng sau khi nạp lại scene (Tiếp tục / Tải).</summary>
        public static SaveData PendingLoad;

        public static int CurrentSlot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(SlotPref, 0), 0, SlotCount - 1);
            set { PlayerPrefs.SetInt(SlotPref, Mathf.Clamp(value, 0, SlotCount - 1)); PlayerPrefs.Save(); }
        }

        public static string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"khoahon_save_{slot}.json");
        public static string FilePath => SlotPath(CurrentSlot);

        public static bool Exists { get { Migrate(); return File.Exists(FilePath); } }
        public static bool SlotExists(int slot) { Migrate(); return File.Exists(SlotPath(slot)); }
        public static bool AnyExists { get { for (int i = 0; i < SlotCount; i++) if (SlotExists(i)) return true; return false; } }

        public static void Write(SaveData data)
        {
            if (data == null) return;
            data.savedAtTicks = System.DateTime.Now.Ticks;
            File.WriteAllText(FilePath, data.ToJson());
        }

        public static SaveData Read() => ReadSlot(CurrentSlot);

        public static SaveData ReadSlot(int slot)
        {
            if (!SlotExists(slot)) return null;
            try { return SaveData.FromJson(File.ReadAllText(SlotPath(slot))); }
            catch (IOException) { return null; }
        }

        public static void Delete() => DeleteSlot(CurrentSlot);

        public static void DeleteSlot(int slot)
        {
            if (SlotExists(slot)) File.Delete(SlotPath(slot));
        }

        /// <summary>Ô lưu mới nhất (cho nút Tiếp tục); -1 nếu chưa có.</summary>
        public static int MostRecentSlot()
        {
            int best = -1; long bestTicks = long.MinValue;
            for (int i = 0; i < SlotCount; i++)
            {
                var d = ReadSlot(i);
                if (d == null) continue;
                if (d.savedAtTicks > bestTicks) { bestTicks = d.savedAtTicks; best = i; }
            }
            return best;
        }

        private static void Migrate()
        {
            if (migrated) return;
            migrated = true;
            try
            {
                string legacy = Path.Combine(Application.persistentDataPath, LegacyFileName);
                if (File.Exists(legacy) && !File.Exists(SlotPath(0))) File.Move(legacy, SlotPath(0));
            }
            catch (IOException) { }
        }
    }
}
