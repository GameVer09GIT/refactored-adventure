# CHANGELOG — Khóa Hồn

## Phase 0 — Khảo sát & kế hoạch (2026-11-05)
- Commit WIP của MapKinhDi lên `main` (`8def707`), tạo nhánh `feature/khoa-hon-v2`.
- Khảo sát dự án qua Unity MCP; ghi kết quả vào `PLAN.md` mục 1.
- Thêm font OFL Be Vietnam Pro + Noto Serif và 4 TMP Font Asset Dynamic trong `Assets/_KhoaHon/Fonts/`; đã kiểm chứng hiển thị dấu tiếng Việt.
- Tạo `CLAUDE.md`, `Docs/KhoaHon/PLAN.md`, `ASSETS_NEEDED.md`, `CHANGELOG.md`.
- Chưa sửa scene hay code gameplay nào.

## Phase 1 — Greybox & điều khiển (2026-11-05)
- Scene mới `Assets/_KhoaHon/Scenes/KH_Main.unity` (đã thêm vào Build Settings sau `SampleScene`), dựng hoàn toàn bằng công cụ `KhoaHon/Build/2. Greybox Scene KH_Main`:
  - Sân, cổng, hàng rào, đường 270 m, xe `Prop_Car_A`.
  - Biệt thự 2 tầng, 10 phòng (`Room_*` có `RoomMarker`), cầu thang lên tầng 2 (dốc collider + bậc hiển thị), cửa bản lề.
  - Hầm: cầu thang đá sâu 10 m, chiếu nghỉ + đống đá sập 2009, hành lang, xà lim, sảnh tế bát giác.
  - NavMesh có area mới `Floor2` (index 3 trong NavMeshAreas): tầng 2 + nửa trên cầu thang.
- Player: bản sao Player/Canvas/DeathScreenManager/EventSystem/SpawnPoint từ MapKinhDi (không lưu scene nguồn) → prefab `KH_Player.prefab`. Hotbar: Đèn pin · Máy ghi âm · Máy ảnh film · Xà beng (Radar gỡ khỏi hotbar chiến dịch).
- Code mới (`Assets/_KhoaHon/Scripts`):
  - Core: StoryFlags, GameEvents, SaveData/SaveSystem, GameManager, Checkpoint/CheckpointManager, HUD, ModalUI, TextTable, UIStyle, FlagActivator, RoomMarker.
  - Interaction: PlayerInteractor (E, 2.2 m), DoorInteractable (khóa/chìa, mở ra xa người chơi), Pickup, ReadableDocument + DocumentReaderUI, InfoInteractable, StoryTrigger, KeyRing.
  - Items: BatteryPool, FilmStock, FlashlightBatteryDrain (hook, không sửa FlashlightItem), Recorder/FilmCamera/Crowbar (cầm được), HotbarGate, RelicInventory, ToolsConfig.
  - Notebook: NotebookManager + NotebookUI 5 tab, dữ liệu Document/Tape/Talisman/Relic/PhotoClue, KhoaHonDatabase.
  - Debug: KhoaHonCheatPanel (F10), KhoaHonAutoWalker.
- Dữ liệu: văn bản PROMPT mục 8 (tài liệu 1921–2009, 3 băng của Quân, 5 kỷ vật + 5 trang bùa, ảnh Quân gửi), TextTable tiếng Việt.
- Code dùng chung bị sửa: chỉ thêm `Recorder, FilmCamera, Crowbar` vào cuối enum `ItemType`.
- Test EditMode: 10 test (`Assets/_KhoaHon/Tests/Editor/KhoaHonCoreTests.cs`).

## Hướng B — dùng map cũ, AI mới, model Blender (2026-11-05)
- `KH_Main` giờ là bản sao `MapKinhDi_Ver1`, dựng bằng `KhoaHon/Build/3. Convert MapKinhDi → KH_Main (Hướng B)`. Greybox Phase 1 giữ ở `KH_Greybox.unity`. File map gốc không bị sửa.
- Bố cục 10 phòng + hầm đặt trong biệt thự Villa1 (xem PLAN.md mục 10). Cửa gốc được chuyển thành cửa tương tác, tầng 2 chia phòng bằng mảnh tường gốc, vá 3 lỗ sàn hư, gỡ rào chắn cầu thang của bản demo, thêm dốc vô hình cho cầu thang chữ U.
- Bà Quản Gia thay GhostAI demo: `HousekeeperBrain` (thuần, 11 test) + `HousekeeperAI` với tuần tra 16 waypoint, nghi ngờ (tiếng động, đèn pin rọi mặt), truy sát ×1.15 runSpeed, choáng (flash 3 s / lược 20 s), chế độ yếu, bắt → jumpscare → màn hình chết → hồi sinh ở checkpoint. Chỉ hoạt động ở area Floor2.
- Trốn tủ / gầm giường (`HidingSpot`, 6 chỗ) + nín thở (`BreathHold`) + tiếng bước chân/chạy (`PlayerNoiseEmitter`).
- Model `civilian_girl` mặc áo bà ba đen (thân, vạt, tay), tóc dài, chùm chìa khóa, da tái, mắt đục; Animator Idle/Walk/Run.
- Blender (chạy nền): 22 model đạo cụ + icon hotbar + ảnh xem trước (`Tools/Blender/khoahon_props.py`). Vật cầm tay (máy ghi âm, máy ảnh, xà beng) dùng model Blender.
- Âm thanh tự tổng hợp (`KhoaHonAudioSynth`): chùm chìa khóa kéo lê, hát ru "ầu ơ" (giai điệu tự viết), nhịp truy đuổi, thì thầm "chạy đi".
- Ánh sáng: nến leo lét ở Phòng Thờ (`CandleFlicker`), đèn chập chờn ở hành lang tầng 2, ánh trăng rọi qua cửa sổ sảnh.
- Test EditMode: 21/21.

