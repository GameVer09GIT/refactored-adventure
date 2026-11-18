@GEMINI.md

# Quy ước bổ sung cho Claude Code — dự án "Lời Nguyền Khóa Hồn"

Tài liệu gốc: `Docs/Blackwood/GDD_Blackwood_v2.md` (cốt truyện), `Docs/Blackwood/PROMPT_ClaudeCode_Blackwood.md` (đặc tả kỹ thuật), `Docs/Blackwood/PLAN.md` (kế hoạch đã duyệt). Quy trình MCP chi tiết: `.agents/skills/unity-mcp-workflow/SKILL.md`.

## Thứ tự ưu tiên
Yêu cầu trực tiếp trong chat → `GEMINI.md` + skill unity-mcp-workflow → PROMPT → GDD. Mâu thuẫn về cốt truyện thì theo GDD; về kỹ thuật/số liệu thì theo PROMPT.

## Quy trình
- Làm theo từng Phase trong PROMPT mục 9. Hết mỗi Phase: commit, cập nhật `Docs/Blackwood/CHANGELOG.md`, báo cáo theo mẫu mục 11, rồi **dừng chờ duyệt**.
- Bước "`view_file` ảnh" trong GEMINI.md: trong Claude Code dùng tool **Read** để mở file PNG.
- Ảnh kiểm tra lưu vào `Temp/Blackwood/` (không commit).
- Chụp ảnh khi không Play: `manage_camera` action `screenshot` với `view_position`/`view_target`. Khi Play: `MCPVisionCapture.Capture("Temp/Blackwood/x.png")`.
  - Khi đang ở menu (camera `BW_MenuCamera`), cả `manage_camera` lẫn `MCPVisionCapture` đều chụp nhầm camera người chơi đang tắt. Dùng `ScreenCapture.CaptureScreenshot("Temp/Blackwood/x.png")` (ảnh màn hình thật có UI) hoặc dựng camera tạm + `Camera.Render()` vào RenderTexture.
- Kiểm tra font/UI mà không đụng scene đang mở: dựng `EditorSceneManager.NewPreviewScene()`, render camera vào RenderTexture rồi ghi PNG.

## Không được làm
- Không sửa các scene gốc trong `Assets/BlackwoodManor/Scenes/Source/` (`MapKinhDi*.unity`, `SampleScene.unity`) và scene demo của asset pack. Không **lưu** scene MapKinhDi đang mở, kể cả khi nó đang dirty.
- Không sửa `Scripts/Ghost/GhostAI.cs`, không sửa IK/pose/offset/light của Slot 1 (Flashlight). Radar (Slot 2 cũ) đã gỡ khỏi thanh đồ — người dùng cho phép sửa thoải mái (2026-11-17).
- Không sửa nội dung file trong asset pack bên thứ ba: mọi thứ trong `Assets/ThirdParty/` (`Flooded_Grounds`, `Abandoned_Asylum`, `ZNS3D`, `civilian_girl`, `FpsHorrorKit`), cùng `Assets/TextMesh Pro`, `Assets/Plugins`. Cần biến thể thì tạo Prefab Variant/material trong `Assets/BlackwoodManor/`.
  - Lưu ý: gọi `HasCharacters(..., searchFallbacks: true)` hoặc dùng font LiberationSans với ký tự lạ sẽ làm Unity tự ghi vào `LiberationSans SDF - Fallback.asset`. Nếu `git status` thấy file này bị sửa thì `git checkout` lại.
- Không đổi Asset Serialization, render pipeline, phiên bản package, Input System backend khi chưa hỏi.
- Không xóa asset hiện có, không push, không merge.

