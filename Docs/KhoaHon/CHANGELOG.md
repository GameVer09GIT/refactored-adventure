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

## Blackwood B — Nâng đồ họa URP (2026-11-07)
- Giữ URP (người dùng chọn). Không sửa `Assets/Settings/*`: tạo bản riêng bằng `KhoaHon/Build/4. Graphics (URP + Quality)` (`KhoaHonGraphicsBuilder`).
  - `Assets/_KhoaHon/Settings/KH_Renderer.asset` (Forward+): SSAO chất lượng cao, Decal, **sương thể tích**.
  - `KH_URP_Low/Medium/High/Ultra`: bóng 1024→4096 (đèn phụ tới 8192), khoảng cách 25→70 m, 2→4 cascade, soft shadow; STP cho High/Ultra, render scale 0.75/0.85/1/1.
  - QualitySettings thêm Low/Medium/High/Ultra (Mobile/PC giữ nguyên), mặc định Standalone = High. Cài đặt chỉ hiện 4 mức mới, lưu theo tên (`kh_quality_name`).
- Sương thể tích tự viết cho Render Graph: `Scripts/Rendering/KHVolumetricFogFeature.cs` + `KHVolumetricFog` (VolumeComponent) + `Shaders/KH_VolumetricFog.shader`. Raymarch nửa độ phân giải, tán xạ ánh trăng có bóng (tia sáng qua cửa sổ) và đèn phụ Forward+ (chùm đèn pin, đèn treo), blur giữ cạnh theo depth, trộn `Blend One SrcAlpha`. Bỏ 0,8 m đầu và kẹp suy giảm gần nguồn để không chói ở tay cầm đèn. Chất lượng theo mức đồ họa.
- Hậu kỳ `KH_HorrorVolume`: thêm Lift/Gamma/Gain, Split Toning (bóng lạnh, sáng ấm), Bloom chất lượng cao + lens dirt sinh thủ tục, DoF Bokeh (tự bật khi đọc tài liệu/sổ tay hoặc `HorrorPostFX.CinematicFocus`). Các Volume toàn cục cũ của MapKinhDi/greybox bị tắt — chỉ còn 1 profile.
- Converter: đèn treo tầng 2 có bóng mềm; trăng lạnh bóng mềm; ambient gần đen; LightingSettings riêng `KH_LightingSettings` (không còn trỏ vào Flooded_Grounds); bake 40 reflection probe thành cubemap riêng (`Textures/Probes`, chế độ Custom).
- Quyết định: máu vẫn là quad URP Lit (giữ độ bóng ướt; Decal Shader Graph mặc định không có smoothness). Decal để dành cho chữ máu/dấu chân night vision (Phase D). Chưa bake GI/APV (scene ~2.300 prefab Flooded_Grounds, bake rất lâu) — ánh sáng trực tiếp realtime phù hợp phong cách "một nguồn sáng".
- Đo: editor 1080p mức High ~51 fps trên RTX 3050 Laptop. Test 27/27. Ảnh: `Temp/KhoaHon/b2_hall.png`, `b2_f2.png`, `b_fog_dense.png` (thử mật độ cao).