## Bản kinh dị + giao diện kiểu game Steam (2026-11-05)
- **Luồng vào game như game thương mại** (`Scripts/UI`): màn cảnh báo nội dung → chỉnh độ sáng (lần đầu) → menu chính điện ảnh (biệt thự gothic trong đêm, cửa sổ đỏ ánh máu, sét chớp, nhạc hộp nhạc, tiêu đề chập chờn) → intro chữ gõ → vào game. Nút: Tiếp tục · Trò chơi mới · Cài đặt · Thông tin · Thoát, hộp xác nhận.
- **Cài đặt đầy đủ** 4 tab — Hình ảnh (chế độ màn hình, độ phân giải, VSync, giới hạn FPS, chất lượng, FOV, làm mờ chuyển động, nhiễu hạt), Âm thanh (Tổng/Nhạc/Hiệu ứng/Môi trường/Giọng nói, phụ đề), Điều khiển (độ nhạy, bảng phím), Hiển thị (độ sáng, FPS, ngôn ngữ). Lưu PlayerPrefs; nút Mặc định/Áp dụng/Quay lại.
- **Menu tạm dừng (Esc)**: Tiếp tục · Cài đặt · Tải lại điểm lưu · Về menu chính · Thoát. HUD tối giản: chấm tâm phóng to khi nhắm vật, thanh pin 5 ô, đếm FPS.
- **Hậu kỳ kinh dị** (`KH_HorrorVolume` + `HorrorPostFX`): vignette, quang sai, nhiễu hạt, méo ống kính tăng khi bà Quản Gia lại gần; nhịp đỏ + tim đập khi bị truy sát.
- **Máu me**: ~90 vết máu (vũng, vệt kéo, dấu tay, dấu tay trẻ con, máu chảy trên tường) đặt theo cốt truyện từng phòng; máu nhỏ giọt (particle) ở 5 điểm; sọ + xương lòi ra từ vách đường hầm (8 phu hồ), đống xương và móc treo trong sảnh tế; xác Quân bọc vải liệm thấm máu (model Blender `KH_XacQuan`) nằm trong vũng máu ở xà lim.
- **Bà Quản Gia đáng sợ hơn**: máu thấm vào texture áo bà ba và da (mắt chảy máu, vạt áo đẫm máu), mắt đen ánh đỏ, dáng đi khập khiễng/giật cổ (`HousekeeperCreep`), tiếng thở, tiếng thét, nhạc truy đuổi mới; jumpscare mặt 3D áp sát ống kính + nhiễu đỏ + rung → màn "BẠN ĐÃ CHẾT" tiếng Việt.
- **Âm thanh tự tổng hợp mới** (15 file): không khí nhà cũ, tiếng gõ/kẽo kẹt/thì thầm/đập cửa ngẫu nhiên sau lưng (dày hơn khi tỉnh táo thấp), stinger khi bị phát hiện, tim đập nhanh, jumpscare, hộp nhạc menu, âm UI.
- Sửa: ảnh ma chụp người thật của `DeathScreenManager` cũ không còn hiện (tắt RawImage + tắt tiếng hét cũ, không sửa file gốc); ký tự `▮ ● →` không có trong font được thay.
- Test EditMode: 21/21. Console 0 lỗi.


## Blackwood A — Đa ngôn ngữ + nội dung kịch bản mới (2026-11-07)
- Kịch bản mới `Kich_Ban_Game.docx` (Blackwood Manor: The Soul-Lock Curse). Định danh nội bộ giữ nguyên (`StoryFlag`, `Hanh`, id asset) để không vỡ save/test; chỉ đổi nội dung hiển thị. Ánh xạ kỷ vật: Moc → búp bê Annabelle, Thuy → thư khách trọ, Tho → lược Mrs. Peake (giữ choáng 20 s), Kim → búa Thomas, Hoa → giày Isabella (bài chú thứ 5).
- `Loc` (Core/Localization.cs): ngôn ngữ Việt/Anh, mặc định Việt, lưu PlayerPrefs `kh_lang`, sự kiện `Changed`.
- `TextTable` có cột `en`; `NotebookSource` có `titleEn/bodyEn`; `TapeData.Line.textEn`, `RelicData.pickupLineEn`, `TalismanData.chantLinesEn`.
- Toàn bộ chữ song ngữ nằm ở `Editor/KhoaHonContent.cs`; `KhoaHonDataBuilder` đọc từ đó (175 key, 9 tài liệu, 3 băng, 5 kỷ vật + 5 trang Vademecum, thoại Lord Blackwood, 3 kết).
- `CharacterProfile` (Evelyn, Alex) + `PlayerCharacter`; `GameManager.Character`, `GameFlow.PendingCharacter`; `SaveData` v2 có `character` (v1 tự chuyển, mặc định Evelyn). Intro theo nhân vật.
- Cài đặt: chọn Tiếng Việt / English, đổi ngay; menu chính và menu tạm dừng dựng lại sau khi đóng Cài đặt.
- Test: 27/27 (thêm 6 `LocalizationTests`). Ảnh: `Temp/KhoaHon/a_boot.png`, `a_settings_en.png`, `a_menu_en.png`.
