using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Nội dung song ngữ cho công cụ riêng của 2 nhân vật: livestream chat (Alex), EVP + sơ đồ trinh thám (Evelyn).
    /// Nhóm chat được đánh số liên tục (chat.s1.1, chat.s1.2, …) — LivestreamChat tự đếm.
    /// </summary>
    public static partial class KhoaHonContent
    {
        public static readonly (string key, string vi, string en)[] TextCharacters = Build();

        static (string, string, string)[] Build()
        {
            var list = new System.Collections.Generic.List<(string, string, string)>
            {
                ("toast.no_film", "Hết phim", "Out of film"),
                ("toast.no_battery", "Hết pin", "Battery empty"),
                ("sub.flash_stun", "(Bà ta rú lên, lấy tay che mặt.)", "(She shrieks and throws her hands over her face.)"),
                ("chat.viewers", "{0} người xem", "{0} watching"),
                ("mind.title", "SƠ ĐỒ TRINH THÁM", "CASE BOARD"),
                ("mind.hint", "M / Esc: đóng  ·  Chỉ đỏ nối hai manh mối khi bạn đã có cả hai", "M / Esc: close  ·  Red string links two clues once you have both"),
                ("mind.progress", "Đã xâu chuỗi {0}/{1}", "Connected {0}/{1}"),
            };
            void Pool(string prefix, (string vi, string en)[] items)
            {
                for (int i = 0; i < items.Length; i++) list.Add((prefix + "." + (i + 1), items[i].vi, items[i].en));
            }

            // ---- Livestream: tầng ngoài / cổng (trêu chọc)
            Pool("chat.s0", new[]
            {
                ("lại fake à Alex", "fake again alex lol"), ("drive faster bro", "drive faster bro"),
                ("cái nhà kia trông như set phim", "that house looks like a film set"), ("hello từ Việt Nam!!", "hello from Vietnam!!"),
                ("tuần trước kênh này 3k người xem đó", "this channel had 3k viewers last week"), ("ai cá Alex chạy về trong 5 phút", "who bets alex bails in 5 min"),
                ("sương mù fake bằng máy khói à", "is that a fog machine"), ("tìm em trai đi rồi hẵng diễn", "find your brother first, then act"),
            });
            // ---- Tầng trệt (tò mò, nghi ngờ)
            Pool("chat.s1", new[]
            {
                ("cái balo kia là của Julian thật à??", "is that julian's actual backpack??"), ("bàn tiệc kìa… vẫn còn đồ ăn??", "the dinner table… there's still food??"),
                ("ok cái ảnh gia tộc hơi ghê", "ok that family portrait is creepy"), ("nghe thấy tiếng gì không", "anyone else hear that"),
                ("check két sắt đi, có ghi năm kìa", "check the safe, there's a year on it"), ("Alex đừng đi một mình nữa", "alex stop going alone"),
                ("clip này lên trending chắc luôn", "this clip is going viral for sure"), ("mic bị rè hay là tiếng thở vậy", "is the mic crackling or is that breathing"),
            });
            // ---- Tầng 2 (sợ)
            Pool("chat.s2", new[]
            {
                ("tiếng chìa khóa kìa ALEX", "KEYS ALEX THE KEYS"), ("tắt đèn pin đi trời ơi", "turn off the flashlight omg"),
                ("ai đó gọi cảnh sát đi", "someone call the police"), ("tôi không dám xem nữa", "i can't watch anymore"),
                ("trốn vào tủ đi!!!", "get in the wardrobe!!!"), ("cái phòng trẻ con kia…", "that child's room…"),
                ("Alex, nín thở khi trốn", "alex hold your breath when hiding"), ("cái gì vừa đi qua cửa vậy", "what just walked past the door"),
            });
            // ---- Hầm (hoảng loạn)
            Pool("chat.s3", new[]
            {
                ("đừng xuống đó", "don't go down there"), ("xương trong tường… thật đấy à", "bones in the wall… is that real"),
                ("chat ơi nó im lặng quá", "chat it's way too quiet"), ("Julian… không…", "julian… no…"),
                ("đừng tin cái ông ngồi ghế", "don't trust the man in the chair"), ("đập vỡ cái cục đá đen đi", "smash the black stone"),
            });
            // ---- Tài khoản ma (không UID) — báo trước nguy hiểm
            Pool("chat.ghost.near", new[]
            {
                ("bà ấy ở sau cánh cửa", "she is behind the door"), ("đừng thở", "don't breathe"),
                ("bên trái", "left side"), ("bà ấy nghe thấy anh rồi", "she heard you"),
                ("đứng yên", "stand still"), ("chìa khóa đang lại gần", "the keys are coming closer"),
            });
            Pool("chat.ghost.hunt", new[]
            {
                ("CHẠY", "RUN"), ("TỦ ÁO. NGAY.", "WARDROBE. NOW."), ("bà ấy không dừng lại đâu", "she won't stop"),
                ("dưới gầm giường", "under the bed"), ("flash vào mặt bà ấy", "flash her face"),
            });
            Pool("chat.ghost.idle", new[]
            {
                ("chúng tôi có mười ba người", "there are thirteen of us"), ("anh tới muộn mười bốn ngày", "you're fourteen days late"),
                ("cậu ấy vẫn ở dưới hầm", "he is still in the cellar"), ("ngôi nhà đang chọn chủ mới", "the house is choosing a new master"),
                ("đừng đọc ngược", "don't read it backwards"), ("chúng tôi đang xem", "we are watching"),
            });
            Pool("chat.flash", new[] { ("flash đẹp đấy", "nice flash"), ("chụp hụt rồi", "missed"), ("pin còn không Alex", "how's your battery alex") });
            Pool("chat.flash_hit", new[] { ("TRÚNG RỒI", "GOT HER"), ("bà ta khựng lại kìa!!", "she froze!!"), ("chạy ngay đi", "run now") });
            Pool("chat.item", new[] { ("cái gì vậy?", "what is that?"), ("zoom vào đi", "zoom in"), ("ghi lại hết đi", "document everything"), ("cái này manh mối đó", "that's a clue") });
            Pool("chat.caught", new[] { ("ALEXXXX", "ALEXXXX"), ("không không không", "no no no"), ("stream chết rồi à??", "did the stream die??") });

            // ---- EVP máy ghi âm của Evelyn
            Pool("evp", new[]
            {
                ("…mười ba…", "…thirteen…"), ("…cửa hầm dưới bàn…", "…the hatch under the table…"),
                ("…bà ấy không xuống được…", "…she can't come down…"), ("…đừng nghe lão…", "…don't listen to him…"),
                ("…cháu lạnh quá…", "…I'm so cold…"), ("…chạy đi…", "…run…"), ("…Evelyn…", "…Evelyn…"),
            });

            // ---- Chú thích sơ đồ trinh thám
            Pool("mind.link", new[]
            {
                ("Mã két = năm khởi dựng 1921", "Safe code = founding year 1921"),
                ("Julian đã thử 1924 — sai", "Julian tried 1924 — wrong"),
                ("Lối xuống hầm nằm dưới sàn bếp", "The way down is beneath the kitchen floor"),
                ("Vòng tròn 5 cánh = 5 Mắt Trận", "Five-pointed circle = five Eyes"),
                ("Người đàn bà cầm chìa khóa = Mrs. Peake", "The woman with the keys = Mrs. Peake"),
                ("Annabelle là Đứa Trẻ", "Annabelle is the Child"),
                ("Vị khách bị chuốc độc là Kẻ Lữ Khách", "The poisoned guest is the Wayfarer"),
                ("Thomas là Người Thợ", "Thomas is the Builder"),
                ("Isabella là Quý Bà — bài chú thứ năm", "Isabella is the Lady — the fifth verse"),
                ("Lòng tham được thao túng qua Vademecum", "Greed spread through the Vademecum"),
                ("Lỗ khoan 2009 sau vách bếp", "The 2009 drill hole behind the kitchen wall"),
            });
            return list.ToArray();
        }

        public struct MindNode { public string id; public Vector2 pos; public MindNode(string i, float x, float y) { id = i; pos = new Vector2(x, y); } }
        public struct MindLink { public string from, to, key; public MindLink(string a, string b, int n) { from = a; to = b; key = "mind.link." + n; } }

        public static readonly MindNode[] MindNodes =
        {
            new MindNode("DOC_1921_BangDong", 0.12f, 0.82f), new MindNode("DOC_1996_DiThuLam", 0.38f, 0.86f), new MindNode("TAPE_2", 0.62f, 0.86f),
            new MindNode("TAPE_1", 0.86f, 0.78f), new MindNode("DOC_1924_BanThietKeHam", 0.9f, 0.5f), new MindNode("PHOTO_QuanSent", 0.66f, 0.6f),
            new MindNode("DOC_1924_SoKhoaHon", 0.42f, 0.52f), new MindNode("REL_Tho", 0.18f, 0.55f), new MindNode("REL_Moc", 0.14f, 0.12f),
            new MindNode("REL_Thuy", 0.3f, 0.14f), new MindNode("REL_Kim", 0.52f, 0.14f), new MindNode("REL_Hoa", 0.74f, 0.2f),
            new MindNode("DOC_1978_ThiepTanGia", 0.08f, 0.4f), new MindNode("DOC_2009_BienBan", 0.9f, 0.24f), new MindNode("TAPE_3", 0.92f, 0.06f),
        };

        public static readonly MindLink[] MindLinks =
        {
            new MindLink("DOC_1921_BangDong", "DOC_1996_DiThuLam", 1),
            new MindLink("DOC_1996_DiThuLam", "TAPE_2", 2),
            new MindLink("TAPE_1", "DOC_1924_BanThietKeHam", 3),
            new MindLink("PHOTO_QuanSent", "DOC_1924_SoKhoaHon", 4),
            new MindLink("DOC_1996_DiThuLam", "REL_Tho", 5),
            new MindLink("DOC_1924_SoKhoaHon", "REL_Moc", 6),
            new MindLink("DOC_1924_SoKhoaHon", "REL_Thuy", 7),
            new MindLink("DOC_1924_SoKhoaHon", "REL_Kim", 8),
            new MindLink("DOC_1924_SoKhoaHon", "REL_Hoa", 9),
            new MindLink("DOC_1978_ThiepTanGia", "DOC_1924_SoKhoaHon", 10),
            new MindLink("DOC_2009_BienBan", "TAPE_3", 11),
        };
    }
}
