namespace KhoaHon.Core
{
    /// <summary>Trạng thái người chơi dùng chung giữa các hệ thống (không phụ thuộc chéo namespace).</summary>
    public static class PlayerState
    {
        /// <summary>Vy đang trốn trong tủ / gầm giường.</summary>
        public static bool IsHiding;
        /// <summary>Đang nín thở (mini-game).</summary>
        public static bool IsHoldingBreath;
    }
}
