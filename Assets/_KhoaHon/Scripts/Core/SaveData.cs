using System;
using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.Core
{
    /// <summary>
    /// Dữ liệu lưu game (JSON). Không lưu pixel ảnh, chỉ lưu id manh mối.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>v1: bản Khóa Hồn gốc. v2: thêm nhân vật.</summary>
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        /// <summary>Tên enum PlayerCharacter.</summary>
        public string character = PlayerCharacter.Evelyn.ToString();
        public string stage = GameStage.Stage0_Gate.ToString();
        public string checkpointId = "";
        public List<string> flags = new List<string>();
        /// <summary>Id các Pickup/ReadableDocument đã nhặt (không xuất hiện lại).</summary>
        public List<string> collected = new List<string>();
        /// <summary>Id các cửa đã mở khóa.</summary>
        public List<string> unlockedDoors = new List<string>();
        /// <summary>Id chìa khóa đang có.</summary>
        public List<string> keys = new List<string>();
        public List<string> notebookEntries = new List<string>();
        public List<string> notebookSeen = new List<string>();
        public List<string> photoClues = new List<string>();
        public float battery = 100f;
        public int film = 8;
        public float sanity = 100f;

        public string ToJson(bool pretty = true) => JsonUtility.ToJson(this, pretty);

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null) return null;
                d.flags ??= new List<string>();
                d.collected ??= new List<string>();
                d.unlockedDoors ??= new List<string>();
                d.keys ??= new List<string>();
                d.notebookEntries ??= new List<string>();
                d.notebookSeen ??= new List<string>();
                d.photoClues ??= new List<string>();
                // v1 → v2: chưa có nhân vật thì mặc định Evelyn
                if (d.version < 2 || string.IsNullOrEmpty(d.character)) d.character = PlayerCharacter.Evelyn.ToString();
                d.version = CurrentVersion;
                return d;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public PlayerCharacter CharacterValue =>
            Enum.TryParse(character, out PlayerCharacter c) ? c : PlayerCharacter.Evelyn;

        public GameStage StageValue =>
            Enum.TryParse(stage, out GameStage s) ? s : GameStage.Stage0_Gate;
    }
}
