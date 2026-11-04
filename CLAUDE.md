@GEMINI.md

# Quy ước bổ sung cho Claude Code — dự án "Lời Nguyền Khóa Hồn"

Tài liệu gốc: `Docs/KhoaHon/GDD_KhoaHon_v2.md` (cốt truyện), `Docs/KhoaHon/PROMPT_ClaudeCode_KhoaHon.md` (đặc tả kỹ thuật), `Docs/KhoaHon/PLAN.md` (kế hoạch đã duyệt). Quy trình MCP chi tiết: `.agents/skills/unity-mcp-workflow/SKILL.md`.

## Thứ tự ưu tiên
Yêu cầu trực tiếp trong chat → `GEMINI.md` + skill unity-mcp-workflow → PROMPT → GDD. Mâu thuẫn về cốt truyện thì theo GDD; về kỹ thuật/số liệu thì theo PROMPT.

## Quy trình
- Làm theo từng Phase trong PROMPT mục 9. Hết mỗi Phase: commit, cập nhật `Docs/KhoaHon/CHANGELOG.md`, báo cáo theo mẫu mục 11, rồi **dừng chờ duyệt**.
- Bước "`view_file` ảnh" trong GEMINI.md: trong Claude Code dùng tool **Read** để mở file PNG.
- Ảnh kiểm tra lưu vào `Temp/KhoaHon/` (không commit).
- Chụp ảnh khi không Play: `manage_camera` action `screenshot` với `view_position`/`view_target`. Khi Play: `MCPVisionCapture.Capture("Temp/KhoaHon/x.png")`.
- Kiểm tra font/UI mà không đụng scene đang mở: dựng `EditorSceneManager.NewPreviewScene()`, render camera vào RenderTexture rồi ghi PNG.

## Không được làm
- Không sửa `MapKinhDi_Ver1.unity`, `SampleScene.unity`, scene demo của asset pack. Không **lưu** scene MapKinhDi đang mở, kể cả khi nó đang dirty.
- Không sửa `Scripts/Ghost/GhostAI.cs`, không sửa IK/pose/offset/light của Slot 1 (Flashlight) và Slot 2 (Radar).
- Không sửa file trong asset pack bên thứ ba: `Flooded_Grounds`, `Abandoned_Asylum`, `Scenes/ZNS3D`, `civilian_girl`, `FpsHorrorKit`, `TextMesh Pro`, `Plugins`. Cần biến thể thì tạo Prefab Variant/material trong `Assets/_KhoaHon/`.
  - Lưu ý: gọi `HasCharacters(..., searchFallbacks: true)` hoặc dùng font LiberationSans với ký tự lạ sẽ làm Unity tự ghi vào `LiberationSans SDF - Fallback.asset`. Nếu `git status` thấy file này bị sửa thì `git checkout` lại.
- Không đổi Asset Serialization, render pipeline, phiên bản package, Input System backend khi chưa hỏi.
- Không xóa asset hiện có, không push, không merge.

