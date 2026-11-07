using KhoaHon.Core;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Toàn bộ nội dung chữ song ngữ (Việt / Anh) của "Blackwood Manor: The Soul-Lock Curse"
    /// theo kịch bản Kich_Ban_Game.docx. KhoaHonDataBuilder đọc từ đây để ghi vào ScriptableObject.
    /// Id asset giữ nguyên tên cũ để không vỡ file lưu và converter.
    /// </summary>
    public static class KhoaHonContent
    {
        // ------------------------------------------------------------------ Bảng chuỗi UI
        public static readonly (string key, string vi, string en)[] Text =
        {
            // Prompt tương tác
            ("prompt.open_door", "[E] Mở cửa", "[E] Open"),
            ("prompt.close_door", "[E] Đóng cửa", "[E] Close"),
            ("prompt.locked", "Cửa bị khóa", "Locked"),
            ("prompt.unlock_with", "[E] Mở khóa bằng {0}", "[E] Unlock with {0}"),
            ("prompt.pickup", "[E] Nhặt {0}", "[E] Take {0}"),
            ("prompt.read", "[E] Đọc: {0}", "[E] Read: {0}"),
            ("prompt.inspect", "[E] Xem", "[E] Examine"),
            ("prompt.main_door_locked", "Cửa chính đã sập khóa. Không mở được.", "The front door has slammed shut. It won't budge."),
            ("prompt.stair_door_locked", "Cửa lên tầng hai bị khóa", "The door to the upper floor is locked"),
            ("prompt.chains", "Nắp hầm bị khóa bằng 5 sợi xích", "The cellar hatch is bound by five chains"),
            ("prompt.door_stuck", "Cửa kẹt cứng, không mở được", "The door is jammed shut"),
            ("prompt.hide_wardrobe", "[E] Trốn vào tủ", "[E] Hide in the wardrobe"),
            ("prompt.hide_bed", "[E] Chui xuống gầm giường", "[E] Hide under the bed"),
            ("prompt.hide_exit", "[E] Ra ngoài", "[E] Come out"),
            ("prompt.hold_breath", "Giữ [Space] để nín thở!", "Hold [Space] to hold your breath!"),
            ("prompt.locked_outside", "Cửa bị khóa từ bên ngoài", "Locked from the other side"),
            ("prompt.altar", "Bàn thờ gia tộc — 13 bia tưởng niệm, 5 bệ nến", "Family shrine — 13 memorial plaques, 5 candle stands"),
            ("prompt.body", "[E] Xem", "[E] Examine"),

            // Mrs. Peake
            ("sub.hk_run", "Chạy đi…", "Run…"),
            ("sub.hk_comb", "Chạy đi… ta không kiềm chế được…", "Run… I can't hold myself back…"),
            ("sub.hk_lullaby", "♪ Ngủ đi con, nến đã tàn… cửa đã cài… Bà Peake giữ chìa khóa, chẳng ai ra ngoài… ♪",
                "♪ Hush now, child, the candle's low… the doors are barred… Mrs. Peake keeps the keys, and no one leaves at all… ♪"),

            // Thông điệp
            ("msg.chains", "Năm sợi xích lớn nối ngầm vào tường. Không có ổ khóa nào cả.", "Five heavy chains run into the walls. There is no lock anywhere."),
            ("msg.rubble", "Đá sập kín một đường hầm khoan hẹp. Không đi tiếp được.", "Collapsed stone blocks a narrow drilled tunnel. No way through."),

            // Vật phẩm
            ("key.upstairs", "chìa khóa cửa cầu thang", "the staircase key"),
            ("item.backpack", "balo của Julian", "Julian's backpack"),
            ("item.battery", "pin", "batteries"),
            ("item.film", "cuộn phim", "a film roll"),
            ("item.safe_placeholder", "đồ trong két sắt", "the safe's contents"),
            ("item.tape", "cuộn băng cassette", "a cassette tape"),
            ("item.newspaper", "mẩu báo cũ", "an old newspaper"),
            ("item.relic_moc", "búp bê vải", "the rag doll"),
            ("item.relic_thuy", "lá thư cầu cứu", "the unsent letter"),
            ("item.relic_tho", "lược sừng cổ", "the horn comb"),
            ("item.relic_kim", "búa đóng đinh", "the bloody hammer"),
            ("item.relic_hoa", "đôi giày lụa thêu", "the embroidered silk slippers"),
            ("item.generic", "vật phẩm", "item"),
            ("item.recorder", "Máy ghi âm", "Cassette Recorder"),
            ("item.film_camera", "Máy ảnh film", "Film Camera"),
            ("item.crowbar", "Xà beng", "Crowbar"),
            ("item.phone", "Điện thoại", "Smartphone"),
            ("item.night_vision", "Kính nhìn đêm", "Night Vision"),

            // Toast / gợi ý
            ("toast.got_item", "Đã nhặt: {0}", "Picked up: {0}"),
            ("toast.notebook_added", "Đã ghi vào Sổ tay: {0}", "Added to notebook: {0}"),
            ("toast.saved", "Đã lưu tiến trình", "Progress saved"),
            ("toast.no_save", "Chưa có bản lưu", "No saved game"),
            ("hint.backpack", "Nghe cuộn băng bằng máy ghi âm hoặc điện thoại (Tab > Băng ghi âm)", "Play the tape on the recorder or phone (Tab > Tapes)"),
            ("sub.front_door_slam", "(Cửa chính sập lại sau lưng. Then cài rơi xuống.)", "(The front door slams behind you. The bolt drops.)"),
            ("year.present", "Hiện tại", "Present"),

            // Sổ tay
            ("ui.close", "Đóng [E]", "Close [E]"),
            ("notebook.title", "Sổ tay điều tra", "Case Notebook"),
            ("notebook.hint", "Tab / J: đóng  ·  Q / E: đổi tab", "Tab / J: close  ·  Q / E: switch tab"),
            ("notebook.tab.documents", "Tài liệu", "Documents"),
            ("notebook.tab.tapes", "Băng ghi âm", "Tapes"),
            ("notebook.tab.photos", "Ảnh", "Photos"),
            ("notebook.tab.talismans", "Vademecum", "Vademecum"),
            ("notebook.tab.relics", "Kỷ vật", "Relics"),
            ("notebook.empty", "Chưa có gì.", "Nothing yet."),
            ("notebook.new", "mới", "new"),
            ("notebook.tape_hint", "[Space] Phát băng này (vừa đi vừa nghe được).", "[Space] Play this tape (you can keep walking)."),

            // Intro (mặc định = Evelyn; mỗi nhân vật có intro riêng bên dưới)
            ("intro.1", "Julian Vance mất tích ở dinh thự Blackwood đã mười bốn ngày.", "Julian Vance has been missing inside Blackwood Manor for fourteen days."),
            ("intro.2", "Đêm cuối cùng, cậu ấy gửi về một bức ảnh chụp sàn nhà bếp. Cảnh sát nói đó chỉ là vết bẩn.", "On his last night he sent a photo of the kitchen floor. The police called it a stain."),
            ("intro.3", "Tôi không tin.", "I don't believe them."),

            // Nhân vật
            ("char.choose", "CHỌN NHÂN VẬT", "CHOOSE YOUR INVESTIGATOR"),
            ("char.evelyn.name", "Evelyn Reid", "Evelyn Reid"),
            ("char.evelyn.role", "Nhà báo điều tra · The Ashford Herald", "Investigative Journalist · The Ashford Herald"),
            ("char.evelyn.motive", "Gần một thế kỷ án mạng bị niêm phong. Cộng sự của cô, Julian Vance, đã mất liên lạc ngay sau khi gửi về một bức ảnh mật mã.",
                "Nearly a century of sealed murder files. Her partner, Julian Vance, went silent right after sending back a coded photograph."),
            ("char.evelyn.tools", "Sổ tay trinh thám tự xâu chuỗi manh mối · Máy ghi âm bắt tiếng EVP để định hướng kẻ săn đuổi · Máy ảnh film có đèn flash",
                "A detective notebook that links the evidence · A cassette recorder that catches EVP and tracks the hunter · A film camera with flash"),
            ("char.alex.name", "Alex Vance", "Alex Vance"),
            ("char.alex.role", "Streamer tâm linh · kênh VanceAfterDark", "Paranormal Streamer · VanceAfterDark"),
            ("char.alex.motive", "Kênh livestream đang lụi tàn. Em trai anh, kỹ thuật viên Julian, biến mất trong dinh thự 14 ngày trước. Đêm nay anh lên sóng, và đi tìm nó.",
                "The channel is dying. His little brother Julian, a sound tech, vanished in the manor 14 days ago. Tonight Alex goes live — and goes looking for him."),
            ("char.alex.tools", "Điện thoại livestream với khung chat trực tiếp · Kính nhìn đêm soi dấu vết ẩn · Đèn flash điện thoại",
                "A livestreaming phone with live chat · Night vision that reveals hidden traces · Phone flash"),
            ("char.start", "Bắt đầu", "Begin"),
            ("intro.evelyn.1", "Hồ sơ Blackwood: 1924, 1953, 1978, 1996, 2009. Mười ba năm một lần, có người chết trong ngôi nhà này.", "The Blackwood files: 1924, 1953, 1978, 1996, 2009. Every generation, someone dies in that house."),
            ("intro.evelyn.2", "Mười bốn ngày trước, Julian gửi cho tôi một bức ảnh chụp sàn nhà bếp. Rồi im lặng.", "Fourteen days ago, Julian sent me a photograph of the kitchen floor. Then nothing."),
            ("intro.evelyn.3", "Tòa soạn bảo tôi bỏ đi. Tôi lái xe đến Blackwood lúc nửa đêm.", "The paper told me to drop it. I drove to Blackwood at midnight."),
            ("intro.alex.1", "Ba trăm người xem. Tuần trước còn ba nghìn.", "Three hundred viewers. Last week it was three thousand."),
            ("intro.alex.2", "Julian bảo nó tìm được thứ sẽ cứu cái kênh này. Đó là tin nhắn cuối cùng của nó.", "Julian said he'd found something that would save the channel. That was his last message."),
            ("intro.alex.3", "Đêm nay tôi lên sóng ở dinh thự Blackwood. Và tôi sẽ đưa em tôi về.", "Tonight I go live from Blackwood Manor. And I'm bringing my brother home."),

            // Menu chính
            ("ui.title_top", "LỜI NGUYỀN KHÓA HỒN", "THE SOUL-LOCK CURSE"),
            ("ui.title_main", "BLACKWOOD MANOR", "BLACKWOOD MANOR"),
            ("ui.continue", "Tiếp tục", "Continue"),
            ("ui.new_game", "Trò chơi mới", "New Game"),
            ("ui.settings", "Cài đặt", "Settings"),
            ("ui.credits", "Thông tin", "Credits"),
            ("ui.quit", "Thoát", "Quit"),
            ("ui.version", "Blackwood Manor · phiên bản 0.5", "Blackwood Manor · build 0.5"),
            ("ui.menu_hint", "Nên đeo tai nghe và chơi trong bóng tối.", "Best played with headphones, in the dark."),
            ("ui.confirm_new", "Bắt đầu lại từ đầu? Tiến trình đã lưu sẽ bị xóa.", "Start over? Your saved progress will be lost."),
            ("ui.confirm_quit", "Thoát game?", "Quit the game?"),
            ("ui.confirm_menu", "Về menu chính? Tiến trình từ điểm lưu gần nhất sẽ mất.", "Return to the main menu? Progress since the last save will be lost."),
            ("ui.confirm_reload", "Tải lại điểm lưu gần nhất?", "Reload the last checkpoint?"),
            ("ui.yes", "Đồng ý", "Yes"),
            ("ui.no", "Hủy", "Cancel"),
            ("ui.confirm", "Xác nhận", "Confirm"),
            ("ui.back", "Quay lại", "Back"),
            ("ui.apply", "Áp dụng", "Apply"),
            ("ui.defaults", "Mặc định", "Defaults"),
            ("ui.applied", "Đã áp dụng cài đặt", "Settings applied"),
            ("ui.paused", "TẠM DỪNG", "PAUSED"),
            ("ui.resume", "Tiếp tục", "Resume"),
            ("ui.reload_checkpoint", "Tải lại điểm lưu", "Reload checkpoint"),
            ("ui.main_menu", "Về menu chính", "Main menu"),
            ("ui.pause_tip", "Bà ta không ngủ. Bà ta chỉ chờ.", "She doesn't sleep. She waits."),
            ("ui.warning_title", "CẢNH BÁO", "WARNING"),
            ("ui.warning_body", "Trò chơi chứa hình ảnh máu me, xác chết, âm thanh đột ngột và ánh sáng nhấp nháy.\nKhông phù hợp với người dưới 18 tuổi, người có bệnh tim hoặc dễ bị kích động.\n\nNhấn phím bất kỳ để tiếp tục.",
                "This game contains graphic violence, gore, sudden loud sounds and flashing lights.\nNot suitable for players under 18, or anyone with a heart condition or photosensitivity.\n\nPress any key to continue."),
            ("ui.calib_title", "ĐIỀU CHỈNH ĐỘ SÁNG", "BRIGHTNESS CALIBRATION"),
            ("ui.calib_body", "Kéo thanh trượt cho tới khi hình bên trái thấy rõ, hình giữa thấy mờ\nvà hình bên phải GẦN NHƯ không nhìn thấy.",
                "Adjust until the left symbol is clearly visible, the middle one faint,\nand the right one BARELY visible."),
            ("ui.credits_body", "BLACKWOOD MANOR: THE SOUL-LOCK CURSE\n\nKịch bản & thiết kế: nhóm dự án refactored-adventure\nLập trình, dựng cảnh, âm thanh tổng hợp: Claude (Anthropic) cùng nhóm\nTài nguyên: Flooded Grounds, Abandoned Asylum, Vintage Living Room, civilian_girl, Mixamo\nPhông chữ: Be Vietnam Pro, Noto Serif (SIL OFL)\n\nCảm ơn bạn đã dám bước vào dinh thự Blackwood.",
                "BLACKWOOD MANOR: THE SOUL-LOCK CURSE\n\nStory & design: the refactored-adventure team\nProgramming, level building, synthesized audio: Claude (Anthropic) with the team\nAssets: Flooded Grounds, Abandoned Asylum, Vintage Living Room, civilian_girl, Mixamo\nFonts: Be Vietnam Pro, Noto Serif (SIL OFL)\n\nThank you for daring to enter Blackwood Manor."),

            // Cài đặt
            ("ui.tab.graphics", "Hình ảnh", "Graphics"),
            ("ui.tab.audio", "Âm thanh", "Audio"),
            ("ui.tab.controls", "Điều khiển", "Controls"),
            ("ui.tab.display", "Hiển thị", "Display"),
            ("ui.h.screen", "Màn hình", "Screen"),
            ("ui.h.quality", "Chất lượng", "Quality"),
            ("ui.h.volume", "Âm lượng", "Volume"),
            ("ui.h.subtitles", "Phụ đề", "Subtitles"),
            ("ui.h.mouse", "Chuột", "Mouse"),
            ("ui.h.keys", "Phím điều khiển", "Key bindings"),
            ("ui.h.brightness", "Độ sáng", "Brightness"),
            ("ui.h.hud", "Giao diện", "Interface"),
            ("ui.screen_mode", "Chế độ hiển thị", "Display mode"),
            ("ui.fullscreen", "Toàn màn hình", "Fullscreen"),
            ("ui.borderless", "Cửa sổ không viền", "Borderless window"),
            ("ui.windowed", "Cửa sổ", "Windowed"),
            ("ui.resolution", "Độ phân giải", "Resolution"),
            ("ui.vsync", "Đồng bộ dọc (VSync)", "VSync"),
            ("ui.fps_cap", "Giới hạn FPS", "Frame rate limit"),
            ("ui.unlimited", "Không giới hạn", "Unlimited"),
            ("ui.quality", "Chất lượng đồ họa", "Graphics quality"),
            ("ui.fov", "Trường nhìn (FOV)", "Field of view"),
            ("ui.q.low", "Thấp", "Low"),
            ("ui.q.medium", "Trung bình", "Medium"),
            ("ui.q.high", "Cao", "High"),
            ("ui.q.ultra", "Siêu cao", "Ultra"),
            ("ui.motion_blur", "Làm mờ chuyển động", "Motion blur"),
            ("ui.film_grain", "Nhiễu hạt phim", "Film grain"),
            ("ui.vol_master", "Tổng", "Master"),
            ("ui.vol_music", "Nhạc", "Music"),
            ("ui.vol_sfx", "Hiệu ứng", "Effects"),
            ("ui.vol_amb", "Môi trường", "Ambience"),
            ("ui.vol_voice", "Giọng nói", "Voice"),
            ("ui.subtitles", "Hiện phụ đề", "Show subtitles"),
            ("ui.headphones", "Âm thanh 3D được thiết kế cho tai nghe: tiếng chìa khóa cho biết bà ta ở bên nào.", "3D audio is designed for headphones: the jingle of keys tells you which side she's on."),
            ("ui.sensitivity", "Độ nhạy chuột", "Mouse sensitivity"),
            ("ui.brightness", "Độ sáng", "Brightness"),
            ("ui.show_fps", "Hiện FPS", "Show FPS"),
            ("ui.language", "Ngôn ngữ", "Language"),
            ("ui.on", "Bật", "On"),
            ("ui.off", "Tắt", "Off"),
            ("ui.key.move", "Di chuyển", "Move"),
            ("ui.key.run", "Chạy", "Sprint"),
            ("ui.key.interact", "Tương tác", "Interact"),
            ("ui.key.notebook", "Sổ tay", "Notebook"),
            ("ui.key.hotbar", "Đổi vật phẩm", "Switch item"),
            ("ui.key.use", "Dùng vật phẩm", "Use item"),
            ("ui.key.breath", "Nín thở (khi trốn)", "Hold breath (hiding)"),
            ("ui.key.pause", "Tạm dừng", "Pause"),
            ("ui.keyname.hotbar", "1 – 4 · cuộn chuột", "1 – 4 · mouse wheel"),
            ("ui.keyname.lmb", "Chuột trái", "Left mouse"),
            ("ui.keyname.hold_space", "Giữ Space", "Hold Space"),
            ("ui.you_died", "BẠN ĐÃ CHẾT", "YOU DIED"),
            ("hud.battery", "PIN", "BATTERY"),
            ("hud.film", "PHIM", "FILM"),

            // Két sắt
            ("prompt.safe", "[E] Xoay mã két sắt", "[E] Try the safe's combination"),
            ("prompt.safe_take", "[E] Lấy đồ trong két", "[E] Take what's inside"),
            ("safe.title", "KÉT SẮT BLACKWOOD", "BLACKWOOD SAFE"),
            ("safe.hint", "A/D chọn vòng · W/S hoặc cuộn chuột đổi số · gõ số trực tiếp · Enter thử · Esc đóng",
                "A/D select dial · W/S or mouse wheel change digit · type digits · Enter try · Esc close"),
            ("safe.wrong", "Chốt không nhúc nhích.", "The bolt doesn't move."),
            ("safe.wrong_hint", "Không phải năm xây xong… mà là năm cơ nghiệp này bắt đầu.", "Not the year it was finished… the year this fortune began."),
            ("safe.opened", "(Chốt thép bật ra. Cửa két nặng nề hé mở.)", "(The steel bolt snaps back. The heavy door swings open.)"),
            // Bàn thờ & bệ nến
            ("prompt.stand", "[E] Bệ nến — bia khắc: {0}", "[E] Candle stand — plaque: {0}"),
            ("prompt.stand_lit", "Ngọn nến đã cháy — {0}", "The candle burns — {0}"),
            ("plaque.moc", "Annabelle Blackwood · 1916–1924", "Annabelle Blackwood · 1916–1924"),
            ("plaque.thuy", "Vị khách không tên · 1924", "The Nameless Guest · 1924"),
            ("plaque.tho", "Eleanor Peake · 1879–1924", "Eleanor Peake · 1879–1924"),
            ("plaque.kim", "Thomas Hale · 1881–1924", "Thomas Hale · 1881–1924"),
            ("plaque.hoa", "Isabella Blackwood · 1895–1924", "Isabella Blackwood · 1895–1924"),
            ("altar.no_pages", "Bệ nến dưới bia \"{0}\" còn trống. Cần một trang Vademecum.", "The stand beneath \"{0}\" is empty. It needs a page of the Vademecum."),
            ("altar.choose", "Đặt trang nào lên bệ của\n{0}?", "Which page belongs to\n{0}?"),
            ("altar.hint", "Mỗi tấm bia là một Mắt Trận. Đọc lại kỷ vật trong Sổ tay nếu chưa chắc. · Esc để hủy",
                "Each plaque names an Eye of the circle. Re-read the relics in your notebook if unsure. · Esc to cancel"),
            ("altar.correct", "(Ngọn nến tự bùng cháy. {0}/5)", "(The candle flares to life by itself. {0}/5)"),
            ("altar.wrong", "(Ngọn nến phụt tắt. Trang giấy nóng rực trên tay. Có thứ gì đó đã nghe thấy.)", "(The flame gutters out. The page burns hot in your hand. Something heard that.)"),
            ("altar.rumble", "(Cả dinh thự rung chuyển…)", "(The whole manor shudders…)"),
            ("altar.opened", "(Dưới nhà bếp vọng lên tiếng xích sắt đứt tung. Nắp hầm đã mở.)", "(From the kitchen below comes the scream of snapping chains. The cellar is open.)"),
            ("msg.altar", "Mười ba tấm bia tưởng niệm. Năm tấm mạ vàng, dưới mỗi tấm là một bệ nến trống.", "Thirteen memorial plaques. Five are gilded, each above an empty candle stand."),
            // Băng
            ("tape.playing", "Đang phát: {0}", "Playing: {0}"),
            ("tape.speaker", "Julian", "Julian"),
            // Cửa sổ sảnh
            ("prompt.window_pry", "[E] Nạy cửa sổ bằng xà beng", "[E] Pry the window open with the crowbar"),
            ("prompt.window_nailed", "[E] Cửa sổ bị đóng đinh", "[E] The window is nailed shut"),
            ("msg.window_nailed", "Ván gỗ đóng đinh chặt. Cần thứ gì đó để nạy.", "The boards are nailed tight. You'd need something to pry them."),
            ("ui.confirm_flee", "Nạy cửa sổ và bỏ chạy?\nJulian vẫn còn ở đâu đó trong ngôi nhà này.", "Pry the window and run?\nJulian is still somewhere in this house."),

            // Hội thoại Lord Blackwood (Phase F dùng)
            ("lord.1", "À… vị khách thứ mười bốn. Mời ngồi. Đừng để ý những bộ xương — họ là người nhà cả thôi.", "Ah… the fourteenth guest. Do sit. Pay no mind to the bones — they're family, after all."),
            ("lord.2", "Cậu Julian của cô đã đi xa hơn bất kỳ ai. Nhưng cậu ấy từ chối món quà của ta. Thật phí phạm.", "Your Julian came further than anyone. But he refused my gift. Such a waste."),
            ("lord.3", "Nhỏ một giọt máu lên Cổ Vật, đọc ngược bài chú thứ năm. Mười ba linh hồn sẽ quỳ dưới chân cô. Mãi mãi.", "One drop of blood upon the Relic. Read the fifth verse backwards. Thirteen souls will kneel at your feet. Forever."),
            ("lord.4", "Hay cô định đập vỡ nó? Ngôi nhà sẽ sập. Cô sẽ chẳng còn gì — kể cả cậu ta.", "Or will you break it? The house will fall. You'll have nothing — not even him."),
            ("lord.choice.burn", "Ném kỷ vật vào vạc, đập nát Cổ Vật", "Cast the relics into the cauldron and shatter the Relic"),
            ("lord.choice.blood", "Nhỏ máu lên Cổ Vật, đọc ngược bài chú", "Bleed upon the Relic and read the verse backwards"),
            ("julian.carving", "\"Lord Blackwood không bao giờ chết. Lão sống bằng chính lòng tham của kẻ kế tục. Đừng để lão gạt…\"",
                "\"Lord Blackwood never dies. He lives on the greed of whoever comes next. Don't let him fool you…\""),

            // Kết thúc
            ("end.1.title", "KẾT THÚC I — GIẢI THOÁT", "ENDING I — RELEASE"),
            ("end.1.body", "Mười ba sợi xích linh hồn đứt tung. Lord Blackwood tan thành tro bụi. Mrs. Peake mỉm cười, lần đầu tiên sau một trăm năm. Julian ôm bạn một lần cuối rồi tan vào ánh sáng.\n\nDinh thự sụp đổ sau lưng. Bạn chạy ra đúng lúc bình minh. Trong túi áo vẫn còn chiếc còi nhỏ của Julian.",
                "Thirteen soul-chains snap at once. Lord Blackwood crumbles to ash. Mrs. Peake smiles — for the first time in a hundred years. Julian holds you one last time, then fades into the light.\n\nThe manor collapses behind you. You stumble out as the sun rises. In your pocket, Julian's little whistle."),
            ("end.2.title", "KẾT THÚC II — TÂN CHỦ NHÂN", "ENDING II — THE NEW MASTER"),
            ("end.2.body", "Mười ba oan hồn quỳ phục dưới chân bạn. Lord Blackwood bị hút vào Cổ Vật làm nô lệ. Julian bị trói trong đó, vĩnh viễn.\n\nVài tháng sau, một nhóm bạn trẻ dừng xe trước cổng. Trên cửa sổ tầng hai, một bóng người mỉm cười.\n\n\"Vào đi… nhà đang đợi các bạn.\"",
                "Thirteen souls kneel before you. Lord Blackwood is dragged into the Relic as your slave. Julian is bound inside it — forever.\n\nMonths later, a carload of friends pulls up at the gate. In a second-floor window, a figure smiles.\n\n\"Come in… the house has been waiting for you.\""),
            ("end.3.title", "KẾT THÚC BÍ MẬT — KẺ HÈN NHÁT", "SECRET ENDING — THE COWARD"),
            ("end.3.body", "Hai giờ sáng. Đường lộ trống trơn. Bạn đã thoát.\n\nNhưng mỗi lần liếc gương chiếu hậu, luôn có một người đàn bà mặc đồ quản gia màu đen ngồi bất động ở ghế sau.\n\nBệnh viện tâm thần Ashford, phòng 13. Bệnh nhân cào lên tường cùng một hình thù, đêm này qua đêm khác.",
                "Two in the morning. The road is empty. You got away.\n\nBut every time you glance in the rear-view mirror, a woman in a black housekeeper's dress sits perfectly still in the back seat.\n\nAshford Asylum, Room 13. The patient scratches the same shape into the wall, night after night."),
            ("end.stats", "Thời gian chơi: {0}  ·  Số lần chết: {1}  ·  Tài liệu: {2}", "Play time: {0}  ·  Deaths: {1}  ·  Documents: {2}"),
            ("end.continue", "Nhấn phím bất kỳ", "Press any key"),
        };

        // ------------------------------------------------------------------ Tài liệu (id giữ nguyên)
        public class DocDef
        {
            public string id; public ChronoYear year; public bool serif = true;
            public string titleVi, titleEn, bodyVi, bodyEn;
        }

        public static readonly DocDef[] Documents =
        {
            new DocDef { id = "DOC_1921_BangDong", year = ChronoYear.Y1921,
                titleVi = "Ảnh gia tộc Blackwood", titleEn = "Blackwood Family Portrait",
                bodyVi = "Ảnh chụp toàn gia tộc trước tiền sảnh còn dựng giàn giáo.\nDòng chữ mạ vàng dưới khung:\n\n\"BLACKWOOD MANOR — KHỞI DỰNG NĂM 1921\nLord Arthur Blackwood cùng gia quyến.\"",
                bodyEn = "The whole family posed before an unfinished façade, scaffolding still up.\nGilt lettering beneath the frame:\n\n\"BLACKWOOD MANOR — FOUNDED 1921\nLord Arthur Blackwood and household.\"" },
            new DocDef { id = "DOC_1924_SoKhoaHon", year = ChronoYear.Y1924,
                titleVi = "The Blackwood Vademecum — Tập I", titleEn = "The Blackwood Vademecum — Volume I",
                bodyVi = "Alchemical Soul-Lock. Lấy năm người thân cận làm năm Mắt Trận: Người Giữ Chìa, Người Thợ, Quý Bà, Đứa Trẻ, Kẻ Lữ Khách. Lấy tám thợ xây chôn trong vách làm mạch dẫn khí uế.\n\nMười ba linh hồn khóa vào Khối Cổ Vật. Kẻ giữ Cổ Vật hưởng phú quý và tuổi thọ không cùng.\n\nNăm bệ nến là năm cửa của trận. Thắp đúng năm trang, xích dưới bếp sẽ tự đứt.\n\n(Kèm sơ đồ năm bệ nến.)",
                bodyEn = "The Alchemical Soul-Lock. Take five intimates as the five Eyes of the circle: the Keeper, the Builder, the Lady, the Child, the Wayfarer. Wall up eight masons to carry the foul air.\n\nThirteen souls bound into the Obsidian Relic. Whoever holds the Relic shall know wealth and years without end.\n\nThe five candle stands are the five gates. Light the five true pages and the chains beneath the kitchen will break of themselves.\n\n(A diagram of five candle stands is attached.)" },
            new DocDef { id = "DOC_1924_BanThietKeHam", year = ChronoYear.Y1924,
                titleVi = "Bản thiết kế hầm ngầm", titleEn = "Cellar Blueprint",
                bodyVi = "Cầu thang đá — sâu mười lăm mét. Xà lim. Sảnh tế bát giác. Vạc đặt chính giữa.\n\n(Chữ ký Thợ Cả Thomas bị gạch xóa bằng mực đỏ.)",
                bodyEn = "Stone stair — fifty feet down. Holding cell. Octagonal ritual hall. Cauldron at the centre.\n\n(Master Builder Thomas's signature has been struck through in red ink.)" },
            new DocDef { id = "DOC_1953_MauBao", year = ChronoYear.Y1953,
                titleVi = "Mẩu báo cũ — The Ashford Herald", titleEn = "Old Clipping — The Ashford Herald",
                bodyVi = "THẢM KỊCH Ở DINH THỰ BLACKWOOD: toàn bộ gia tộc Blackwood được phát hiện tử vong trong phòng ngủ chính. Lord Arthur, khi ấy đã 79 tuổi, được tìm thấy ngồi trên ghế, mắt mở trừng. Nhà chức trách chưa xác định nguyên nhân.",
                bodyEn = "TRAGEDY AT BLACKWOOD MANOR: the entire Blackwood family has been found dead in the master bedroom. Lord Arthur, aged 79, was discovered seated upright, eyes wide open. Authorities have not determined a cause." },
            new DocDef { id = "DOC_1978_ThiepTanGia", year = ChronoYear.Y1978,
                titleVi = "Thiệp tân gia nhà Miller", titleEn = "The Millers' Housewarming Invitation",
                bodyVi = "Gia đình Miller trân trọng kính mời quý khách đến dự tiệc tân gia tại dinh thự Blackwood.\n\nMặt sau, nét chữ run rẩy:\n\"Nửa cuốn sách nói ta sẽ có tất cả. Chỉ cần một bữa tối.\"",
                bodyEn = "The Miller family requests the pleasure of your company at a housewarming dinner at Blackwood Manor.\n\nOn the back, in a shaking hand:\n\"Half a book tells me I can have everything. All it takes is one dinner.\"" },
            new DocDef { id = "DOC_1996_DiThuLam", year = ChronoYear.Y1996,
                titleVi = "Di thư Harrison Sterling", titleEn = "Harrison Sterling's Last Letter",
                bodyVi = "Mọi cơ quan của ngôi nhà đều nằm ở phòng làm việc và phòng thờ gia tộc. Mật mã két sắt nằm ở năm khai sinh cơ nghiệp này.\n\nTa đã mở được két, nhưng không qua được bàn thờ. Người đàn bà cầm chùm chìa khóa đêm nào cũng đứng ở chân giường ta.\n\nTa không chịu nổi nữa.",
                bodyEn = "Every mechanism in this house answers to the study and the family shrine. The safe's combination is the year this fortune was born.\n\nI opened the safe. I could not pass the shrine. The woman with the ring of keys stands at the foot of my bed every night.\n\nI cannot bear it any longer." },
            new DocDef { id = "DOC_2009_BienBan", year = ChronoYear.Y2009,
                titleVi = "Biên bản niêm phong", titleEn = "Police Seal Notice",
                bodyVi = "SỞ CẢNH SÁT HẠT ASHFORD — BIÊN BẢN NIÊM PHONG.\nSáu công nhân đội cải tạo mất tích sau khi khoan xuyên vách tầng hầm. Hiện trường niêm phong vĩnh viễn. Cấm mọi hành vi xâm nhập.",
                bodyEn = "ASHFORD COUNTY POLICE — NOTICE OF SEAL.\nSix members of a renovation crew went missing after drilling through a cellar wall. The premises are sealed permanently. Trespassers will be prosecuted." },
            new DocDef { id = "DOC_Now_TheQuan", year = ChronoYear.Present, serif = false,
                titleVi = "Thẻ của Julian Vance", titleEn = "Julian Vance's ID Badge",
                bodyVi = "JULIAN VANCE — Kỹ thuật viên âm thanh\nCộng tác viên · The Ashford Herald\n\nMặt sau dán một mẩu giấy: \"Alex — Evelyn — nếu em không về thì đừng đến đây.\"",
                bodyEn = "JULIAN VANCE — Sound Technician\nContributor · The Ashford Herald\n\nA note taped to the back: \"Alex — Evelyn — if I don't come back, don't come here.\"" },
        };

        public static readonly DocDef Photo = new DocDef
        {
            id = "PHOTO_QuanSent", year = ChronoYear.Present, serif = false,
            titleVi = "Bức ảnh cuối của Julian", titleEn = "Julian's Last Photograph",
            bodyVi = "Ảnh chụp flash sàn nhà bếp, gửi đêm cuối cùng. Giữa vết bẩn hiện lờ mờ một vòng tròn năm cánh bằng máu mà mắt thường không thấy.",
            bodyEn = "A flash photo of the kitchen floor, sent on his last night. Inside the stain, faintly, a five-pointed circle drawn in blood — invisible to the naked eye.",
        };

        // ------------------------------------------------------------------ Băng cassette của Julian
        public class TapeDef
        {
            public string id, titleVi, titleEn, descVi, descEn;
            public (string vi, string en)[] lines;
        }

        public static readonly TapeDef[] Tapes =
        {
            new TapeDef { id = "TAPE_1", titleVi = "Băng #1 — balo ở Sảnh", titleEn = "Tape #1 — Backpack in the Hall",
                descVi = "Cuộn băng tìm thấy trong balo của Julian.", descEn = "Found in Julian's backpack.",
                lines = new[]
                {
                    ("Ngày thứ… ba? Alex, Evelyn — nếu ai đó nghe được cái này thì em xin lỗi vì không nghe máy.", "Day… three? Alex, Evelyn — if either of you hears this, I'm sorry I stopped answering."),
                    ("Em tìm ra rồi. Lối xuống hầm nằm dưới nhà bếp, ngay dưới cái bàn tiệc.", "I found it. The way down is under the kitchen — right beneath the dinner table."),
                    ("Có năm sợi xích. Không có ổ khóa nào cả…", "Five chains. And not a single lock…"),
                    ("(tiếng kim loại leng keng xa xa)", "(distant jingling of metal)"),
                    ("…Nghe thấy không? Tiếng chìa khóa.", "…Hear that? Keys."),
                    ("Và tiếng bước chân, một bên nặng một bên nhẹ. Như người đi khập khiễng.", "And footsteps — one heavy, one light. Like someone with a limp."),
                    ("Nó ở trên lầu. Em tắt máy đây.", "It's upstairs. I'm switching off."),
                } },
            new TapeDef { id = "TAPE_2", titleVi = "Băng #2 — cạnh két sắt", titleEn = "Tape #2 — Beside the Safe",
                descVi = "Cuộn băng để cạnh két sắt trong Phòng Làm Việc.", descEn = "Left next to the safe in the study.",
                lines = new[]
                {
                    ("Két khóa số. Ông Sterling viết mã nằm ở năm khai sinh cơ nghiệp…", "Combination safe. Sterling wrote the code is the year the fortune was born…"),
                    ("Em thử năm xây xong, 1924. Không được.", "I tried the year it was finished, 1924. Nothing."),
                    ("(tiếng chìa khóa leng keng trên trần)", "(keys jingling on the ceiling above)"),
                    ("Bà ta ở ngay trên đầu em.", "She's right above me."),
                    ("Đêm qua em thấy bà ta đứng ở chiếu nghỉ cầu thang nhìn xuống, nhưng không bước xuống.", "Last night I saw her on the landing, looking down. She didn't come down."),
                    ("Hình như bà ta không xuống được dưới này.", "I don't think she can come down here."),
                } },
            new TapeDef { id = "TAPE_3", titleVi = "Băng #3 — cầu thang hầm", titleEn = "Tape #3 — Cellar Stairs",
                descVi = "Cuộn băng rơi trên bậc thang đá, cạnh đống đá sập.", descEn = "Dropped on the stone steps beside the rubble.",
                lines = new[]
                {
                    ("(giọng yếu, thở gấp)", "(weak voice, laboured breathing)"),
                    ("Em tìm được cái lỗ đội thợ khoan năm 2009, sau vách bếp…", "I found the hole the 2009 crew drilled, behind the kitchen wall…"),
                    ("Chui lọt xuống được thì vách sập. Chân em kẹt dưới đá… chảy máu nhiều quá.", "I squeezed through and the wall came down. My leg's pinned… so much blood."),
                    ("Có ai đó dưới này, ngồi trên ghế, nói chuyện rất nhẹ nhàng.", "There's someone down here. Sitting in a chair. Speaking so gently."),
                    ("Lão bảo lão có thể cho em mọi thứ. Cho cái kênh của anh Alex. Cho bài báo của chị Evelyn.", "He says he can give me everything. Alex's channel. Evelyn's story."),
                    ("Nếu em không ra được… đừng nghe lão.", "If I don't make it out… don't listen to him."),
                } },
        };

        // ------------------------------------------------------------------ Kỷ vật & trang Vademecum (theo thứ tự Hanh 0..4)
        public class RelicDef
        {
            public string titleVi, titleEn, bodyVi, bodyEn, lineVi, lineEn;
            public string pageTitleVi, pageTitleEn, pageBodyVi, pageBodyEn;
            public string[] chantVi, chantEn;
        }

        public static readonly RelicDef[] Relics =
        {
            // 0 Moc → Đứa Trẻ (Child's Room)
            new RelicDef
            {
                titleVi = "Búp bê vải của bé Annabelle", titleEn = "Annabelle's Rag Doll",
                bodyVi = "Con búp bê vải sờn của bé Annabelle — con nuôi nhà Blackwood, bị nhốt và bỏ đói trong tủ áo năm 1924.\nTrong bụng búp bê có khâu một trang Vademecum.",
                bodyEn = "The threadbare doll of little Annabelle — the Blackwoods' ward, locked in a wardrobe and starved in 1924.\nA page of the Vademecum is stitched inside it.",
                lineVi = "Con lạnh quá… mở cửa cho con…", lineEn = "I'm so cold… please open the door…",
                pageTitleVi = "Vademecum — Trang Đứa Trẻ", pageTitleEn = "Vademecum — The Child's Page",
                pageBodyVi = "Bài chú bạn chép lại từ trang khâu trong búp bê.", pageBodyEn = "The verse you copied from the page inside the doll.",
                chantVi = new[] { "Nhỏ bé nhất, giữ cánh cửa đầu tiên.", "Bóng tối trong tủ là chăn của ngươi.", "Ngươi đói, và cơn đói giữ ngươi lại.", "Ngủ đi, Mắt Trận thứ tư." },
                chantEn = new[] { "The smallest one, keeper of the first door.", "The dark of the wardrobe is your blanket.", "You hunger, and the hunger holds you.", "Sleep now, fourth Eye of the circle." },
            },
            // 1 Thuy → Kẻ Lữ Khách (Guest Room)
            new RelicDef
            {
                titleVi = "Lá thư cầu cứu chưa gửi", titleEn = "The Unsent Letter",
                bodyVi = "\"Ai đọc được thư này xin báo cho gia đình tôi ở Bristol: tôi không bỏ đi. Bữa tối nay có vị lạ. Lord Blackwood rót rượu mãi không thôi…\"\n(Chữ nhòe máu ở cuối.)\nKẹp trong thư là một trang Vademecum.",
                bodyEn = "\"Whoever finds this, please tell my family in Bristol: I did not run away. Tonight's supper tasted wrong. Lord Blackwood kept filling my glass…\"\n(The ink smears into blood at the end.)\nA page of the Vademecum is folded inside.",
                lineVi = "(Tiếng ly thủy tinh vỡ đâu đó.)", lineEn = "(Somewhere, a wine glass shatters.)",
                pageTitleVi = "Vademecum — Trang Kẻ Lữ Khách", pageTitleEn = "Vademecum — The Wayfarer's Page",
                pageBodyVi = "Bài chú bạn chép lại từ trang kẹp trong lá thư.", pageBodyEn = "The verse you copied from the page inside the letter.",
                chantVi = new[] { "Người lạ đến, người lạ ở lại.", "Rượu đỏ là dây buộc.", "Thư không bao giờ tới nơi.", "Ở lại đi, Mắt Trận thứ năm." },
                chantEn = new[] { "The stranger came, the stranger stays.", "Red wine is the binding cord.", "The letter never reaches home.", "Stay now, fifth Eye of the circle." },
            },
            // 2 Tho → Người Giữ Chìa / Mrs. Peake (Servant Quarters) — giữ logic choáng 20 s
            new RelicDef
            {
                titleVi = "Lược sừng của Mrs. Peake", titleEn = "Mrs. Peake's Horn Comb",
                bodyVi = "Chiếc lược sừng cổ của bà quản gia Eleanor Peake — người đầu tiên bị trói linh hồn vào Khối Cổ Vật, biến thành The Keeper.\nQuấn quanh lược là một trang Vademecum.",
                bodyEn = "The antique horn comb of housekeeper Eleanor Peake — the first soul bound into the Obsidian Relic, made into The Keeper.\nA page of the Vademecum is wound around it.",
                lineVi = "Chạy đi… ta không kiềm chế được…", lineEn = "Run… I can't hold myself back…",
                pageTitleVi = "Vademecum — Trang Người Giữ Chìa", pageTitleEn = "Vademecum — The Keeper's Page",
                pageBodyVi = "Bài chú bạn chép lại từ trang quấn quanh chiếc lược.", pageBodyEn = "The verse you copied from the page wound around the comb.",
                chantVi = new[] { "Kẻ trung thành nhất, giữ mọi chìa khóa.", "Ngươi canh cửa cho đến tận cùng.", "Không ai ra, không ai vào nếu ngươi không cho.", "Canh giữ đi, Mắt Trận thứ nhất." },
                chantEn = new[] { "The most loyal one, keeper of every key.", "You guard the doors until the end of days.", "None leave, none enter, without your leave.", "Keep watch, first Eye of the circle." },
            },
            // 3 Kim → Người Thợ / Thomas (Library)
            new RelicDef
            {
                titleVi = "Búa của Thợ Cả Thomas", titleEn = "Master Builder Thomas's Hammer",
                bodyVi = "Chiếc búa đóng đinh đẫm máu. Thợ Cả Thomas bị Lord Blackwood giết bằng chính chiếc búa này ngay khi hầm hoàn tất.\nQuấn quanh cán búa là một trang Vademecum.",
                bodyEn = "A bloodied claw hammer. Lord Blackwood killed Master Builder Thomas with it the night the cellar was finished.\nA page of the Vademecum is wrapped around the handle.",
                lineVi = "(Tiếng gõ búa vọng ra từ trong tường.)", lineEn = "(Hammering echoes from inside the walls.)",
                pageTitleVi = "Vademecum — Trang Người Thợ", pageTitleEn = "Vademecum — The Builder's Page",
                pageBodyVi = "Bài chú bạn chép lại từ trang quấn quanh cán búa.", pageBodyEn = "The verse you copied from the page around the hammer.",
                chantVi = new[] { "Bàn tay dựng nên ngôi nhà.", "Đá và vữa là xương thịt ngươi.", "Tám người theo ngươi vào vách.", "Đứng vững đi, Mắt Trận thứ hai." },
                chantEn = new[] { "The hands that raised this house.", "Stone and mortar are your flesh.", "Eight men followed you into the walls.", "Stand firm, second Eye of the circle." },
            },
            // 4 Hoa → Quý Bà Isabella (Master Bedroom) — bài chú thứ 5 (Kết 2 đọc ngược)
            new RelicDef
            {
                titleVi = "Đôi giày lụa thêu của Quý bà Isabella", titleEn = "Lady Isabella's Silk Slippers",
                bodyVi = "Đôi giày lụa thêu chỉ bạc của Quý bà Isabella Blackwood, người vợ bị chồng đem làm Mắt Trận khi đang mang thai.\nLót trong giày là một trang Vademecum — trang thứ năm.",
                bodyEn = "Silver-embroidered silk slippers of Lady Isabella Blackwood, made an Eye of the circle by her own husband while she was with child.\nTucked inside is a page of the Vademecum — the fifth page.",
                lineVi = "(Mùi hoa oải hương héo thoảng qua.)", lineEn = "(A scent of withered lavender drifts by.)",
                pageTitleVi = "Vademecum — Trang Quý Bà (bài chú thứ năm)", pageTitleEn = "Vademecum — The Lady's Page (the fifth verse)",
                pageBodyVi = "Bài chú bạn chép lại từ trang lót trong giày. Lề trang ghi bằng mực đỏ: \"Đọc ngược để nhận lấy Cổ Vật.\"",
                pageBodyEn = "The verse you copied from the page inside the slippers. A note in red ink in the margin: \"Read it backwards to claim the Relic.\"",
                chantVi = new[] { "Người được yêu nhất, bị phản bội nhất.", "Lụa trắng hóa tro trong lửa nến.", "Lời thề trước bàn thờ là xiềng xích.", "Ở bên ta mãi, Mắt Trận thứ ba." },
                chantEn = new[] { "The most beloved, the most betrayed.", "White silk turned to ash in candle flame.", "The vow before the altar is the chain.", "Stay by me forever, third Eye of the circle." },
            },
        };
    }
}
