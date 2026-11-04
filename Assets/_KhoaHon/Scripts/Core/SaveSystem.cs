using System.IO;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Đọc/ghi file lưu ở Application.persistentDataPath/khoahon_save.json.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "khoahon_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Dữ liệu chờ áp dụng sau khi nạp lại scene (Tiếp tục / Tải).</summary>
        public static SaveData PendingLoad;

        public static bool Exists => File.Exists(FilePath);

        public static void Write(SaveData data)
        {
            if (data == null) return;
            File.WriteAllText(FilePath, data.ToJson());
        }

        public static SaveData Read()
        {
            if (!Exists) return null;
            return SaveData.FromJson(File.ReadAllText(FilePath));
        }

        public static void Delete()
        {
            if (Exists) File.Delete(FilePath);
        }
    }
}
