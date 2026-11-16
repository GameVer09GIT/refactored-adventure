namespace KhoaHon.Core
{
    /// <summary>
    /// Độ khó (Cài đặt › Trò chơi): 0 = Dễ, 1 = Thường, 2 = Ác mộng. Chỉ là các hệ số nhân lên thông số trong
    /// HousekeeperConfig / ToolsConfig — không đổi luật chơi, đổi được giữa chừng.
    /// </summary>
    public static class Difficulty
    {
        public static int Level = 1;

        private static float Pick(float easy, float normal, float hard) => Level <= 0 ? easy : (Level >= 2 ? hard : normal);

        /// <summary>Tốc độ truy đuổi của Mrs. Peake.</summary>
        public static float HuntSpeed => Pick(0.9f, 1f, 1.07f);
        /// <summary>Tầm nhìn của bà.</summary>
        public static float SightRange => Pick(0.85f, 1f, 1.15f);
        /// <summary>Thời gian phải rọi đèn để làm bà chói mắt.</summary>
        public static float LightFillTime => Pick(0.8f, 1f, 1.3f);
        /// <summary>Thời gian bà nhờn đèn sau mỗi lần chói.</summary>
        public static float LightImmuneTime => Pick(0.7f, 1f, 1.4f);
    }
}