## Blackwood C — Lớp giải đố (2026-11-08)
- Logic thuần + test: `Scripts/Puzzles/PuzzleLogic.cs` (`SafeLock`, `AltarPuzzle`); `Tests/Editor/PuzzleTests.cs` (5 test).
- `SafeKeypad`: két 4 vòng số ở Phòng Làm Việc, mã 1921 (ảnh gia tộc). UI lớp trên cùng: A/D chọn vòng, W/S/cuộn chuột/gõ số, Enter thử; sai 3 lần hiện gợi ý "năm cơ nghiệp bắt đầu". Mở xong nhấn E lấy đồ (Pickup không hình: chìa khóa cầu thang + Vademecum tập I + bản thiết kế hầm, tự mở tài liệu). Cờ `SafeOpened`.
- `CandleStand` ×5 + `CandleAltar` ở Phòng Thờ: mỗi bệ dưới một tấm bia tên Mắt Trận (Annabelle, Vị khách không tên, Eleanor Peake, Thomas Hale, Isabella). Chọn trang Vademecum trong túi; đúng → nến bùng sáng; sai → nến tắt, −8 lý trí, tiếng động bán kính 14 m (Mrs. Peake nghe được). Đủ 5 → nhà rung (`CameraShake`), 5 xích rơi bằng vật lý, `CellarOpened` + Stage3 + lưu tại CP_Shrine.
- `TapePlayer`: phát băng (rè + clip nếu có + phụ đề "Julian:" theo thời gian), vẫn đi lại được, dừng khi chết/tạm dừng, nghe hết hồi lý trí. Sổ tay: chọn băng + [Space]/Enter để phát.
- `PryableWindow` ở cửa sổ sảnh (đóng ván): cần xà beng → hỏi xác nhận → `EndingDirector.Play(Coward)`.
- `EndingDirector` (bản đầu): đặt cờ kết, lưu thành tích `kh_ending_*`, hook cutscene cho Phase F, màn chữ kết (tiêu đề, lời kể, thống kê thời gian/số lần chết/tài liệu), xóa save rồi về menu.
- Âm thanh tổng hợp mới (`KhoaHonAudioSynthPuzzle.cs`): rè băng, click, núm két, mở két, sai mã, rung nhà, xích đứt, nến bùng/tắt, nạy cửa sổ.
- Sửa: lỗi `GetComponent<Rigidbody>() ?? AddComponent` (null giả của Unity) khiến xích không rơi.
- Ghi nhận cho Phase D: bàn thờ còn dáng Á Đông (bài vị sơn đỏ, đài sen) và các vệt máu nhỏ giọt trên cửa sổ trông như thanh nhựa → thay bằng bia đồng + giá nến sắt kiểu phương Tây, làm lại vệt máu.
- Test 32/32. Ảnh: `Temp/KhoaHon/c_safe_wrong.png`, `c_altar_choice.png`, `c_altar_lit.png`, `c_tape2.png`, `c_flee_confirm.png`, `c_ending_coward.png`.

## Blackwood D — Hai nhân vật và công cụ riêng (2026-11-08)
- **Màn chọn nhân vật** (sau Trò chơi mới): 2 thẻ Evelyn / Alex (tên, vai trò, động cơ, công cụ, icon). `GameFlow.PendingCharacter`; nạp save tự lấy nhân vật trước khi thanh đồ khởi tạo. `CharacterLoadout` (thứ tự −150) gán slot 2–4 theo `CharacterProfile.hotbar`; slot 1 đèn pin không đụng.
- **Evelyn**: máy ghi âm định hướng (`RecorderItem`): khi nghe, tiếng chìa khóa của Mrs. Peake pan trái/phải theo hướng + to dần khi gần, la bàn EVP trên HUD, thỉnh thoảng bắt được giọng EVP. Máy ảnh film có flash (`FilmCameraItem`, tốn phim, làm choáng). **Sơ đồ trinh thám** (phím M, `DetectiveMindMap`): bảng ghim 15 manh mối, 11 sợi chỉ đỏ có chú thích suy luận.
- **Alex**: `ItemType.Phone`, `ItemType.NightVision` (thêm cuối enum). Điện thoại: flash tốn pin, cầm thì khung livestream phóng to. **Livestream** (`LivestreamChat` + logic thuần `ChatDirector`): LIVE, số người xem tăng theo độ sâu, người xem trêu chọc/sợ hãi theo tầng, tài khoản ma (chữ đỏ) báo trước khi Mrs. Peake đến gần hoặc đang săn (từ tầng 2). Máy quay nhìn đêm (`NightVisionItem`): khuếch đại sáng, tông xanh, hạt, đèn hồng ngoại, tốn pin, hiện **dấu vết ẩn** (`HiddenInk`): dấu chân huỳnh quang Sảnh → nắp hầm, chữ "1921", lời cảnh báo của Julian.
- `FlashUtil` dùng chung cho 2 loại flash (nón 30°, tầm 9 m, kiểm tra che khuất → `HousekeeperAI.OnFlashed`). Tham số mới trong `ToolsConfig`.
- **Model Blender mới** (`Tools/Blender/blackwood_props.py`, `KH_BW_*`): bia đồng, giá nến sắt rèn, Vademecum, Khối Cổ Vật, điện thoại, máy quay, xe sedan, biển báo, máy chữ, bóng ma Lord Blackwood. Phòng thờ đổi sang kiểu phương Tây (13 bia đồng treo tường, giá nến sắt, tủ gỗ sồi + khăn đỏ).
- **Sửa lỗi lớn**: camera người chơi (từ MapKinhDi) tắt `renderPostProcessing` và không có tag MainCamera → mọi hậu kỳ chưa từng hiện khi chơi. Converter bật lại + khóa cứng trong `KhoaHonSettings.ApplyLive`. Cân lại tông màu: Tonemapping Neutral (ACES làm tối/ám xanh), phơi sáng nền +0.9, tắt Split Toning/LGG/SMH.
- Test 35/35 (thêm `ChatDirectorTests`). Ảnh: `Temp/KhoaHon/d_alex_phone.png`, `d_nv_pp.png`, `d_evelyn_compass.png`, `d_mindmap.png`, `d_grade_test1.png`, `d_bw_preview.png`.

