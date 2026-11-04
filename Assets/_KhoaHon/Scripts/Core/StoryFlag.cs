namespace KhoaHon.Core
{
    /// <summary>Cờ cốt truyện. Chỉ thêm giá trị mới ở cuối để không lệch dữ liệu đã lưu.</summary>
    public enum StoryFlag
    {
        None = 0,
        GateLoop1, GateLoop2, GateLoop3,
        EnteredHouse,
        GotQuanBackpack,
        HasCrowbar,
        SafeOpened,
        HasUpstairsKey,
        Floor2Entered,
        Relic_Moc, Relic_Thuy, Relic_Tho, Relic_Kim, Relic_Hoa,
        Talisman_Moc_Placed, Talisman_Thuy_Placed, Talisman_Tho_Placed, Talisman_Kim_Placed, Talisman_Hoa_Placed,
        CellarOpened,
        FoundQuan,
        HasWhistle,
        MetDaiQuan,
        Ending1, Ending2, EndingSecret,
    }

    /// <summary>Giai đoạn lớn của chiến dịch.</summary>
    public enum GameStage
    {
        Stage0_Gate,
        Stage1_GroundFloor,
        Stage2_Floor2,
        Stage3_Cellar,
        Stage4_Ritual,
        Ending,
    }

    /// <summary>Ngũ Hành — dùng cho kỷ vật, bùa, đài sen.</summary>
    public enum Hanh
    {
        Moc = 0,
        Thuy = 1,
        Tho = 2,
        Kim = 3,
        Hoa = 4,
    }

    /// <summary>Mốc Biên niên sử dùng để sắp xếp Sổ tay.</summary>
    public enum ChronoYear
    {
        Y1921 = 0,
        Y1924 = 1,
        Y1953 = 2,
        Y1978 = 3,
        Y1996 = 4,
        Y2009 = 5,
        Present = 6,
    }

    public static class HanhUtil
    {
        public static StoryFlag RelicFlag(Hanh h) => StoryFlag.Relic_Moc + (int)h;
        public static StoryFlag TalismanPlacedFlag(Hanh h) => StoryFlag.Talisman_Moc_Placed + (int)h;

        public static string YearLabel(ChronoYear y)
        {
            switch (y)
            {
                case ChronoYear.Y1921: return "1921";
                case ChronoYear.Y1924: return "1924";
                case ChronoYear.Y1953: return "1953";
                case ChronoYear.Y1978: return "1978";
                case ChronoYear.Y1996: return "1996";
                case ChronoYear.Y2009: return "2009";
                default: return "Hiện tại";
            }
        }
    }
}