## Thư mục & code
- **Cấu trúc thống nhất (từ 2026-11-17):** `Assets/` chỉ còn `BlackwoodManor/` (toàn bộ code + asset của game, kể cả script cũ `Scripts/Player|Inventory|Ghost|Audio`, công cụ cũ ở `Editor/Setup/`, scene gốc ở `Scenes/Source/`), `ThirdParty/` (asset pack), `Plugins/`, `TextMesh Pro/`, `Resources/DOTweenSettings.asset`. Tiền tố asset `BW_`, lớp/namespace `Blackwood*` (script cũ: `Blackwood.Inventory`, `Blackwood.PlayerAudio`, `Blackwood.Lighting`; `PlayerController`, `GhostAI`… vẫn ở global namespace). Khóa PlayerPrefs `kh_*` và file lưu `khoahon_save_*.json` giữ nguyên để không mất dữ liệu người chơi. Di chuyển/đổi tên asset luôn dùng `AssetDatabase.MoveAsset`/`RenameAsset` (giữ GUID) và khóa reload bằng `EditorApplication.LockReloadAssemblies()` trong lúc sửa code đi kèm.
- Mọi thứ mới nằm trong `Assets/BlackwoodManor/` (Scenes, Scripts/<Core|Interaction|Items|Notebook|Puzzles|AI|Sanity|Narrative|Sequences|Debug>, Data, Prefabs, Materials, Audio, Timelines, Fonts, Textures).
- Namespace `Blackwood.<Folder>`. Tên class/biến tiếng Anh; comment và tooltip tiếng Việt.
- Code gameplay để trong **Assembly-CSharp** (không asmdef) vì cần gọi `PlayerController` (global namespace) và `Blackwood.Inventory`. Project hiện không có asmdef nào ngoài package.
- Logic thuần (StoryFlags, LotusAltar check, SafeCode, FSM transition table) viết thành class C# không phụ thuộc MonoBehaviour để test được.
- EditMode test (đã chốt ở Phase 1): đặt trong `Assets/BlackwoodManor/Tests/Editor/` (thuộc Assembly-CSharp-Editor, tự thấy Assembly-CSharp và NUnit — **không cần asmdef**). Chạy bằng MCP `run_tests` mode `EditMode`, `group_names: ["Blackwood.Tests"]`.
- Mỗi ScriptableObject/MonoBehaviour phải nằm trong file `.cs` cùng tên class (Unity yêu cầu).
- Sau khi tạo/xóa file `.cs` từ bên ngoài Unity: gọi `AssetDatabase.ImportAsset(path, ForceUpdate)` cho từng file mới rồi `CompilationPipeline.RequestScriptCompilation()`. Nếu Console báo `CS2001 ... could not be found` (Unity còn nhớ file đã xóa) hoặc file mới không vào danh sách biên dịch: tạo lại file trống rồi xóa bằng `AssetDatabase.DeleteAsset`, hoặc `AssetDatabase.MoveAsset` file mới sang thư mục khác. Kiểm tra bằng `CompilationPipeline.GetAssemblies(...).sourceFiles`.
- **Scene chính (Hướng B):** `BW_Main` = bản sao MapKinhDi. Dựng lại toàn bộ bằng menu `Blackwood/Build/3. Convert MapKinhDi → BW_Main (Hướng B)` (`BlackwoodVillaConverter`). Lệnh này ghi đè BW_Main, nên mọi chỉnh sửa scene phải nằm trong công cụ, không sửa tay trong BW_Main.
- Greybox cũ: `BW_Greybox.unity` (chỉ để tham khảo). Dữ liệu: `Blackwood/Build/1. Data Assets`.
- Blender: `"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --factory-startup --python Tools/Blender/props_common.py -- "D:/refactored-adventure/Tools/Blender/out" [TênModel...] [--preview|--icons]`. Converter tự nhập FBX từ `Tools/Blender/out`.
- Bake NavMesh trong Editor ngay sau khi vừa tạo collider: phải gọi `Physics.SyncTransforms()` trước `BuildNavMesh()`, nếu không collider mới bị bỏ qua.
- Ghi nhiều thuộc tính bằng 2 `SerializedObject` trên cùng một object: Apply cái thứ nhất trước rồi mới set mảng bằng cái thứ hai, nếu không sẽ bị ghi đè.
- Ảnh PNG nhập làm Sprite: đặt `spriteImportMode = Single` (Unity 6 mặc định Multiple, khi đó không có sub-asset Sprite).
- Khi test Play qua MCP: đặt `PlayerController.mouseSensitivity = 0` ở runtime (chuột thật của người dùng làm lệch camera). Tự đi bộ kiểm tra đường: `Blackwood.DebugTools.BlackwoodAutoWalker.Begin(points, straight)`.
- Text hiển thị cho người chơi đặt trong ScriptableObject ở `Assets/BlackwoodManor/Data/`, không hardcode.
- Tham số cân chỉnh gameplay đặt trong ScriptableObject config ở `Data/Configs/` (tránh bẫy SerializedField).
- Thêm `ItemType` mới **chỉ ở cuối enum**: `Recorder, FilmCamera, Crowbar`.
- Luồng game: `GameFlow.State` (Boot → Menu → Intro → Playing ⇄ Paused). Hệ thống gameplay phải tự dừng khi `!GameFlow.IsPlaying`. Bảng Cài đặt / hộp xác nhận / menu tạm dừng nằm trên `UIKit.TopLayer` (canvas sort 95), không đặt dưới HUD (HUD bị tắt khi ở menu).
- Font Be Vietnam Pro / Noto Serif **không có** ký tự `▮ ■ █ ● → □`. Thanh/biểu tượng vẽ bằng `Image`; mũi tên viết `>`.
- Ngoại hình Mrs. Peake: lưới váy/khăn/tóc dựng bằng `Tools/Blender/housekeeper_garments.py` (chạy trong Blender đang mở qua MCP `blender_bridge run_python` hoặc chạy nền) → `Tools/Blender/out/characters/BW_HK_Garments.fbx`; `BlackwoodHousekeeperLook.Dress` gắn vào xương civilian_girl và tính lại bindpose. Xem trước nhanh không cần dựng scene: `BlackwoodHousekeeperLook.Preview(true)` → `Temp/Blackwood/hk_look_*.png`.
- Tự vệ bằng đèn pin: logic thuần ở `AI/LightExposure`, thông số ở `HousekeeperConfig` (nhóm "Chống lại bằng đèn pin"), hệ số độ khó ở `Core/Difficulty`. Chỉ đọc hướng đèn Spot của đèn pin — không sửa đèn.
- Pin: mỗi dụng cụ một loại (`Items/PowerCells`, thuần, có test): đèn pin = D, máy ghi âm = AA, điện thoại = sạc dự phòng, kính nhìn đêm = pin máy quay (`BatteryPool.CellFor`). API cũ của `BatteryPool` (`Current`, `Drain(float)`, `Add`) nghĩa là pin D. Thay pin bằng phím R (`Items/BatteryReload`); đồ nhặt pin đặt qua `Pickup.SetCell` và tự ẩn nếu nhân vật không có dụng cụ dùng loại đó. Đèn pin bật/tắt qua `FlashlightItem.SetLightStateImmediate`, chập chờn qua `SetFlicker` (không đổi trạng thái bật) — đừng gọi `SetLightStateImmediate(true)` để "khôi phục" sau chập chờn. Tốc độ hao pin ở `ToolsConfig`, được ép lại trong `BlackwoodDataBuilder`.
- Hệ thống không khí runtime (giông, bụi, hiện tượng khi ma ở gần, nhạc căng thẳng, objective) gắn trong `BlackwoodHorrorBuilder.WireSystems`; clip âm thanh stereo đăng ký trong `BlackwoodAudioSynth.StereoNames`.
- Âm thanh tổng hợp: `Write` chạy `Polish` (thông cao 24 Hz, vào 1 ms/tắt 8 ms cho âm một lần, trần độ to `LoudCap` cho stinger) rồi `Master`. Soát lại bằng script đo LUFS + ảnh phổ (`Temp/Blackwood/audio/spec_*.png`) sau khi sửa bộ tổng hợp.
- Âm thanh ElevenLabs: file gốc `.mp3` ở `Audio/EL/Raw/<tên>.mp3` (tên trùng clip tổng hợp, vd. `sfx_jumpscare`; biến thể bước chân `hk_step_01..04`). Menu `Blackwood/Build/Audio (ElevenLabs import)` (`BlackwoodAudioSynth.ImportElevenLabs`) giải mã bằng Unity → cắt lặng, nối vòng, khớp độ to BS.1770 với bản tổng hợp cũ (giữ cân bằng âm lượng) → `Audio/EL/<tên>.wav`. Mọi công cụ nạp clip qua `BlackwoodAudioSynth.Load(tên)` (ưu tiên EL). Gói miễn phí ElevenLabs giới hạn số lượt tạo mỗi phút: chạy ≤ 10 nút một lần.
- Tripo AI (model 3D): khóa API của người dùng lưu ngoài repo ở `%USERPROFILE%/.blackwood/tripo_key` — không bao giờ ghi vào file trong repo. Kiểm tra số dư: `GET https://openapi.tripo3d.ai/v3/account/balance`.
- Vùng map: `BlackwoodVillaConverter.VisibleArea` (x −45..225, z −130..100). `TrimPlayArea` xóa vật thể map gốc nằm hẳn ngoài vùng (+30 m) và tầng hầm cũ `MapDuoiLongDat`; người chơi bị chặn trong sân bởi `Bound_*` (BuildExteriorBlockers); sương exp² 0,017 + camera người chơi far 170 m.
- Chụp chuỗi ảnh theo mốc thời gian khi Play: đăng ký `EditorApplication.update` trong `execute_code` rồi gọi `ScreenCapture.CaptureScreenshot` (mỗi lệnh MCP cách nhau vài giây nên không chụp tay kịp cảnh rượt đuổi).
- Máu/xương/xác/hậu kỳ dựng trong `BlackwoodHorrorBuilder` (gọi từ converter). Không dán quad máu lên nhân vật: nhuộm vào texture (`Soak`).
- Chất liệu đạo cụ Blender: UV chiếu hộp theo mét trong `props_common.box_uv` (`UV_DENSITY`, `UV_FIT` cho màn hình); texture PBR sinh bởi `BlackwoodPropTextures` (bảng `Specs` theo tên vật liệu — vật liệu Blender mới phải thêm một dòng `Spec`). Nhập lại model không dựng scene: `Blackwood/Build/2b. Import Blender Models`; soát nhanh: `Preview Props`.
- Vật liệu URP tạo/sửa bằng script: bật phát sáng bằng `BlackwoodMaterialUtil.Emissive` (cờ `RealtimeEmissive`) và gọi `BlackwoodMaterialUtil.Validate` cuối cùng. Unity 6 tự tắt `_EMISSION` nếu cờ GI là `None`/`EmissiveIsBlack`.
- Ngoại hình thân/tay người chơi theo nhân vật: `BlackwoodPlayerLook` (texture + vật liệu `Materials/Player`) và component `Characters/PlayerLook`. Không đụng `PlayerArmIK`.
  - Tay áo/găng 3D: `BlackwoodPlayerGarments` chép tam giác của lưới `hand_left/right` theo trọng số xương rồi đẩy ra theo pháp tuyến (giữ UV/bindpose/trọng số → khớp IK, không cần Blender), lưu ở `Models/Player`. Chúng dùng chung vật liệu da của nhân vật nên ngưỡng cắt phải trùng ngưỡng vải/da 0,5 của `ClothMask`. Găng chỉ bật khi chơi Alex (`PlayerLook.alexOnly`).