## Blackwood E — Lý trí ẩn & Mrs. Peake (2026-11-08)
- `Sanity/SanityModel` (thuần, 4 test) + `SanitySystem`: lý trí 5–100 không có thanh hiển thị. Giảm trong bóng tối (0.35/s), nhìn thẳng Mrs. Peake (3.5/s), bị săn (1.2/s); trốn trong tối không giảm; chỗ sáng (đèn pin bật hoặc gần đèn) hồi 0.6/s tới 75. 4 bậc → `GameEvents.SanityTierChanged`.
- Hiệu ứng: quang sai dao động + nhịp tim to dần khi lý trí thấp (HorrorPostFX), bóng người thoáng qua ở rìa tầm nhìn biến mất khi quay sang nhìn, ảo thanh tiếng chìa khóa một bên tai (bậc 2–3).
- `NoisyProp` (5 đồ vật ở hành lang tầng 2) + `PlayerPushProps` (người chơi xô ngã đồ, chạy thì mạnh hơn) → tiếng động bán kính 11 m → Mrs. Peake nghi ngờ. Đã kiểm chứng: xô ấm đổ → bà tới và bắt người chơi.
- Âm thanh mới: `sfx_prop_clatter`, `sfx_prop_thud`.
- Test 39/39.

## Blackwood F — Cutscene & 3 kết thúc (2026-11-08)
- `Sequences/CutsceneKit`: camera điện ảnh tạm thời (tắt camera + điều khiển người chơi, ẩn HUD/thanh đồ), letterbox, màn mờ đen/trắng, phụ đề cutscene, hình người tối.
- **Giai đoạn 0 — vòng lặp cổng** (`GateLoopSequence`, chạy sau chữ mở đầu của Trò chơi mới): ngồi trong xe sedan (model Blender) trên đường z=−18, giữ W lái / S phanh / chuột nhìn quanh, đèn pha có bóng xuyên sương dày (Volume riêng). Lần 1 bóng phụ nữ giữa đường → phanh rít → tỉnh dậy trước cổng; lần 2 ba biển báo đổi thành tên người mất tích + radio khóc; lần 3 chết máy, đèn tắt, 2 cánh cổng sắt tự mở. Cờ GateLoop1–3. Đã kiểm chứng chạy trọn vẹn và trao quyền điều khiển trước bậc thềm dinh thự.
- **Xà lim**: `JulianBody` — xem thi thể Julian → dòng chữ khắc, phản ứng riêng của Alex/Evelyn, nhận chiếc còi (FoundQuan, HasWhistle), −12 lý trí.
- **Sảnh Tế** (`RitualSequence`): Khối Cổ Vật obsidian lơ lửng xoay trên vạc, bóng ma Lord Blackwood (vật liệu bán trong suốt phát sáng). Bước vào → 4 câu đàm phán với góc máy riêng (thoại riêng khi nói với Alex), DoF lấy nét theo chủ thể (`HorrorPostFX.FocusDistance`), rồi bảng lựa chọn. `EndingRules` (thuần, 2 test): Kết 1 cần đủ 5 kỷ vật + búa Thomas; Kết 2 cần bài chú thứ năm.
- **3 kết** (`EndingCutscenes` → `EndingDirector.CutsceneHook`, có lưới an toàn timeout):
  - I Giải thoát: kỷ vật vào vạc, lửa bùng, Cổ Vật vỡ (chớp trắng, rung), Lord tan biến, nhà rung → bình minh ngoài dinh thự (Volume trời quang).
  - II Tân Chủ Nhân: đọc ngược bài chú thứ năm (chữ đỏ), oan hồn quỳ → "Vài tháng sau": cửa sổ tầng 2 có bóng người, xe đỗ trước cổng, "Vào đi…".
  - Ẩn Kẻ Hèn Nhát: lái xe lúc 2 giờ sáng, quay lại thấy bóng quản gia ở ghế sau → Phòng 13 bệnh viện tâm thần (dựng trong converter, hình bát giác cào trên tường, tiếng cào).
