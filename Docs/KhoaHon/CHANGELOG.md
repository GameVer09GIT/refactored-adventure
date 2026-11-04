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
