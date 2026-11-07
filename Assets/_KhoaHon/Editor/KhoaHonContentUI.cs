namespace KhoaHon.EditorTools
{
    /// <summary>Chữ song ngữ cho giao diện chuẩn Steam: ô lưu, chương, màn tải, thành tựu, cài đặt hỗ trợ, credits.</summary>
    public static partial class KhoaHonContent
    {
        public static readonly (string key, string vi, string en)[] TextUI =
        {
            ("ui.load_game", "Tải trò chơi", "Load Game"),
            ("ui.extras", "Thêm", "Extras"),
            ("ui.choose_slot_new", "CHỌN Ô LƯU", "CHOOSE A SAVE SLOT"),
            ("ui.choose_slot_load", "TẢI TRÒ CHƠI", "LOAD GAME"),
            ("ui.slot", "Ô lưu {0}", "Slot {0}"),
            ("ui.slot_empty", "Trống", "Empty"),
            ("ui.loading", "Đang tải…", "Loading…"),
            ("ui.endings", "Kết thúc", "Endings"),
            ("ui.ending_locked", "???  (chưa mở khóa)", "???  (locked)"),
            ("ui.achievements", "Thành tựu  {0}/{1}", "Achievements  {0}/{1}"),
            ("ui.credits_more", "ÂM THANH\nTổng hợp thủ tục bằng code — không dùng mẫu có bản quyền\n\nMÔ HÌNH ĐẠO CỤ\nBlender 5.2 (dựng bằng script)\n\nCÔNG CỤ\nUnity 6 · URP · MCP for Unity\n\nGửi Julian.\nVà gửi bạn — người đã dám bước qua cánh cổng ấy.",
                "AUDIO\nProcedurally synthesized in code — no licensed samples\n\nPROP MODELS\nBlender 5.2 (scripted)\n\nTOOLS\nUnity 6 · URP · MCP for Unity\n\nFor Julian.\nAnd for you — who dared to walk through that gate."),

            ("stage.0", "Chương 0 — Cổng sương mù", "Chapter 0 — The Fog Gate"),
            ("stage.1", "Chương 1 — Tầng trệt", "Chapter 1 — Ground Floor"),
            ("stage.2", "Chương 2 — Lãnh địa Mrs. Peake", "Chapter 2 — Mrs. Peake's Domain"),
            ("stage.3", "Chương 3 — Mê đạo hầm ngầm", "Chapter 3 — The Cellar"),
            ("stage.4", "Chương 4 — Sảnh Tế", "Chapter 4 — The Ritual Hall"),
            ("stage.5", "Kết thúc", "Finale"),

            ("ui.volumetric_fog", "Sương thể tích", "Volumetric fog"),
            ("ui.h.access", "Hỗ trợ", "Accessibility"),
            ("ui.subtitle_size", "Cỡ phụ đề", "Subtitle size"),
            ("ui.size.normal", "Thường", "Normal"),
            ("ui.size.large", "Lớn", "Large"),
            ("ui.size.xlarge", "Rất lớn", "Extra large"),
            ("ui.camera_shake", "Rung camera", "Camera shake"),

            // Mẹo màn tải
            ("tip.1", "Mrs. Peake không xuống được tầng trệt. Cầu thang là ranh giới của bà ta.", "Mrs. Peake cannot come down to the ground floor. The stairs are her border."),
            ("tip.2", "Trốn trong tủ hoặc dưới gầm giường, giữ Space để nín thở khi bà ta lại gần.", "Hide in a wardrobe or under a bed — hold Space to hold your breath when she comes close."),
            ("tip.3", "Tiếng chìa khóa leng keng cho biết bà ta ở bên nào. Hãy đeo tai nghe.", "The jingle of keys tells you which side she's on. Wear headphones."),
            ("tip.4", "Đèn pin giữ cho bạn tỉnh táo — nhưng cũng cho bà ta thấy bạn.", "Your flashlight keeps you sane — and shows her where you are."),
            ("tip.5", "Mỗi tấm bia trong phòng thờ khắc tên một Mắt Trận. Đọc kỹ các kỷ vật.", "Each plaque in the shrine names an Eye of the circle. Read the relics carefully."),
            ("tip.6", "Đồ vật đổ vỡ gây tiếng động. Đi chậm qua hành lang tầng 2.", "Things that fall make noise. Walk slowly through the upstairs halls."),
            ("tip.7", "Flash máy ảnh hoặc điện thoại có thể làm bà ta khựng lại vài giây.", "A camera or phone flash can stun her for a few seconds."),
            ("tip.8", "Ở trong bóng tối quá lâu, bạn sẽ bắt đầu thấy những thứ không có thật.", "Stay in the dark too long and you'll start seeing things that aren't there."),
            ("tip.9", "Năm xây xong chưa chắc là năm cơ nghiệp bắt đầu.", "The year it was finished isn't always the year it began."),

            // Thành tựu
            ("ach.unlocked", "Thành tựu: {0}", "Achievement: {0}"),
            ("ach.end_release.title", "Bình minh", "Daybreak"), ("ach.end_release.desc", "Giải thoát mười ba linh hồn.", "Free the thirteen souls."),
            ("ach.end_master.title", "Tân Chủ Nhân", "The New Master"), ("ach.end_master.desc", "Nhận lấy Khối Cổ Vật.", "Claim the Obsidian Relic."),
            ("ach.end_coward.title", "Kẻ Hèn Nhát", "The Coward"), ("ach.end_coward.desc", "Bỏ chạy qua cửa sổ sảnh.", "Flee through the hall window."),
            ("ach.both_characters.title", "Hai góc nhìn", "Two Perspectives"), ("ach.both_characters.desc", "Hoàn thành game với cả Evelyn và Alex.", "Finish the game as both Evelyn and Alex."),
            ("ach.no_death.title", "Không một vết xước", "Untouched"), ("ach.no_death.desc", "Đạt kết thúc chính mà không chết lần nào.", "Reach a main ending without dying."),
            ("ach.safe_cracked.title", "Năm khai sinh", "Year of Founding"), ("ach.safe_cracked.desc", "Mở két sắt của Lord Blackwood.", "Open Lord Blackwood's safe."),
            ("ach.found_julian.title", "Mười bốn ngày", "Fourteen Days"), ("ach.found_julian.desc", "Tìm thấy Julian.", "Find Julian."),
            ("ach.first_flash.title", "Nụ cười trước ống kính", "Say Cheese"), ("ach.first_flash.desc", "Làm choáng Mrs. Peake bằng đèn flash.", "Stun Mrs. Peake with a flash."),
            ("ach.all_relics.title", "Năm Mắt Trận", "Five Eyes"), ("ach.all_relics.desc", "Thu thập đủ 5 kỷ vật.", "Collect all five relics."),
            ("ach.cellar_open.title", "Xích đứt", "Broken Chains"), ("ach.cellar_open.desc", "Mở nắp hầm dưới nhà bếp.", "Open the cellar hatch."),
            ("ach.archivist.title", "Người lưu trữ", "Archivist"), ("ach.archivist.desc", "Ghi đầy sổ tay điều tra.", "Fill the case notebook."),
        };
    }
}
