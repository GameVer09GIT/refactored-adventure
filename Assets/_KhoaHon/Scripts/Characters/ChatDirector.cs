using KhoaHon.Core;

namespace KhoaHon.Characters
{
    /// <summary>Mức đe dọa của Mrs. Peake đối với Alex (đầu vào của khung chat).</summary>
    public enum Threat { None = 0, Near = 1, Hunting = 2 }

    /// <summary>
    /// Logic thuần chọn nhóm tin nhắn cho livestream của Alex:
    /// càng vào sâu trong nhà càng ít người trêu, càng nhiều tài khoản ma (không UID) báo trước nguy hiểm.
    /// </summary>
    public static class ChatDirector
    {
        /// <summary>Tiền tố key TextTable của nhóm tin nhắn (vd "chat.s1", "chat.ghost.near").</summary>
        public static string PickPool(GameStage stage, Threat threat, float roll01)
        {
            int depth = Depth(stage);
            // Tài khoản ma chỉ xuất hiện từ tầng 2 trở đi, và luôn lên tiếng khi bà ta đang săn
            if (depth >= 2 && threat == Threat.Hunting) return "chat.ghost.hunt";
            if (depth >= 2 && threat == Threat.Near && roll01 < 0.7f) return "chat.ghost.near";
            float ghostChance = depth >= 3 ? 0.35f : depth == 2 ? 0.12f : 0f;
            if (roll01 > 1f - ghostChance) return "chat.ghost.idle";
            return "chat.s" + depth;
        }

        public static int Depth(GameStage stage)
        {
            switch (stage)
            {
                case GameStage.Stage0_Gate: return 0;
                case GameStage.Stage1_GroundFloor: return 1;
                case GameStage.Stage2_Floor2: return 2;
                default: return 3;
            }
        }

        /// <summary>Số người xem mục tiêu theo độ sâu (stream "nóng" dần).</summary>
        public static int TargetViewers(GameStage stage, Threat threat)
        {
            int[] baseV = { 312, 540, 1280, 3900 };
            int v = baseV[Depth(stage)];
            if (threat == Threat.Hunting) v = (int)(v * 1.6f);
            return v;
        }
    }
}