- Đếm số lần chết cho màn thống kê (`kh_deaths_run`, reset khi chơi mới).
- Âm thanh mới: động cơ, phanh rít, chết máy, radio khóc, tiếng cào, Cổ Vật vỡ.
- Sửa: camera cutscene là con của xe nên bị xóa theo xe (kết ẩn kẹt màn đen).
- Test 41/41. Ảnh: `Temp/KhoaHon/f_gate1.png`, `f_gate3.png`, `f_after_gate.png`, `f_lord2.png`, `f_choice.png`, `f_nm1.png`, `f_release_end.png`.

## Blackwood G — Giao diện & hệ thống chuẩn Steam (2026-11-08)
- **3 ô lưu** (`SaveSystem`): `khoahon_save_{0..2}.json`, ô hiện tại nhớ trong PlayerPrefs, file cũ tự chuyển sang ô 1. Save thêm `savedAtTicks` + `playSeconds` (GameManager đếm giờ chơi).
- **Menu chính**: Tiếp tục (ô mới nhất) · Trò chơi mới (chọn ô → hỏi ghi đè → chọn nhân vật) · Tải trò chơi (danh sách ô: nhân vật, chương, giờ chơi, ngày lưu) · Cài đặt · Thêm · Thoát.
- **Thêm**: 3 thẻ kết thúc (khóa/mở), 11 thành tựu (`AchievementService`, có `IAchievementBackend` để gắn Steamworks sau), credits cuộn dọc.
- **Màn tải** (`LoadingScreen`): nạp scene bất đồng bộ, tên dinh thự, mẹo chơi ngẫu nhiên (9 mẹo), thanh tiến trình, mờ dần — dùng cho Trò chơi mới/Tiếp tục/Tải/Về menu/Kết thúc.
- **Cài đặt**: thêm Sương thể tích bật/tắt, nhóm Hỗ trợ (cỡ phụ đề 100/125/150%, rung camera bật/tắt).
- Tên vật phẩm trên thanh đồ theo ngôn ngữ (bản sao ItemData lúc chạy, không ghi asset; đổi ngôn ngữ cập nhật ngay). `HotbarGate.ReplaceItem`.
- Thống kê màn kết dùng giờ chơi thật. Phiên bản hiển thị 1.0.
- Test 41/41. Ảnh: `Temp/KhoaHon/g_menu.png`, `g_slots.png`, `g_extras.png`, `g_loading.png`.