- Test nạp file lưu: chép save sang slot trống (vd. `khoahon_save_2.json`) rồi nạp slot đó. Game tự lưu đè slot đang chơi khi chết/qua checkpoint — không thử trên save thật của người dùng.
- Chơi thử tự động (phải đang Play ở menu chính): `BlackwoodPlaytest.Start(PlayerCharacter, EndingId, extraHiding)` / `StartWalk()` hoặc menu `Blackwood/Test/...` — chạy trên slot 2, kết quả `PLAYTEST ...` trong Console + `Temp/Blackwood/playtest_report.txt`, ảnh `pt_*.png`. Kết thúc game ghi thành tích vào PlayerPrefs của Editor (registry `HKCU/Software/Unity/UnityEditor/DefaultCompany/Blackwood Manor` — sao lưu trước nếu cần giữ nguyên). Quét lỗi vặt không cần Play: `BlackwoodAudit.Run()` (khóa chữ + vật lơ lửng/lún).
- Đặt vật lên sàn/bàn trong converter: model Blender có tâm ở giữa vật → thêm tên vào danh sách `Settle` trong `FixPlacement`. Đồ nhặt nhỏ dùng model thật qua `SwapModel` (hộp `PickupBox` chỉ còn làm vùng tương tác).
- Ghi file vào `Assets` từ công cụ: `BlackwoodDataBuilder.WriteBytesIfChanged` (Windows hay khóa PNG đang mở → IOException 1224 làm converter dừng giữa chừng).
- Bóng đổ của đèn điểm/đèn chiếu do `Rendering/ShadowBudget` điều phối (atlas 4096 = 16 ô 1024 px; đèn điểm chiếm 6 ô). Đèn cần luôn có bóng thì đặt làm con của Player hoặc tăng ngân sách có tính toán.

## Font tiếng Việt (đã kiểm chứng ở Phase 0)
- UI: `Assets/BlackwoodManor/Fonts/BeVietnamPro-Regular SDF.asset` / `BeVietnamPro-SemiBold SDF.asset`.
- Tài liệu, bùa, bảng đồng: `NotoSerif-Regular SDF.asset` / `NotoSerif-Bold SDF.asset`.
- Đều là Dynamic SDF, giấy phép OFL (file `OFL_*.txt` cùng thư mục). Không dùng LiberationSans cho chữ tiếng Việt (thiếu glyph).

## Git
- Nhánh làm việc hiện tại: `Minh-Huy` (trước đây `feature/khoa-hon-v2`). Commit nhỏ theo từng hệ thống, message tiếng Anh `feat(blackwood): ...`.