## Thư mục & code
- Mọi thứ mới nằm trong `Assets/_KhoaHon/` (Scenes, Scripts/<Core|Interaction|Items|Notebook|Puzzles|AI|Sanity|Narrative|Sequences|Debug>, Data, Prefabs, Materials, Audio, Timelines, Fonts, Textures).
- Namespace `KhoaHon.<Folder>`. Tên class/biến tiếng Anh; comment và tooltip tiếng Việt.
- Code gameplay để trong **Assembly-CSharp** (không asmdef) vì cần gọi `PlayerController` (global namespace) và `HorrorGame.Inventory`. Project hiện không có asmdef nào ngoài package.
- Logic thuần (StoryFlags, LotusAltar check, SafeCode, FSM transition table) viết thành class C# không phụ thuộc MonoBehaviour để test được.
- EditMode test (đã chốt ở Phase 1): đặt trong `Assets/_KhoaHon/Tests/Editor/` (thuộc Assembly-CSharp-Editor, tự thấy Assembly-CSharp và NUnit — **không cần asmdef**). Chạy bằng MCP `run_tests` mode `EditMode`, `group_names: ["KhoaHon.Tests"]`.
- Mỗi ScriptableObject/MonoBehaviour phải nằm trong file `.cs` cùng tên class (Unity yêu cầu).
- Sau khi tạo/xóa file `.cs` từ bên ngoài Unity: gọi `AssetDatabase.ImportAsset(path, ForceUpdate)` cho từng file mới rồi `CompilationPipeline.RequestScriptCompilation()`. Nếu Console báo `CS2001 ... could not be found` (Unity còn nhớ file đã xóa) hoặc file mới không vào danh sách biên dịch: tạo lại file trống rồi xóa bằng `AssetDatabase.DeleteAsset`, hoặc `AssetDatabase.MoveAsset` file mới sang thư mục khác. Kiểm tra bằng `CompilationPipeline.GetAssemblies(...).sourceFiles`.
- **Scene chính (Hướng B):** `KH_Main` = bản sao MapKinhDi. Dựng lại toàn bộ bằng menu `KhoaHon/Build/3. Convert MapKinhDi → KH_Main (Hướng B)` (`KhoaHonVillaConverter`). Lệnh này ghi đè KH_Main, nên mọi chỉnh sửa scene phải nằm trong công cụ, không sửa tay trong KH_Main.
- Greybox cũ: `KH_Greybox.unity` (chỉ để tham khảo). Dữ liệu: `KhoaHon/Build/1. Data Assets`.
- Blender: `"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" --background --factory-startup --python Tools/Blender/khoahon_props.py -- "D:/refactored-adventure/Tools/Blender/out" [TênModel...] [--preview|--icons]`. Converter tự nhập FBX từ `Tools/Blender/out`.
- Bake NavMesh trong Editor ngay sau khi vừa tạo collider: phải gọi `Physics.SyncTransforms()` trước `BuildNavMesh()`, nếu không collider mới bị bỏ qua.
- Ghi nhiều thuộc tính bằng 2 `SerializedObject` trên cùng một object: Apply cái thứ nhất trước rồi mới set mảng bằng cái thứ hai, nếu không sẽ bị ghi đè.
- Ảnh PNG nhập làm Sprite: đặt `spriteImportMode = Single` (Unity 6 mặc định Multiple, khi đó không có sub-asset Sprite).
- Khi test Play qua MCP: đặt `PlayerController.mouseSensitivity = 0` ở runtime (chuột thật của người dùng làm lệch camera). Tự đi bộ kiểm tra đường: `KhoaHon.DebugTools.KhoaHonAutoWalker.Begin(points, straight)`.
- Text hiển thị cho người chơi đặt trong ScriptableObject ở `Assets/_KhoaHon/Data/`, không hardcode.
- Tham số cân chỉnh gameplay đặt trong ScriptableObject config ở `Data/Configs/` (tránh bẫy SerializedField).
- Thêm `ItemType` mới **chỉ ở cuối enum**: `Recorder, FilmCamera, Crowbar`.

## Font tiếng Việt (đã kiểm chứng ở Phase 0)
- UI: `Assets/_KhoaHon/Fonts/BeVietnamPro-Regular SDF.asset` / `BeVietnamPro-SemiBold SDF.asset`.
- Tài liệu, bùa, bảng đồng: `NotoSerif-Regular SDF.asset` / `NotoSerif-Bold SDF.asset`.
- Đều là Dynamic SDF, giấy phép OFL (file `OFL_*.txt` cùng thư mục). Không dùng LiberationSans cho chữ tiếng Việt (thiếu glyph).

## Git
- Nhánh làm việc: `feature/khoa-hon-v2`. Commit nhỏ theo từng hệ thống, message tiếng Anh `feat(khoahon): ...`.