## Blackwood H — Build Windows (2026-11-08)
- Sửa lỗi có sẵn khiến bản build phát hành không biên dịch được: `PlayerArmIKTuningUI` chỉ tồn tại trong Editor/Development nhưng `PlayerController` và `HotbarInventory` gọi vô điều kiện → thêm bản rỗng cho bản phát hành (`IsVisible` luôn false). Không sửa PlayerController/HotbarInventory/IK.
- `KhoaHon/Build/5. Windows Build (Blackwood Manor)` (`KhoaHonBuildTool`): chỉ KH_Main, mở lại scene từ đĩa trước khi build, productName "Blackwood Manor", xuất `Builds/BlackwoodManor/BlackwoodManor.exe`.
- Kết quả: **Succeeded**, 0 lỗi, 490 cảnh báo, 1.516 MB, 73 phút (lần đầu biên dịch shader; URP Lit 6144 biến thể). Chạy thử 30 giây ở chế độ cửa sổ: không crash, Player.log không có exception (chỉ lỗi chứng chỉ mạng vô hại).
- Chưa commit: thay đổi URP tự ghi khi build (`m_Prefiltering*` trong `Assets/Settings/*`, `KH_URP_*`, preloadedAssets/batching trong ProjectSettings, LightingData).

## Blackwood I — Sửa lỗi hình ảnh: tay cầm, vật lơ lửng, máu, con ma (2026-11-08)
- **Tay cầm đồ**: `PlayerArmIK` gập ngón cho mọi vật (trước chỉ đèn pin; Radar giữ nguyên tư thế cũ). Xà beng dùng chung HandPose/IK của đèn pin, nắm kín cán (thân dày ×1.35, thép sơn đỏ sẫm `M_KH_CrowbarSteel`). Máy ghi âm, máy ảnh, điện thoại, camera quay đêm xoay đúng mặt (ống kính ra trước, màn hình về người chơi). Thông số nằm trong `TuneHeldGrips` của converter.
- **Vật lơ lửng** (`FixPlacement` chạy cuối converter): tờ biên bản 2009 dán sát cửa chính có đinh ghim (trước cách cửa 0,5 m), ván đóng cửa sổ sảnh áp mặt tường + đinh + texture gỗ thật, 13 bia tưởng niệm gắn lên bảng gỗ dựng trên bàn thờ (trước treo lơ lửng trước cửa sổ). Giấy tờ dùng texture giấy cũ có chữ viết tay (`T_PaperNote`). Balo của Julian là model Blender thật (`KH_BW_Backpack`) thay khối hộp trắng. Giá nến làm lại bằng thân tiện (bản cũ bị xoắn méo thành khối bát diện).
- **Máu**: bỏ quad chữ nhật alpha-clip; thay bằng 103 URP Decal Projector chiếu lên sàn/tường/thảm (ôm theo gờ ván, không lộ khung). Texture sinh mới (`KhoaHonBloodTextures`): vết bắn có tia + giọt nòng nọc + sương li ti, vũng loang nhiều thùy, vệt kéo lê có sọc, dấu tay tách ngón/vân in/vệt trượt, máu chảy trên tường có hạt đọng; mép mềm, viền khô sẫm, normal map theo độ dày. Thêm ~25 vết máu dọc sảnh → cầu thang → hành lang tầng 2 (tường tìm bằng raycast).
- **Con ma (Mrs. Peake)**: bỏ áo bà ba khối cứng + màu phẳng. Giữ lưới skinned và normal map gốc của civilian_girl, albedo xử lý thành xác chết kiểu Banshee (`KhoaHonBansheeTextures`): da xám tái có gân, hốc mắt thâm đen, tròng trắng đục phát sáng nhẹ, nước mắt máu, máu trào miệng xuống cổ, khoang miệng tối, tóc xám bết, quần áo vải trắng ngà bẩn đẫm máu. Dáng cao gầy (scale 0.96/1.08/0.96). `HousekeeperCreep` thêm há hàm như đang gào (hàm + môi trên, mở rộng khi truy đuổi).
- Không sửa file asset pack (texture mới ghi vào `Assets/_KhoaHon/Textures/Housekeeper`, `Textures/Gore`).
- Test 41/41. Ảnh: `Temp/KhoaHon/i_crow3.png`, `i_cam3.png`, `i_rec3.png`, `j_phone2.png`, `j_nv2.png`, `v_doc.png`, `v_plaque.png`, `n_ghost2.png`, `n_crowbar_hall.png`, `blood_preview.png`.
