namespace KhoaHon.EditorTools
{
    /// <summary>Chữ song ngữ cho cutscene: vòng lặp cổng, xà lim Julian, Sảnh Tế, 3 kết thúc.</summary>
    public static partial class KhoaHonContent
    {
        public static readonly (string key, string vi, string en)[] TextSequences =
        {
            // Vòng lặp cổng
            ("gate.hint", "Giữ [W] để lái đi. [S] / [Space] để phanh. Chuột để nhìn quanh.", "Hold [W] to drive away. [S] / [Space] to brake. Mouse to look around."),
            ("gate.wake", "…Tôi tỉnh dậy. Chiếc xe vẫn đỗ trước cổng dinh thự.", "…I come to. The car is parked in front of the manor gate again."),
            ("gate.again", "Không thể nào. Thử lại.", "That's impossible. Try again."),
            ("gate.sign.1", "ANNABELLE  1924", "ANNABELLE  1924"),
            ("gate.sign.2", "H. STERLING  1996", "H. STERLING  1996"),
            ("gate.sign.3", "JULIAN VANCE", "JULIAN VANCE"),
            ("gate.wake2", "…Lại là cánh cổng ấy. Ngôi nhà không cho tôi đi.", "…The same gate. The house won't let me leave."),
            ("gate.try", "Lần cuối.", "One last time."),
            ("gate.dead", "Động cơ chết hẳn. Đèn pha tắt ngấm.", "The engine dies. The headlights go dark."),
            ("gate.open", "Cánh cổng sắt tự mở ra. Nó đang mời tôi vào.", "The iron gate swings open by itself. It's inviting me in."),

            // Xà lim Julian
            ("prompt.julian", "[E] Xem thi thể", "[E] Examine the body"),
            ("prompt.julian_seen", "[E] Julian…", "[E] Julian…"),
            ("toast.whistle", "Đã nhặt: chiếc còi nhỏ của Julian", "Picked up: Julian's little whistle"),
            ("julian.reaction.alex", "Julian… em ơi. Anh xin lỗi. Anh đến muộn quá.", "Julian… little brother. I'm sorry. I came too late."),
            ("julian.reaction.evelyn", "Cậu đã đi xa hơn bất kỳ ai, Julian. Tôi sẽ kết thúc chuyện này.", "You came further than anyone, Julian. I'll finish this."),

            // Thoại Lord khi nói với Alex (Julian là em trai)
            ("lord.2.alex", "Em trai anh đã đi xa hơn bất kỳ ai. Nhưng nó từ chối món quà của ta. Thật phí phạm.", "Your little brother came further than anyone. But he refused my gift. Such a waste."),
            ("lord.3.alex", "Nhỏ một giọt máu lên Cổ Vật, đọc ngược bài chú thứ năm. Mười ba linh hồn sẽ quỳ dưới chân anh. Và hàng triệu người sẽ xem anh. Mãi mãi.", "One drop of blood upon the Relic. Read the fifth verse backwards. Thirteen souls will kneel at your feet. And millions will watch you. Forever."),
            ("lord.4.alex", "Hay anh định đập vỡ nó? Ngôi nhà sẽ sập. Anh sẽ chẳng còn gì — kể cả nó.", "Or will you break it? The house will fall. You'll have nothing — not even him."),

            // Sảnh Tế
            ("ritual.choose", "Lord Blackwood chờ câu trả lời của bạn.", "Lord Blackwood awaits your answer."),
            ("ritual.need_relics", "(cần đủ 5 kỷ vật và búa của Thomas)", "(needs all 5 relics and Thomas's hammer)"),
            ("ritual.need_verse", "(cần bài chú thứ năm)", "(needs the fifth verse)"),

            // Kết 1
            ("end.1.a", "Từng kỷ vật một rơi vào vạc máu đen.", "One by one, the relics fall into the black cauldron."),
            ("end.1.b", "Chiếc búa của Thomas nặng trĩu trong tay.", "Thomas's hammer is heavy in your hands."),
            ("end.1.c", "Mười ba sợi xích linh hồn đứt tung cùng lúc.", "Thirteen soul-chains snap at once."),
            ("end.1.d", "Bình minh. Lần đầu tiên sau một trăm năm.", "Dawn. For the first time in a hundred years."),
            // Kết 2
            ("end.2.a", "Một giọt máu rơi lên mặt đá đen. Nó ấm lên.", "A drop of blood falls on the black stone. It grows warm."),
            ("end.2.b", "Mười ba oan hồn quỳ xuống. Ngôi nhà đã có chủ mới.", "Thirteen souls kneel. The house has a new master."),
            ("end.2.later", "Vài tháng sau.", "Months later."),
            ("end.2.quote", "\"Vào đi… nhà đang đợi các bạn.\"", "\"Come in… the house has been waiting for you.\""),
            // Kết ẩn
            ("end.3.a", "Hai giờ sáng. Tôi đã thoát. Tôi đã thoát…", "Two in the morning. I got out. I got out…"),
            ("end.3.b", "Bệnh viện tâm thần Ashford. Phòng 13.", "Ashford Asylum. Room 13."),
        };
    }
}
