using System;
using UnityEngine;

namespace KhoaHon.Core
{
    public enum NoiseType { Footstep, Run, PropFall, Safe, Altar, Other }

    /// <summary>
    /// Sự kiện toàn cục (static C# events). Các hệ thống đăng ký trong OnEnable và hủy trong OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<StoryFlag> FlagSet;
        public static event Action<GameStage> StageChanged;
        public static event Action<Vector3, float, NoiseType> NoiseEmitted;
        public static event Action<string> PhotoTaken;
        public static event Action<string> ItemCollected;
        public static event Action PlayerCaught;
        public static event Action<int> SanityTierChanged;
        public static event Action<string> CheckpointReached;

        public static void RaiseFlagSet(StoryFlag f) => FlagSet?.Invoke(f);
        public static void RaiseStageChanged(GameStage s) => StageChanged?.Invoke(s);
        public static void RaiseNoise(Vector3 pos, float radius, NoiseType type) => NoiseEmitted?.Invoke(pos, radius, type);
        public static void RaisePhotoTaken(string clueId) => PhotoTaken?.Invoke(clueId);
        public static void RaiseItemCollected(string id) => ItemCollected?.Invoke(id);
        public static void RaisePlayerCaught() => PlayerCaught?.Invoke();
        public static void RaiseSanityTierChanged(int tier) => SanityTierChanged?.Invoke(tier);
        public static void RaiseCheckpointReached(string id) => CheckpointReached?.Invoke(id);

        /// <summary>Xóa toàn bộ subscriber (gọi khi nạp lại scene để tránh tham chiếu treo).</summary>
        public static void ResetAll()
        {
            FlagSet = null; StageChanged = null; NoiseEmitted = null; PhotoTaken = null;
            ItemCollected = null; PlayerCaught = null; SanityTierChanged = null; CheckpointReached = null;
        }
    }
}
