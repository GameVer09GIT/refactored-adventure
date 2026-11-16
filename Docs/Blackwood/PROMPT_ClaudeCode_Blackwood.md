# PROMPT CHO CLAUDE CODE — "LỜI NGUYỀN KHÓA HỒN" (Unity 6 · dự án `refactored-adventure`)

> Dán toàn bộ file này vào Claude Code đang mở ở thư mục gốc `D:\refactored-adventure`.
> Tài liệu cốt truyện đầy đủ nằm ở `Docs/KhoaHon/GDD_KhoaHon_v2.md`. Prompt này là **bản đặc tả kỹ thuật** để hiện thực GDD đó trong dự án hiện tại.

---

## 0. VAI TRÒ & MỤC TIÊU

Bạn là lập trình viên Unity chính của một dự án indie 1–2 người. Nhiệm vụ: biến GDD **"Lời Nguyền Khóa Hồn" (bản v2, vai chính Hạ Vy)** thành một bản game chơi được từ đầu đến cuối, có đủ **3 kết thúc**, trong dự án Unity hiện có. Bạn được tự do **tái sử dụng, sửa, hoặc tạo mới** vật thể, nhân vật, prefab, script — miễn là tuân thủ các quy tắc ở mục 2 và không làm hỏng những gì đang chạy ổn.

Làm việc **theo từng Phase** (mục 9). Kết thúc mỗi Phase: commit, viết báo cáo ngắn, rồi **dừng lại chờ tôi xác nhận** trước khi sang Phase tiếp theo.

Ưu tiên khi có xung đột: (1) yêu cầu trực tiếp của tôi trong chat → (2) `GEMINI.md` + `.agents/skills/unity-mcp-workflow/SKILL.md` → (3) prompt này → (4) GDD. Nếu GDD và prompt này mâu thuẫn về **cốt truyện**, theo GDD; về **kỹ thuật/số liệu**, theo prompt này.

---

## 1. HIỆN TRẠNG DỰ ÁN (đã khảo sát sẵn — hãy xác minh lại ở Phase 0, đừng tin mù quáng)

**Engine & package:** Unity `6000.3.21f1`, URP 17.3 (`Assets/Settings/PC_RPAsset.asset`, `HorrorGlobalVolumeProfile.asset`), Input System 1.20 (`Assets/InputSystem_Actions.inputactions`, code đang đọc trực tiếp `Keyboard.current`/`Mouse.current`), AI Navigation 2.0.14, Cinemachine 3.1.7, Timeline 1.8.12, Animation Rigging 1.3.1, TextMesh Pro, DOTween (`Assets/Plugins/Demigiant`), Test Framework 1.6, **MCP for Unity** (`com.coplaydev.unity-mcp`).

**Git:** đang ở nhánh `main`, có thay đổi chưa commit ở `Assets/MapKinhDi/MapKinhDi_Ver1.unity` và `Assets/Flooded_Grounds/Scenes/Scene_A_Terrain.asset`. Có nhánh `Minh-Huy`.

**Scene làm việc chính hiện tại:** `Assets/MapKinhDi/MapKinhDi_Ver1.unity` (~4 MB, **lưu dạng binary** dù project để Force Text — đừng cố đọc/parse file này bằng text, hãy thao tác qua Unity MCP). Scene này đã có: `Player`, `Ghost`, `Canvas` + `RespawnButton`, `NavMesh_Manager`, terrain, cây/bụi, rào, các khối nhà modular `Villa1_*`/`Villa2_*`, 10 đèn `PeriodicFlickerLight`, 278 `FootstepSurface`, 7 `FootstepZone`. Build Settings hiện chỉ có `Assets/Scenes/SampleScene.unity`.

**Script tự viết (namespace/vị trí):**
| Script | Vai trò | Ghi chú tái sử dụng |
|---|---|---|
| `Scripts/Player/PlayerController.cs` | FPS controller: walk 3.5, run 6.5 (Shift), stamina 100 (drain 22/s, regen 8/s), `SetControlEnabled`, `SetPitch`, `IsRunning`, `HorizontalSpeed` | **Dùng làm controller của Vy.** Thêm event tiếng động, không viết lại. |
| `Scripts/Player/RespawnManager.cs` | `RespawnPlayer()` về `spawnPoint` | Mở rộng thành hệ checkpoint. |
| `Scripts/UI/DeathScreenManager.cs` | Màn hình chết | Giữ, nối vào checkpoint. |
| `Scripts/Ghost/GhostAI.cs` | FSM `Patrol/Chase/Lost/Kill`, NavMeshAgent, tốc độ tính theo player, FOV 140°, detect 12m, hear 3m, jumpscare quay mặt + hét, `OnPlayerCaught`, `RespawnGhost` | **Không sửa** (scene cũ còn dùng). Đọc kỹ rồi viết AI mới cho bà Quản Gia, mượn lại logic jumpscare/LOS. |
| `Scripts/Ghost/GhostAmbientSound.cs`, `GhostFloat.cs`, `GhostMaterialFixer.cs` | Âm thanh rên, trôi lơ lửng, sửa material hồng | Tái sử dụng cho các hồn ma. |
| `Scripts/Inventory/*` (namespace `HorrorGame.Inventory`) | `HotbarInventory` 4 ô (phím 1–4, cuộn chuột, chuột trái → `HeldItem.OnPrimaryUse()`), `ItemData` (ScriptableObject, `ItemType {None, Flashlight, Radar, Generic}`), `HeldItem` (abstract: `OnEquip/OnUnequip/OnPrimaryUse/OnPlayerDied/OnPlayerRespawned`), `FlashlightItem`, `RadarItem`, `PlayerArmIK`, `HandPose`, `ViewModelSway` | Thêm item mới bằng cách kế thừa `HeldItem`. **Slot 1 Flashlight đã chạy ổn — cấm đụng IK/pose/offset/light của nó.** |
| `Scripts/Audio/*` (namespace `HorrorAudio`) | `PlayerFootsteps` (gỗ/đất theo `FootstepSurface`/`FootstepZone`), `PlayerBreathing`, `PlayerHeartbeat` | Dùng cho tiếng bước chân sàn gỗ, nhịp tim của hệ Sanity. |
| `Scripts/Lighting/PeriodicFlickerLight.cs` | Đèn chớp tắt | Dùng cho tầng 2. |
| `Scripts/Utils/MCPVisionCapture.cs` | `MCPVisionCapture.Capture("Temp/x.png")` | Dùng để tự kiểm tra hình ảnh. |
| `Assets/Editor/*` | `GhostSetupUtility`, `InventoryAutoSetupEditor`, `HandRigSetupEditor`, `AudioProceduralGenerator`, `SnapToGroundTool`, `FixPinkMaterials`… | Đọc trước khi tự viết tool trùng chức năng. |

**FpsHorrorKit** (`Assets/FpsHorrorKit/Scripts`, namespace `FpsHorrorKit`, comment tiếng Thổ Nhĩ Kỳ, **chưa dùng trong scene**): `HidingSystem` (trốn tủ, có `hidingPoint/outPoint`, xoay cửa), `DoorSystem` (cửa khóa/có chìa), `DrawerSystem`, `DragToOpenSystem`, `InspectSystem`, `LetterUIManager` (đọc thư, gõ chữ), `PhotoCaptureSystem`/`PhotoAlbum`, `DialogueSystem`/`DialogueData` (phụ đề), `ITOKey`, `ITOCamera`, `ITOCameraBattery`, `ITOLantern`, `IInteractable`, `PlayerInteract`. Các script này phụ thuộc `FpsController`/`PlayerInteract` của kit. → **Đọc để mượn ý tưởng/code, port sang namespace mới gắn với `PlayerController` của dự án. Không thêm `FpsController` của kit vào game.**

**Nhân vật / model có sẵn:**
- `Assets/civilian_girl/` — model nhân vật nữ (FBX + prefab + material). Tay FPS: `Assets/Prefabs/Hands/CivilianGirlHands.prefab`, `HandRig.prefab`. → **Đây là Hạ Vy.**
- `Assets/Ghost/Prefabs/Ghost.prefab` (mesh `ghost.obj`, `GhostVisual`, `M_GhostCloth`, `M_GhostEyes`), tiếng `jumpscare_scream.wav`, ảnh `you-died.jpg`. → **Nền cho bà Quản Gia.**
- `Assets/Animation/` — `X Bot@Idle.fbx`, `X Bot@Walking.fbx`, `Running.fbx`, `PlayerAnim.controller` (Mixamo X Bot). → Dùng làm thân hình placeholder cho hồn Quân, Trịnh Đại Quan, thi thể.

**Môi trường / đạo cụ có sẵn:**
- `Flooded_Grounds/Prefabs`: nhà modular `Villa1_*`, `Villa2_*` (tường, sàn, cầu thang trong `Villa1_IntStairs_A/B`, cửa `Villa1_Door_A/B`, kính cửa sổ), rào + **cổng sắt** `Struct_Fence1_Gate_A(_Door/_DM)`, `Struct_Pole_*`, `Struct_Billboard_*`, **xe** `Prop_Car_A` và **xe hỏng** `Prop_Car1_DM`, `Prop_Cabinet_A/B` (tủ áo), `Prop_Bed_A/B`, `Prop_Sofa_A`, `Prop_LargeTable_A`, `Prop_SmallTable_*`, `Prop_Chair_*`, `Prop_Lamp_A–E`, `Prop_Painting_A–D`, `Prop_Clock_A`, `Prop_Vase_*`, `Prop_Rug_A–D`, `Prop_Pulpit_A`, `Prop_Gravestone_A–E`, `ATM_InteriorDust`, `ATM_Leaves_*`, âm thanh `3D_DeepRattle`, `3D_Taps`, `3D_LeafRustle`; terrain layer có **Asphalt** (làm đường).
- `Scenes/ZNS3D/Prefabs` (Vintage Living Room): `wall_default`, `wall_with_big_door`, `wall_with_small_door`, `wall_with_one_window`, `wall_with_two_window`, `wall_corner*`, `floor_*`, `ceiling_*`, `bookshelf`, `book_1–9`, `gramophone`, `clock_wall`, `painting_1–6`, `photo_1–5`, `photo_frame_desk`, `sofa_big/small`, `tea_table`, `antique_light`, `light_desk`, `light_tall`, `curtain_1/2`, `carpet_1–3`, `vase_1–3`, `phone`, `ceramic_teapot`.
- `Abandoned_Asylum/Prefabs`: `Bed`, `Cupboard`, `BigShelf`, `KitchenShelf`, `TableKitchen`, `Kettle`, `BigKettle`, `Pot`, `Plate`, `Plate_Broken`, `Mug`, `Fridge`, `Chandelier`, `Lamp`, `LampWall`, `ChairLeather`, `MirrorShelf` (gương), `Statue`, `Pillar`, `Pipe`, `Box_V1/V2`, `Carpet`, `Paper`, `Board`, `Door_V1–V3`, `DoorD_V1/V2`, `MainDoor`, `Window*`, `WheelChair`, `MorgueBox`, `SurgeryTable`… và scene `Show.unity` (bệnh viện tâm thần — **dùng cho cảnh kết ẩn**). Không có nến, bài vị, đài sen, Ấn Đồng, vạc, búp bê, hài thêu, lược, búa, còi, máy ghi âm, máy ảnh film → tự dựng placeholder.
- `Assets/Audio` + `Assets/Resources/Audio`: bước chân gỗ/đất (+ `wood_creak_01–03`), thở/thở dốc, nhịp tim, `ghost_breath_loop`, `ghost_moan_loop`, `ghost_scare_01–03`, `flashlight_click`, `hotbar_switch`, `radar_blip/scan`.

---

## 2. QUY TẮC BẮT BUỘC

1. **Đọc trước khi làm:** `GEMINI.md`, `.agents/skills/unity-mcp-workflow/SKILL.md`, `Docs/KhoaHon/GDD_KhoaHon_v2.md`. Tạo `CLAUDE.md` ở gốc repo, dòng đầu `@GEMINI.md`, rồi thêm các quy ước của dự án Khóa Hồn (thư mục, namespace, cách test) để các phiên sau tự nạp. Trong Claude Code, bước "`view_file` ảnh" của GEMINI.md = dùng tool **Read** mở file PNG.
2. **Safe Play Mode:** luôn kiểm tra `EditorApplication.isPlaying` trước khi sửa/lưu Scene/Prefab/ScriptableObject; đang Play thì `manage_editor stop` trước.
3. **Bẫy SerializedField:** đổi giá trị mặc định trong `.cs` thì phải đồng bộ cả Inspector qua `SerializedObject` (hoặc khóa ở runtime). Tham số cân chỉnh gameplay đặt trong **ScriptableObject config** (mục 4.3) để tránh bẫy này.
4. **Cô lập tính năng:** không sửa logic/Transform/IK/pose/đèn của Slot 1 (Flashlight) và Slot 2 (Radar) hiện có. Thêm `ItemType` mới **chỉ ở cuối enum** (`Recorder, FilmCamera, Crowbar`) để không làm lệch giá trị đã serialize. Mọi thay đổi dùng chung phải kiểm tra hồi quy trên các slot khác.
5. **Tự kiểm tra thị giác** cho mọi việc liên quan UI, camera, ánh sáng, vị trí vật thể, cầm đồ, cutscene: `read_console` (0 lỗi) → Play → `MCPVisionCapture.Capture(...)` (đợi ≥1 frame) → Read ảnh → tự đánh giá → Stop.
6. **Không phá scene cũ:** không sửa `MapKinhDi_Ver1.unity`, `SampleScene.unity`, scene demo của asset pack. Muốn dùng lại thì **Save As** sang scene mới trong `Assets/_KhoaHon/Scenes/`.
7. **Git:** trước khi làm, báo tôi về 2 file đang thay đổi chưa commit và **hỏi** có commit chúng không — không tự ý commit/stash/discard. Sau đó tạo nhánh `feature/khoa-hon-v2`. Commit nhỏ, theo từng hệ thống, message tiếng Anh dạng `feat(khoahon): ...`. Không push, không merge.
8. **Không sửa file của asset pack bên thứ ba** (Flooded_Grounds, Abandoned_Asylum, ZNS3D, civilian_girl, FpsHorrorKit, TextMesh Pro, Plugins). Cần biến thể → tạo Prefab Variant/material mới trong `Assets/_KhoaHon/`.
9. **Tiếng Việt hiển thị đúng dấu:** kiểm tra font TMP hiện tại có hiển thị được "ạ ặ ầ ẫ ữ ợ ỹ đ Đ" không. Nếu không, tạo TMP Font Asset (Dynamic) từ một font giấy phép **OFL** hỗ trợ tiếng Việt (ví dụ Be Vietnam Pro, Noto Sans, Noto Serif — tải từ Google Fonts nếu máy có mạng; không có mạng thì hỏi tôi). **Không** nhúng font hệ thống Windows (Arial, Times…) vì không được phép phân phối. Dùng 1 font serif cổ (tài liệu, bùa, bảng đồng) và 1 font sans (UI).
10. **Không bịa thêm cốt truyện** vượt ra ngoài GDD. Mọi nội dung mới phải qua kiểm tra **4 Luật** (Ngũ Hành, Thu Hồn, Giữ Mồi, Lòng Tham). Không viết câu tự khẳng định kiểu "không thể phản bác".
11. **Asset thiếu → dùng placeholder** (primitive/ProBuilder-style mesh, X Bot, material tối màu) và ghi vào `Docs/KhoaHon/ASSETS_NEEDED.md` (tên, mô tả, nơi dùng, mức ưu tiên). Không chặn tiến độ vì thiếu asset đẹp.
12. **Hiệu năng:** mục tiêu ≥60 FPS ở 1080p trên PC tầm trung. Ánh sáng tối là chủ đạo: hạn chế realtime light đổ bóng (đèn pin + tối đa vài đèn có shadow trong tầm nhìn), dùng Light Probes/Reflection Probes, occlusion culling nếu cần.

---

## 3. QUY ƯỚC THƯ MỤC & CODE

```
Assets/_KhoaHon/
  Scenes/        KH_Main.unity, KH_End_Asylum.unity, KH_Menu.unity (Phase 5)
  Scripts/
    Core/        GameManager, StoryFlags, SaveSystem, CheckpointManager, GameEvents
    Interaction/ IInteractable, PlayerInteractor, InteractPrompt, các Interactable cụ thể
    Items/       RecorderItem, FilmCameraItem, CrowbarItem, BatteryPool, RelicInventory
    Notebook/    NotebookManager, NotebookUI, NotebookEntry
    Puzzles/     SafeKeypad, LotusAltar, CellarChains, MirrorNoReflection…
    AI/          HousekeeperAI, HousekeeperConfig, HidingSpot, BreathHold, NoiseSystem
    Sanity/      SanitySystem, SanityEffects, FakeAudioEvents
    Narrative/   DialogueRunner, SubtitleUI, ChoiceUI, EvpWhisperZone, TapePlayer
    Sequences/   GateLoopSequence, EndingDirector, các Timeline signal receiver
    Debug/       KhoaHonCheatPanel (chỉ Editor/Development build)
  Data/          (ScriptableObject) Documents/, Tapes/, Talismans/, Relics/, EvpLines/, Dialogues/, Configs/
  Prefabs/       Interactables/, Characters/, Props/, UI/
  Materials/  Audio/  Timelines/  Fonts/  Textures/
Assets/_KhoaHon/Tests/EditMode/   (asmdef riêng)
Docs/KhoaHon/    GDD_KhoaHon_v2.md, PROMPT (file này), PLAN.md, ASSETS_NEEDED.md, CHANGELOG.md
```
- Namespace `KhoaHon.<Folder>`. Tạo `KhoaHon.Runtime.asmdef` (tham chiếu Assembly-CSharp không được, nên: hoặc để mọi thứ trong Assembly-CSharp không asmdef, hoặc tách logic thuần C# vào asmdef riêng để test). **Chọn cách ít rủi ro:** code gameplay để trong Assembly-CSharp (không asmdef) vì cần gọi `PlayerController`/`HorrorGame.Inventory`; logic thuần (StoryFlags, kiểm tra đài sen, mã két, FSM transition table) viết thành class C# thuần để EditMode test gọi được qua asmdef test tham chiếu Assembly-CSharp (`"overrideReferences"` hoặc test trong Assembly-CSharp-Editor) — xác nhận cách nào chạy được rồi ghi vào CLAUDE.md.
- Comment và tooltip bằng **tiếng Việt** (đồng bộ với code hiện có), tên biến/class tiếng Anh.
- Mọi text hiển thị cho người chơi đặt trong ScriptableObject/`Data`, **không hardcode** trong script.

---

## 4. KIẾN TRÚC LÕI

### 4.1. Trạng thái cốt truyện
- `StoryFlags` (enum `StoryFlag` + `HashSet`): `GateLoop1/2/3`, `EnteredHouse`, `GotQuanBackpack`, `HasCrowbar`, `SafeOpened`, `HasUpstairsKey`, `Floor2Entered`, `Relic_Moc/Thuy/Tho/Kim/Hoa`, `Talisman_<Hanh>_Placed`, `CellarOpened`, `FoundQuan`, `HasWhistle`, `MetDaiQuan`, `Ending1/2/Secret`.
- `GameStage` enum: `Stage0_Gate`, `Stage1_GroundFloor`, `Stage2_Floor2`, `Stage3_Cellar`, `Stage4_Ritual`, `Ending`.
- `GameEvents` (static C# events): `FlagSet`, `StageChanged`, `NoiseEmitted(Vector3 pos, float radius, NoiseType)`, `PhotoTaken(PhotoResult)`, `ItemCollected`, `PlayerCaught`, `SanityTierChanged`.

### 4.2. Lưu & checkpoint
- `SaveSystem` lưu JSON vào `Application.persistentDataPath/khoahon_save.json`: flags, stage, item/kỷ vật/bùa đã có, ảnh đã chụp (lưu id manh mối, không lưu pixel), số phim/pin, checkpoint id, sanity.
- Checkpoint: `CP_Gate`, `CP_Hall`, `CP_Floor2Landing`, `CP_Shrine` (sau khi đặt bùa đầu tiên), `CP_CellarStairs`, `CP_RitualHall`.
- **Bị bắt:** jumpscare → `DeathScreenManager` → hồi sinh tại checkpoint gần nhất, **giữ nguyên** đồ đã nhặt, reset vị trí bà Quản Gia về patrol xa người chơi, đặt sanity ≥ 50.

### 4.3. Config (ScriptableObject, chỉnh không cần sửa code)
`HousekeeperConfig`, `SanityConfig`, `ToolsConfig` (pin, phim, cooldown), `AudioMixConfig`. Giá trị khởi tạo lấy từ bảng ở mục 5.

### 4.4. Tương tác
- `PlayerInteractor` gắn vào Player: raycast từ camera 2.2 m, phím **E**, hiện prompt tiếng Việt giữa-dưới màn hình (TMP). Interface `IInteractable { string Prompt {get;} bool CanInteract(); void Interact(); }`.
- Port từ FpsHorrorKit (viết lại sạch, đổi sang `PlayerController`): `DoorInteractable` (khóa/chìa, cót két), `HidingSpot` (từ `HidingSystem`), `ReadableDocument` (từ `LetterUIManager`, có nút đóng, chữ hiện dần, tự ghi vào Sổ tay), `Pickup`, `InspectableObject` (xoay xem kỷ vật — tùy chọn).
- Khi UI mở (Sổ tay, đọc tài liệu, keypad, đối thoại): `PlayerController.SetControlEnabled(false)`, mở chuột; đóng thì khôi phục.

### 4.5. Âm thanh
- Tạo `AudioMixer` `KH_Mixer` với group: Master/Music/SFX/Ambience/Voice/EVP. Snapshot `Normal`, `Hunt` (nhạc dồn dập), `Hidden` (lowpass, nhịp tim to), `EVP` (tăng SFX của bà Quản Gia + tiếng rè), `LowSanity`.
- Âm thanh 3D của bà Quản Gia: `spatialBlend = 1`, logarithmic, min 2 / max 25 (giống `GhostAI`).

---

## 5. ĐẶC TẢ HỆ THỐNG GAMEPLAY

### 5.1. Hạ Vy (Player)
- Dùng `Player` + `PlayerController` + tay `CivilianGirlHands` có sẵn. Không đổi tốc độ walk 3.5 / run 6.5.
- Thêm `PlayerNoiseEmitter`: chạy (Shift) phát noise bán kính **8 m** mỗi 0.5 s; đi bộ **2 m**; làm đổ đồ (Rigidbody prop va chạm > 2 m/s) **12 m**; nhập sai mã két 3 lần **10 m**.
- Phím: `E` tương tác · `Tab`/`J` Sổ tay · `1–4` hotbar · chuột trái dùng đồ đang cầm · giữ `Space` nín thở khi đang trốn · `Esc` pause.

### 5.2. Hotbar (4 ô) — ánh xạ mới cho chiến dịch Khóa Hồn
| Ô | Vật phẩm | Khi nào có | Ghi chú |
|---|---|---|---|
| 1 | **Đèn pin** (`Item_Flashlight` sẵn có) | Từ đầu | Chỉ **thêm** tiêu hao pin qua `BatteryPool` bằng component riêng/hook, không đụng IK/pose/light. |
| 2 | **Máy ghi âm cassette (EVP)** — `ItemType.Recorder`, script `RecorderItem : HeldItem` | Từ đầu (nằm trên ghế phụ xe) | Mượn **pose `Pose_HoldDevice`** của Radar bằng cách tạo `ItemData` mới; tạo model đơn giản (hộp + 2 cuộn băng + đèn LED). Radar **gỡ khỏi hotbar** của chiến dịch này (giữ nguyên asset/code). |
| 3 | **Máy ảnh film có flash** — `ItemType.FilmCamera`, `FilmCameraItem : HeldItem` | Từ đầu | Model placeholder (hộp + ống kính). |
| 4 | **Xà beng** — `ItemType.Crowbar`, `CrowbarItem : HeldItem` | Nhặt từ balo của Quân | Không dùng để đánh; chỉ để nạy cửa sổ sảnh (kết ẩn) và có thể nạy 1–2 thùng gỗ chứa pin/phim. |

### 5.3. Pin & phim (`ToolsConfig`)
- `BatteryPool` dùng chung cho đèn pin và máy ghi âm: đầy 100%. Đèn pin bật: −1% mỗi 3 s (≈300 s). Máy ghi âm đang nghe: −1% mỗi 2 s. Pin < 15%: đèn chập chờn. 0%: tắt, chỉ bật được khi nhặt pin. Pickup "Pin tiểu" +50%. Rải 8–10 pin trong nhà.
- Phim: bắt đầu **8 kiểu**, mỗi cuộn phim nhặt thêm +6, rải 4 cuộn. Cooldown flash 1.5 s. Phim hết vẫn ngắm được nhưng không chụp.

### 5.4. Máy ghi âm EVP
- Chuột trái: bật/tắt chế độ nghe. Khi bật: snapshot `EVP`, loop tiếng rè nhẹ, tiếng chùm chìa khóa của bà Quản Gia to và rõ hướng hơn (trái/phải theo panning 3D, xa/gần theo âm lượng). **Không có UI chỉ hướng** — đúng GDD.
- **Lời thì thầm EVP** (`EvpLine` ScriptableObject: text, clip, điều kiện, `isLie`): phát qua `EvpWhisperZone` (trigger ở cửa phòng/cầu thang) và theo trạng thái AI (bà đang ở cầu thang → "Bà ta đang lên cầu thang…"). Mỗi line có cooldown, không lặp quá 2 lần.
- **Sanity < 30:** 40% lời thì thầm là **lời nói dối** (chọn line `isLie` hoặc đảo nghĩa: báo bà ở hướng ngược lại, "phòng này an toàn" khi bà đang trong phòng).
- Phát **băng của Quân**: từ tab Băng ghi âm trong Sổ tay, chỉ khi đang có máy ghi âm; phát có phụ đề; trong lúc phát vẫn điều khiển được nhân vật.

### 5.5. Máy ảnh flash & chữ ẩn
- Tạo layer mới **`HiddenInk`**. Camera chính **loại** layer này khỏi culling mask → mắt thường không thấy.
- Chụp (chuột trái): nháy flash (spot light trắng cường độ cao 0.08 s + post-exposure), tiếng màn trập, render một camera phụ (cùng vị trí/FOV, culling mask = mặc định + `HiddenInk`) vào RenderTexture → `Texture2D` → hiển thị ảnh vừa chụp 1.5 s ở góc màn hình (khung ảnh film, viền trắng) → lưu vào Sổ tay tab **Ảnh**.
- `PhotoClue` component đặt trên vật thể ẩn: id, mô tả, mốc năm. Ảnh "bắt được" manh mối khi clue nằm trong frustum, ≤ 8 m, không bị che (raycast) → tạo `NotebookEntry` có chú thích, và set flag nếu có.
- **Flash vào mặt bà Quản Gia** (bà trong tầm ≤ 10 m, góc ≤ 20° so với tâm khung ngắm, LOS thông): bà **choáng 3 s** rồi **chuyển thẳng sang Hunt** (mục 5.8).

### 5.6. Sổ tay điều tra (Tab / J)
- UI dạng sổ giấy cũ, 5 tab: **Tài liệu · Băng ghi âm · Ảnh · Bùa chú · Kỷ vật**. Bản đầu là **danh sách**, sắp xếp theo mốc Biên niên sử: 1921, 1924, 1953, 1978, 1996, 2009, Hiện tại. (Sơ đồ mạng nhện: KHÔNG làm, để bản sau.)
- Tự động thêm mục khi đọc tài liệu, nhặt băng, chụp được manh mối, nhặt trang bùa (Vy "tự chép" bài chú vào sổ), nhặt kỷ vật.
- Mở Sổ tay **không dừng game** (bà Quản Gia vẫn đi) — để tăng căng thẳng; có bool trong config để đổi.
- Badge "mới" cho mục chưa xem.

### 5.7. Kỷ vật & trang bùa
- `RelicInventory`: 5 ô cố định (Mộc/Thủy/Thổ/Kim/Hỏa), **không vứt được**, hiển thị trong tab Kỷ vật. Nhặt kỷ vật = nhận luôn trang bùa của nó (`TalismanData`: hành, bài chú, ký hiệu).
- Trang bùa đặt lên đài sen sẽ **rời khỏi** tab Bùa chú (nhưng bài chú đã chép vẫn còn trong sổ).

### 5.8. AI săn đuổi — "Chấp Pháp Quản Gia" (`HousekeeperAI`, script MỚI)
Viết mới, có thể kế thừa/copy có chọn lọc từ `GhostAI` (LOS, jumpscare, âm thanh 3D). FSM:

| Trạng thái | Vào khi | Hành vi | Ra khi |
|---|---|---|---|
| **Patrol** | Mặc định | Đi chậm giữa **waypoint cố định** tầng 2 (route có thứ tự + chờ 2–4 s ở mỗi điểm, đôi khi vào phòng). Loop tiếng **chùm chìa khóa kéo lê** (3D) + lẩm bẩm khàn. Thỉnh thoảng hát ru. | Nghe noise → Suspicious; thấy Vy → Hunt |
| **Suspicious** | Nghe noise trong bán kính; **đèn pin chiếu thẳng vào mặt bà** (≤ 12 m, cone đèn chứa đầu bà, LOS thông); thấy thoáng qua ở rìa tầm nhìn | Đi nhanh tới vị trí noise/lần thấy cuối, rà soát 8–10 s (nhìn quanh, ghé 1–2 điểm trốn gần nhất nhưng **không mở** nếu không thấy Vy chui vào) | Thấy rõ Vy → Hunt; hết thời gian → Patrol |
| **Hunt** | Thấy Vy trong FOV + LOS; sau flash choáng | Lao theo Vy. Snapshot `Hunt`. Tốc độ = **1.15 × runSpeed** (bình thường) hoặc **1.00 × runSpeed** (sau khi Vy lấy lược). Nếu ở chế độ yếu: **mỗi lần phát hiện Vy, khựng 2 s**, thì thầm "Chạy đi…", rồi mới lao tới. | Mất LOS ≥ 4 s → Suspicious tại vị trí cuối; bắt được → Kill |
| **Stunned** | Flash (3 s) / Vy nhặt lược (20 s) | Đứng yên, gập người, che mặt (nếu không có animation thì rung + nghiêng mesh), âm thanh rú | Hết giờ → Hunt (flash) / Patrol (lược) |
| **Kill** | Khoảng cách ≤ 1.5 m trong Hunt | Mượn jumpscare của `GhostAI` (quay camera về mặt bà 0.4 s + hét) → `PlayerCaught` → checkpoint | — |

- **Phát hiện:** FOV **110°**, tầm nhìn **10 m** khi Vy ở chỗ tối, **16 m** nếu đèn pin Vy đang bật; check mỗi 0.15 s; nghe theo `NoiseEmitted`.
- **Tốc độ Patrol** 1.3 m/s, **Suspicious** 3.0 m/s. Đọc `runSpeed` từ `PlayerController` lúc Start (không hardcode 6.5).
- **Phạm vi:** bà chỉ hoạt động ở **tầng 2 + cầu thang**. Dùng NavMesh Area riêng (`Floor2`) hoặc NavMeshModifierVolume; nếu Vy chạy xuống tầng trệt trong lúc Hunt, bà dừng ở chiếu nghỉ, chuyển Suspicious rồi về Patrol. *(Quyết định thiết kế bổ sung để tầng trệt là vùng an toàn tương đối — ghi rõ trong PLAN.md, tôi có thể đổi.)*
- **Trốn:** `HidingSpot` cho **tủ áo** (`Prop_Cabinet_A/B`, `Cupboard`) và **gầm giường** (`Prop_Bed_A/B`, `Bed`): vào/ra bằng E, camera nội suy vào `hidingPoint`, nhìn qua khe (vignette + giới hạn góc quay). Đang trốn và bà **không thấy lúc chui vào** → bà không phát hiện được. Nếu bà **thấy lúc chui vào**, bà đi tới và đứng sát chỗ trốn → mini-game **nín thở**: giữ `Space`, thanh hơi (UI tối giản, mờ) cạn trong **7 s**; thả sớm hoặc cạn hơi → tiếng thở hắt → bị lôi ra (Kill). Giữ đủ đến khi bà bỏ đi (5–6 s) → thoát. Snapshot `Hidden`, nhịp tim to dần theo khoảng cách bà.
- **Chiếc lược (Phòng Gia Nhân):** khi Vy nhặt, ở bất kỳ đâu bà đang đứng: rú lên, Stunned 20 s, giọng nói "Chạy đi… ta không kiềm chế được…", sau đó bật cờ `weakened = true` vĩnh viễn.
- **Tiết kiệm animation:** dùng âm thanh thay biểu cảm. Nếu mesh ghost không có rig: trôi bằng `GhostFloat`, nghiêng thân theo hướng đi, áo đổi sang material **áo bà ba đen**; thêm prop **chùm chìa khóa** đung đưa ở hông (mesh đơn giản).
- Gizmos: vẽ FOV, tầm nghe, waypoint, vị trí noise cuối.

### 5.9. Hệ thống lý trí ẩn (Sanity, `SanityConfig`) — không có thanh UI
- Giá trị 0–100, bắt đầu 100.
- Giảm: đứng trong bóng tối (đèn pin tắt + không có nguồn sáng trong 6 m) quá 10 s → −1/s; nhìn thẳng hồn ma/bà Quản Gia trong 8 m → −4/s; sự kiện hù (jumpscare script, gặp thi thể Quân) −10 đến −20.
- Hồi: đèn pin bật hoặc đứng gần nguồn sáng +0.5/s; nghe băng của Quân +5 (một lần mỗi băng).
- Ngưỡng hiệu ứng (lerp mượt, qua Volume weight trên `HorrorGlobalVolumeProfile` hoặc Volume riêng):
  - **< 70:** nhịp tim (`PlayerHeartbeat`) rõ dần trong tai nghe.
  - **< 50:** Chromatic Aberration, Film Grain tăng; thỉnh thoảng **tiếng bước chân giả sau lưng** (spawn AudioSource sau Vy 3–5 m, dùng `wood_step`).
  - **< 30:** Motion Blur, Lens Distortion nhẹ; **cửa tự đập rầm** (chọn 1 cửa trong 10 m); EVP bắt đầu **nói dối**.
- Sanity không gây chết. Không bao giờ dưới 5.

### 5.10. Ánh sáng
- Nhà tối đặc. Nguồn sáng chính: đèn pin + flash. Chỉ vài nguồn yếu cố định (ánh chớp qua cửa sổ, nến Phòng Thờ, ánh đỏ vạc máu dưới hầm). Ambient gần như 0, fog nhẹ trong nhà.
- Lên tầng 2: kích hoạt nhóm `PeriodicFlickerLight`, giảm nhiệt độ màu (post-processing White Balance lạnh), hơi thở thành khói (particle nhỏ từ camera — tùy chọn).
- Bên ngoài: mưa đêm (particle mưa + âm thanh mưa loop + chớp sáng ngẫu nhiên kèm sấm), sương mù dày cho đoạn cổng/đường.

---

## 6. LEVEL & NỘI DUNG TỪNG GIAI ĐOẠN

### 6.0. Bố cục
- Scene chính `KH_Main.unity`. Ở Phase 0, chụp ảnh MapKinhDi_Ver1 qua MCP và quyết định: (a) **Save As** MapKinhDi_Ver1 thành `KH_Main` rồi tận dụng terrain/rào/cây và khối nhà Villa hiện có làm vỏ ngoài **Biệt thự Thanh Vân**; hoặc (b) tạo scene mới, dựng đường + cổng + biệt thự từ modular `Villa1/Villa2` + tường `ZNS3D`. Nêu lý do chọn trong PLAN.md.
- Biệt thự 2 tầng + hầm. **10 phòng**:
  - Tầng trệt (4): **Sảnh Chính** (cầu thang lớn, cửa chính then cài cổ, cửa sổ sảnh nạy được), **Nhà Bếp** (bàn tiệc 1978 + nắp hầm sắt 5 xích), **Phòng Khách** (ảnh gia tộc + bảng đồng), **Phòng Làm Việc** (két sắt).
  - Tầng 2 (6): **Phòng Thờ Gia Tộc**, **Phòng Trẻ Em** (Mộc), **Phòng Khách Trọ** (Thủy), **Phòng Gia Nhân** (Thổ), **Thư Viện Cổ** (Kim), **Phòng Ngủ Chủ Nhân** (Hỏa). Hành lang vòng để bà Quản Gia tuần tra và người chơi vòng tránh; mỗi phòng có ≥1 chỗ trốn; ít nhất 2 phòng có 2 cửa.
  - Hầm (2): **Xà lim ngầm**, **Sảnh Tế Khóa Hồn** (phòng bát giác), nối bằng cầu thang đá dốc ~10 m và hành lang vách lộ xương.
- Greybox trước bằng khối đơn giản, có NavMesh, chơi thử được, rồi mới thay prop. Tổng thời lượng mục tiêu: **45–75 phút** cho lần chơi đầu.
- Đặt nhãn Gizmo/tên GameObject rõ ràng: `Room_SanhChinh`, `Room_NhaBep`… để tôi tìm trong Hierarchy.

### 6.1. Giai đoạn 0 — Cổng & vòng lặp sương mù (`GateLoopSequence`)
- Mở đầu: màn đen, phụ đề mở màn (mục 8.1) → trong xe `Prop_Car_A`, camera ghế lái, xe **tự chạy** (DOTween `DOPath` theo waypoint) tới trước cổng sắt `Struct_Fence1_Gate_A` dưới mưa, nửa đêm. Máy ghi âm nằm trên ghế phụ.
- Tại cổng: lựa chọn **[Q] Quay đầu xe** / **[E] Xuống xe**.
  - Xuống xe bất cứ lúc nào → cổng kẽo kẹt tự hé, Vy đi bộ vào sân → `Stage1`.
  - Quay đầu lần **1**: xe chạy vào sương → bóng phụ nữ áo bà ba đen đứng giữa đường → phanh gấp, màn hình tối sầm → Vy tỉnh lại trong xe trước cổng.
  - Lần **2**: biển báo ven đường (`Struct_Billboard`/`Struct_Pole` + TMP) đổi thành **tên người mất tích** (danh sách ở 8.4) → máy ghi âm trên ghế phụ **tự bật**, tiếng khóc rấm rứt (giới thiệu EVP) → tối sầm → lại trước cổng.
  - Lần **3**: xe chết máy giữa đường, đổi model sang `Prop_Car1_DM` + khói từ nắp capo → màn hình tối → Vy đứng trước cổng; cổng sắt rỉ sét tự mở, gió lạnh thốc ra (particle lá + âm thanh). Bắt buộc bước vào.
- Đoạn lái **on-rails**, người chơi chỉ chọn quay đầu; mỗi vòng ~15–20 s.
- Sau khi vào sân, nếu Vy quay ra cổng: sương mù dày lên, phát lại một âm vang, fade, Vy lại đứng quay mặt vào nhà (**Luật Giữ Mồi**) — trừ trường hợp kết ẩn.

### 6.2. Giai đoạn 1 — Tầng trệt
- **Sảnh Chính:** cửa lớn sập khóa sau lưng (then cài cổ, không mở được). Chân cầu thang có **balo của Quân**: băng cassette **#1**, **thẻ phóng viên** (tài liệu), **xà beng** (vào ô 4). Gợi ý tự động: "Có thể nghe cuộn băng bằng máy ghi âm (Tab → Băng ghi âm)". Cửa lên tầng 2 khóa.
- **Nhà Bếp:** bàn tiệc 1978 bày nguyên (đĩa vỡ, ly đổ, rượu). Sàn có **ký hiệu Ngũ Hành bằng máu** (`PhotoClue` trên layer `HiddenInk`) — chụp được → mục "Khớp với ảnh Quân gửi". Nắp hầm sắt + **5 sợi xích** nối vào tường (không tương tác được, prompt "Bị khóa bằng 5 sợi xích"). Gần đó: **Di thư Lâm Gia Huy** (mục 8.3).
- **Phòng Khách:** ảnh gia tộc Trịnh khổ lớn + **bảng đồng** "Khởi công Thanh Vân — 1921".
- **Phòng Làm Việc:** két sắt — `SafeKeypad` UI 4 số (nút bấm cơ, tiếng lách cách), mã **1921**. Sai 3 lần → tiếng động lớn (noise 10 m) + sanity −5. Mở: **Sổ Khóa Hồn tập 1**, **bản thiết kế hầm**, **chìa khóa cửa cầu thang tầng 2**.
- Rải thêm: tài liệu mốc 1953, 2009, 1–2 pin, 1 cuộn phim.
- Mở cửa cầu thang → `Stage2`.

### 6.3. Giai đoạn 2 — Tầng 2: lãnh địa Quản Gia & trận đồ Ngũ Hành
- Bước lên: đèn chớp tắt, nhiệt độ giảm, tiếng hát ru vọng → cutscene ngắn (≤ 5 s, không mất điều khiển quá 3 s): bóng bà Quản Gia đi qua cuối hành lang → bắt đầu Patrol. Checkpoint `CP_Floor2Landing`.
- **Phòng Thờ Gia Tộc:** bàn thờ 13 bài vị (5 bài vị Mắt Trận khắc tên **hành** — Mộc/Thủy/Thổ/Kim/Hỏa — và 8 bài vị phu hồ không tên), trước đó là **5 đài sen** trống, mỗi đài nằm dưới 1 bài vị Mắt Trận. Nến leo lét.
  - `LotusAltar`: đứng trước đài, E → chọn trang bùa từ danh sách bùa đang có. Đúng hành → bùa dính vào, lóe sáng nhẹ, tiếng chuông. **Sai hành** → bùa bật ra trả về túi, lời thì thầm trách, sanity −5, noise 8 m (dụ bà Quản Gia tới — tạo áp lực, không phải hình phạt nặng).
  - Đặt được từng trang một, lưu tiến độ. Đủ 5 → cả nhà rung (camera shake + bụi rơi + tiếng gỗ rên), dưới bếp 5 xích đứt (destroy + particle + âm thanh), nắp hầm bật mở → `CellarOpened`, phụ đề "Có gì đó vừa vỡ ra dưới nhà bếp…".
- **5 phòng Mắt Trận** (mỗi phòng 1 kỷ vật + 1 trang bùa; khi nhặt phát 1 lời thì thầm/âm thanh riêng):
  | Phòng | Hành | Kỷ vật | Trang bùa giấu ở | Sự kiện khi nhặt |
  |---|---|---|---|---|
  | Trẻ Em | Mộc | Búp bê vải của bé An (trong **tủ áo**) | Khâu trong bụng búp bê | Thì thầm "Cháu lạnh quá…", cửa tủ tự đóng sầm |
  | Khách Trọ | Thủy | Huyết thư chưa gửi | Kẹp trong huyết thư | Tiếng nước nhỏ giọt, ly trên bàn rung |
  | Gia Nhân | Thổ | **Lược sừng gãy** của bà Quản Gia | Quấn quanh lược | Bà rú, Stunned 20 s, "Chạy đi… ta không kiềm chế được…", `weakened` |
  | Thư Viện Cổ | Kim | Búa đóng đinh đẫm máu của Thợ Cả | Quấn quanh cán búa | Sách rơi khỏi kệ (`book_1–9`), tiếng gõ búa vọng trong tường |
  | Ngủ Chủ Nhân | Hỏa | Đôi hài thêu của Nhị nương | Lót trong hài | Mùi khét (khói mờ), ánh lửa lóe trong gương; **bài chú trên trang này là bài chú khóa hồn** (dùng ở Kết 2) |
- Mỗi phòng có ít nhất 1 `PhotoClue` (huyết tự/dấu chân) dẫn về thứ tự hoặc vị trí giấu kỷ vật, cùng tài liệu mốc 1924/1978/1996 rải xen.
- Thứ tự thu thập tự do. Gợi ý độ khó: lược ở phòng xa nhất để người chơi phải sống sót với bà ở tốc độ 115% trước.

### 6.4. Giai đoạn 3 — Mê đạo hầm ngầm
- Không có AI săn. Căng thẳng bằng âm thanh, sanity, vài jumpscare có kịch bản (tối đa 2).
- **Lối Quân đã vào:** ở chân cầu thang đá có một đường hầm khoan hẹp (dấu vết đội thợ 2009) đã **sập kín bằng đá** — giải thích vì sao Quân xuống được hầm dù 5 xích vẫn còn nguyên, và vì sao cậu bị kẹt, chết vì mất máu. *(Bổ sung để vá lỗ hổng trong GDD — xem mục 12.)*
- Cầu thang đá dốc ~10 m, vách trát vữa lộ xương 8 phu hồ (mesh xương placeholder/decal). Checkpoint `CP_CellarStairs`. Băng **#3** của Quân rơi trên bậc thang (mục 8.2).
- **Xà lim ngầm:** thi thể Quân (X Bot nằm nghiêng, material tối, áo phóng viên — placeholder), dòng chữ khắc trên sàn (TMP 3D font serif, hiệu ứng khắc) — mục 8.5. Trong tay Quân: **chiếc còi nhựa kỷ niệm** → E: Vy cất vào túi áo (`HasWhistle`). Ngay sau đó máy ghi âm (dù đang tắt) tự bật: giọng Quân thì thầm "Chị…" — sanity −15.
- **Sảnh Tế Khóa Hồn:** phòng bát giác, bệ đá cổ giữa phòng, **Ấn Đồng** lơ lửng xoay chậm trên **vạc máu đen** (mặt chất lỏng đen bóng, ánh đỏ phát ra từ dưới). Ghế gỗ mun (`ChairLeather` + material gỗ đen) — **bóng ma Trịnh Đại Quan** (X Bot/placeholder, áo the, material bán trong suốt khói đen) hiện ra ngồi trên ghế khi Vy vào giữa phòng → đối thoại (mục 7). Checkpoint `CP_RitualHall`.

---

## 7. KẾT THÚC

### 7.1. Đối thoại với Trịnh Đại Quan (`DialogueRunner` + `ChoiceUI`)
- Phụ đề + lựa chọn. Vy đứng yên, camera bị hút nhẹ về phía lão (có thể quay nhìn xung quanh).
- 3 lượt cám dỗ (nội dung ở mục 8.6). Mỗi lượt có lựa chọn: **"Nghe tiếp"** / **"Từ chối"**. Ở lượt 3 (hứa trả Quân về): **"Nhận Ấn"** / **"Từ chối"**.
- Chọn "Từ chối" ở bất kỳ lượt nào → lão nổi giận, các xích linh hồn trong vách rung lên, không gian méo (sanity effect), → **chuỗi Kết 1** mở.
- Nếu đang cầm máy ghi âm bật trong lúc đối thoại: bắt được giọng Quân "Chị… đừng tin lão…" (line thật, `isLie = false`, không bị sanity làm sai lệch — đây là manh mối chủ chốt).

### 7.2. KẾT 1 — GIẢI THOÁT (Good/True)
- Điều kiện: Từ chối. Bắt buộc theo thứ tự GDD v2: **đập Ấn Đồng bằng búa của Thợ Cả TRƯỚC**, rồi **thiêu 5 kỷ vật** (gồm cả búa) vào vạc.
- Gameplay: E vào Ấn → animation tay cầm búa (dùng HandRig/pose đơn giản hoặc chỉ camera + âm thanh) → 3 nhát (bấm chuột trái 3 lần, mỗi nhát Ấn nứt thêm, cả phòng rung). Ấn vỡ → E vào vạc 5 lần, mỗi lần ném 1 kỷ vật (UI chọn từ tab Kỷ vật; búa chỉ ném được sau cùng).
- Cutscene (Timeline + Cinemachine):
  1. 13 sợi xích linh hồn đứt vụn (particle sáng), Đại Quan gào thét rồi tan thành tro.
  2. Bà Quản Gia hiện ra với gương mặt hiền từ (material sáng, không còn áo đen u ám), mỉm cười cúi đầu cảm tạ.
  3. Hồn Quân bước đến ôm Vy từ biệt rồi hóa thành đốm sáng.
  4. Biệt thự sập đổ: rung mạnh, bụi, đá rơi — Vy chạy (on-rails ngắn hoặc người chơi tự chạy theo đường được chặn sẵn) lên bếp → sảnh → cổng, đúng lúc **bình minh** (đổi skybox/màu ánh sáng).
  5. Cảnh cuối: Vy ngoài cổng; máy ảnh và máy ghi âm hỏng hoàn toàn; trong túi áo chỉ còn **chiếc còi của Quân** (cận cảnh tay lấy còi ra). Phụ đề kết (8.7).

### 7.3. KẾT 2 — TÂN CHỦ NHÂN (Bad/Loop)
- Điều kiện: "Nhận Ấn" ở lượt 3.
- Gameplay: E vào Ấn → nhỏ máu (âm thanh + màn hình nhuốm đỏ) → Sổ tay mở trang **bài chú khóa hồn** (trang bùa Hỏa) → người chơi chọn lần lượt 4 câu chú theo **thứ tự ngược** (từ câu cuối lên câu đầu). Sai thứ tự: chữ nhòe, chọn lại (không phạt).
- Cutscene:
  1. 13 oan hồn ngừng rên xiết, quỳ phục dưới chân Vy. Đại Quan bị phản phệ, thành nô lệ dưới chân Ấn.
  2. Hồn Quân xuất hiện — **không sống lại**, mắt vô hồn, quỳ cùng các oan hồn (đúng Luật Thu Hồn).
  3. Vy đứng trước gương (`MirrorShelf`): gương **không phản chiếu** bóng cô (dùng planar reflection/RenderTexture camera loại layer Player-body ra).
  4. "Vài tháng sau…": góc nhìn một phóng viên trẻ đứng trước cổng ban ngày âm u, nhìn lên cửa sổ tầng 2 — Vy (civilian_girl, material mắt đỏ đục) đứng sau kính, mỉm cười: "Vào đi… nhà đang đợi em."

### 7.4. KẾT ẨN — KẺ HÈN NHÁT (Secret)
- Điều kiện: đã nhặt balo Quân (có xà beng) và **chưa mở hầm** (`CellarOpened == false`; giới hạn này là quyết định bổ sung — xem mục 12). Tại **cửa sổ sảnh**: prompt "Nạy cửa sổ (Xà beng)". Hiện hộp xác nhận: "Bỏ lại Quân và chạy trốn?" [Ở lại] / [Chạy].
- Chọn Chạy: nạy cửa (âm thanh gỗ nứt) → Vy trèo ra ngoài → chạy bộ trong mưa ra cổng → **lần này sương mù không chặn lại** (Luật Giữ Mồi: nhà thả Vy vì đã có oan hồn bám theo) → chạy dọc đường tới đường lộ (2 giờ sáng) → đèn pha xe tải (placeholder `Prop_Car_A` phóng to/đổi màu, ghi ASSETS_NEEDED) dừng lại.
- Cutscene: trong cabin xe tải, cận cảnh **gương chiếu hậu** — một bóng phụ nữ mặc áo bà ba đen ngồi bất động ở băng ghế sau (dùng lại visual bà Quản Gia). Cắt cảnh.
- Cảnh cuối trong `KH_End_Asylum.unity` (Save As từ `Abandoned_Asylum/Show.unity` hoặc dựng phòng nhỏ từ prefab Asylum): phòng bệnh trắng bệch, Vy ngồi quay lưng, dùng móng tay cào lên tường hình **chiếc Ấn Đồng** (decal vết cào lộ dần). Phụ đề kết (8.7).

### 7.5. Sau kết thúc
- Màn hình tên kết thúc + "Kết đã mở: x/3", lưu vào `PlayerPrefs` để menu hiển thị. Quay về menu.

---

## 8. NỘI DUNG VĂN BẢN (dùng nguyên văn, đặt vào ScriptableObject; cho phép sửa câu chữ nhỏ cho tự nhiên nhưng giữ ý)

### 8.1. Mở màn (phụ đề trên màn đen)
> "Quân mất tích ở biệt thự Thanh Vân đã mười bốn ngày.
> Đêm cuối cùng, nó gửi cho tôi một tấm ảnh chụp sàn nhà bếp. Cảnh sát nói đó chỉ là vết bẩn.
> Tôi không tin."

### 8.2. Băng của Quân (phụ đề; lồng tiếng để trống clip, ghi ASSETS_NEEDED)
- **Băng #1 (balo, Sảnh):** "Ngày thứ… ba? Chị Vy, nếu chị nghe được cái này thì em xin lỗi vì không nghe máy. Em tìm ra rồi — lối xuống hầm nằm dưới nhà bếp, ngay dưới cái bàn tiệc. Có năm sợi xích, không có ổ khóa nào cả… (tiếng kim loại leng keng xa xa) …Chị nghe thấy không? Tiếng chìa khóa. Và tiếng bước chân, một bên nặng một bên nhẹ, như người đi khập khiễng. Nó ở trên lầu. Em tắt máy đây."
- **Băng #2 (Phòng Làm Việc, cạnh két):** "Két khóa số. Ông Lâm viết mã nằm ở năm khai sinh cơ nghiệp… Em thử năm xây xong, 1924 — không được. (tiếng chìa khóa leng keng trên trần) Bà ta ở ngay trên đầu em. Đêm qua em thấy bà ta đứng ở chiếu nghỉ cầu thang nhìn xuống, nhưng không bước xuống. Hình như bà ta không xuống được dưới này." *(Vừa gợi ý mã 1921, vừa dạy luật bà chỉ ở tầng 2.)*
- **Băng #3 (cầu thang hầm, cạnh đống đá sập):** (giọng yếu, thở gấp) "Em tìm được cái lỗ đội thợ khoan năm 2009, sau vách bếp… chui lọt xuống được thì vách sập. Chân em kẹt dưới đá… chảy máu nhiều quá. Có ai đó dưới này, ngồi trên ghế, nói chuyện rất nhẹ nhàng. Lão bảo lão có thể cho em mọi thứ. Chị ơi… nếu em không ra được — đừng nghe lão."

### 8.3. Tài liệu theo Biên niên sử
- **1921 — Bảng đồng (Phòng Khách):** "KHỞI CÔNG THANH VÂN — 1921. Gia chủ Trịnh Đại Quan."
- **1924 — Sổ Khóa Hồn, tập 1 (két sắt), trích:** "Ngũ Hành Khóa Hồn. Lấy năm người thân cận làm Mắt Trận, mỗi người giữ một hành: Mộc, Thủy, Thổ, Kim, Hỏa. Lấy tám phu hồ chôn sống trong vách làm mạch dẫn khí. Mười ba hồn khóa vào Ấn Đồng, kẻ giữ Ấn hưởng phúc lộc không cùng. Năm đài sen là năm cửa của trận. Trận mở thì cửa hầm mở." (Kèm sơ đồ 5 đài sen — ảnh/hình vẽ.)
- **1924 — Bản thiết kế hầm (két sắt):** bản vẽ tay: "Cầu thang đá — sâu năm trượng. Xà lim. Sảnh tế bát giác. Vạc đặt chính giữa." Chữ ký Thợ Cả bị gạch xóa bằng mực đỏ.
- **1953 — Mẩu báo cũ (Sảnh/Phòng Làm Việc):** "THẢM KỊCH Ở BIỆT THỰ THANH VÂN: toàn bộ gia tộc họ Trịnh được phát hiện tử vong trong phòng ngủ. Nhà chức trách chưa xác định nguyên nhân."
- **1978 — Thiệp tân gia gia đình Trần (Nhà Bếp, trên bàn tiệc):** "Trân trọng kính mời quý khách đến dự tiệc tân gia của gia đình chúng tôi tại biệt thự Thanh Vân." Mặt sau, nét chữ run: "Nửa cuốn sổ nói rằng ta sẽ có tất cả. Chỉ cần một bữa tối."
- **1996 — Di thư Lâm Gia Huy (Nhà Bếp):** "Mọi cơ quan của ngôi nhà đều điều khiển từ phòng làm việc và phòng thờ gia tộc. Mật mã két sắt nằm ở năm khai sinh cơ nghiệp này. Ta đã mở được két, nhưng không qua được bàn thờ. Người đàn bà cầm chùm chìa khóa đêm nào cũng đứng ở chân giường ta. Ta không chịu nổi nữa."
- **2009 — Biên bản niêm phong (Sảnh, dán trên cửa):** "Biên bản niêm phong hiện trường. Sáu công nhân đội cải tạo mất tích sau khi thi công khoan tường tầng hầm. Cấm mọi hành vi xâm nhập."
- **Hiện tại — Thẻ phóng viên của Quân:** "THẺ PHÓNG VIÊN — Hạ Quân — Phóng viên ảnh." (GDD chưa ghi tên đệm/tên tòa soạn → để nguyên, ghi TODO trong ASSETS_NEEDED.)
- **Hiện tại — Ảnh Quân gửi** (có sẵn trong Sổ tay từ đầu): ảnh flash sàn bếp có ký hiệu Ngũ Hành.
- **Huyết thư (Thủy, Phòng Khách Trọ):** "Ai đọc được thư này xin báo cho gia đình tôi ở phương xa: tôi không bỏ đi. Bữa tối nay có vị lạ. Chủ nhà mời rượu mãi không thôi…" (chữ nhòe máu ở cuối).

### 8.4. Tên trên biển báo (vòng lặp 2)
"TRẦN VĂN LỘC · TRẦN THỊ HẠNH · LÂM GIA HUY · NGUYỄN VĂN TÚ (2009) · ĐỘI THỢ CẢI TẠO · HẠ QUÂN" — dòng cuối nhòe dần như đang được viết thêm.

### 8.5. Chữ Quân khắc trong xà lim
"Trịnh Đại Quan không bao giờ chết. Lão sống bằng chính lòng tham của kẻ kế tục. Đừng để lão gạt…"

### 8.6. Trịnh Đại Quan (giọng trầm, chậm, lịch thiệp)
- **Mở đầu:** "Mời cô ngồi. Đã lâu lắm rồi mới có người đi được tới tận đây. Lâm Gia Huy dừng lại ở bàn thờ. Cậu em cô… dừng lại ở xà lim."
- **Lượt 1 — Của cải:** "Ngôi nhà này đã nuôi họ Trịnh suốt ba mươi năm. Tiền bạc, ruộng đất, danh vọng — Ấn Đồng cho tất cả. Cô chỉ cần đưa tay ra."
- **Lượt 2 — Sự thật & danh tiếng (đánh vào nghề nhà báo):** "Cô là người đi tìm sự thật. Ta có thể kể cho cô mọi bí mật của mảnh đất này, mọi vụ án người ta đã chôn vùi. Cả nước sẽ phải đọc tên cô."
- **Lượt 3 — Quân:** "Còn cậu em… Linh hồn nó đang ở đây, trong Ấn. Cầm lấy Ấn, cô sẽ là chủ của mọi linh hồn trong ngôi nhà này. Cô muốn nó trở về, phải không? Ta trả nó lại cho cô."
- **Khi bị từ chối:** "Ngu xuẩn! Mười ba hồn này là của ta! Không có ta, chúng chẳng là gì cả!"
- **Khi nhận Ấn (Kết 2):** "Tốt… rất tốt. Nhỏ máu lên Ấn. Rồi đọc ngược bài chú cô đã chép."

### 8.7. Lời thoại / phụ đề khác
- **Bà Quản Gia:** "Chạy đi… ta không kiềm chế được…" · "Chạy đi…" · lẩm bẩm khàn (không rõ chữ) · hát ru **tự sáng tác** (không dùng lời bài hát có bản quyền), ví dụ: "Ầu ơ… ngủ đi con, đêm dài… cửa đã khóa… đèn đã tàn…"
- **EVP thật:** "Đừng vào phòng đó…" · "Bà ta đang lên cầu thang…" · "Bà ta ở ngay sau cánh cửa…" · "Trong tủ… có hơi ấm…" · "Cháu lạnh quá…" · "Chị…"
- **EVP nói dối (sanity thấp):** "Phòng này an toàn…" · "Bà ta đi xuống rồi…" · "Chạy đi, chạy ngay!" (khi đứng yên mới là an toàn) · "Ra khỏi tủ đi…"
- **Kết 1, phụ đề cuối:** "Không còn bức ảnh nào. Không còn cuộn băng nào. Chỉ còn tiếng còi của Quân, và một buổi sáng."
- **Kết 2:** "Vào đi… nhà đang đợi em."
- **Kết ẩn:** "Bệnh nhân không nói chuyện với ai. Mỗi đêm, cô ấy chỉ vẽ một hình tròn trên tường, và đòi tắt đèn."

---

## 9. PHASE & TIÊU CHÍ NGHIỆM THU

Mỗi Phase: viết code → test EditMode cho logic thuần → kiểm tra Console 0 lỗi → kiểm tra thị giác (mục 2.5) cho phần có hình → commit → cập nhật `Docs/KhoaHon/CHANGELOG.md` → báo cáo (mục 11) → **DỪNG chờ tôi**.

### Phase 0 — Khảo sát & kế hoạch (không viết gameplay)
- Đọc GEMINI.md, skill, GDD, prompt này. Liệt kê tool MCP Unity đang khả dụng và xác nhận kết nối (đọc Console, lấy hierarchy của MapKinhDi_Ver1, chụp 3–4 ảnh scene).
- Xác minh lại mục 1 (ghi chỗ nào khác với mô tả). Đọc kỹ `GhostAI`, `HotbarInventory`, `HeldItem`, `FlashlightItem`, `RadarItem`, `PlayerController`, `RespawnManager`, `DeathScreenManager`, và các script FpsHorrorKit cần port.
- Kiểm tra font tiếng Việt (mục 2.9).
- Hỏi tôi về 2 file chưa commit, rồi tạo nhánh.
- Tạo `CLAUDE.md`, `Docs/KhoaHon/PLAN.md` (quyết định scene a/b, sơ đồ mặt bằng dạng ASCII hoặc ảnh cho tầng trệt/tầng 2/hầm, danh sách script sẽ tạo, rủi ro), `ASSETS_NEEDED.md` khởi tạo.
- **Nghiệm thu:** PLAN.md đủ để tôi duyệt; chưa sửa scene/code gameplay nào.

### Phase 1 — Greybox & điều khiển (≈ "Tháng 1")
- `KH_Main` với greybox đủ 10 phòng + hầm 2 khu + sân/cổng/đoạn đường; cầu thang đi được; NavMesh tầng 2 (area riêng).
- Core: StoryFlags, GameEvents, Save/Checkpoint, PlayerInteractor + prompt, DoorInteractable (khóa/chìa), ReadableDocument UI, Pickup.
- Hotbar mới: đèn pin + BatteryPool, ô máy ghi âm/máy ảnh (tạm chỉ equip được, chưa chức năng), xà beng.
- Sổ tay dạng danh sách 5 tab + RelicInventory.
- Font tiếng Việt chạy đúng trên mọi UI.
- Cheat panel F10 (chỉ Editor/Dev): nhảy tới checkpoint, cấp đồ, set flag, set sanity, bật/tắt AI.
- **Nghiệm thu:** đi hết nhà từ cổng tới Sảnh Tế (cheat mở cửa) không kẹt; đọc 1 tài liệu → hiện trong Sổ tay đúng mốc năm; lưu/thoát/tải lại giữ nguyên trạng thái; ảnh chụp mỗi tầng cho thấy layout.

### Phase 2 — Giải đố & AI (≈ "Tháng 2")
- Két sắt 1921 + nội dung; 5 kỷ vật + 5 trang bùa + LotusAltar; xích hầm & nắp hầm.
- HousekeeperAI đầy đủ FSM, waypoint, noise, đèn pin chiếu mặt, chế độ yếu sau lược, Kill + checkpoint.
- HidingSpot (tủ/gầm giường) + nín thở.
- **Nghiệm thu:** chơi được từ Sảnh tới mở nắp hầm; bà Quản Gia: (1) không bao giờ xuống tầng trệt, (2) không kẹt NavMesh trong 10 phút để tự chạy (ghi log), (3) chạy thẳng không thoát được ở 115%, thoát được bằng cắt LOS + trốn, (4) sau lược tốc độ đúng 100% và có khựng 2 s. Test EditMode cho bảng chuyển trạng thái và LotusAltar.

### Phase 3 — Công cụ của Vy, âm thanh, sanity, jumpscare (≈ "Tháng 3")
- RecorderItem (EVP định hướng qua mixer, whisper zone, phát băng, nói dối khi sanity thấp).
- FilmCameraItem (flash, layer `HiddenInk`, chụp vào Sổ tay, choáng bà 3 s → Hunt, phim).
- SanitySystem + hiệu ứng ẩn theo ngưỡng; tiếng bước chân giả; cửa đập.
- Âm thanh: mưa/sấm ngoài trời, mixer snapshot, chùm chìa khóa, hát ru; sương mù cổng; ánh sáng động tầng 2.
- Giai đoạn 0 hoàn chỉnh: xe on-rails, 3 vòng lặp, cổng mở.
- **Nghiệm thu:** ảnh chụp sàn bếp trong game có ký hiệu, mắt thường không thấy (2 ảnh MCP đối chiếu); đeo tai nghe phân biệt được bà ở trái/phải; FPS ≥ 60 trong Profiler ở hành lang tầng 2.

### Phase 4 — Hầm, đối thoại, 3 kết (≈ đầu "Tháng 4")
- Xà lim, Sảnh Tế, Đại Quan, DialogueRunner/ChoiceUI, EndingDirector, 3 Timeline cutscene, `KH_End_Asylum`.
- **Nghiệm thu:** từ save ở `CP_RitualHall`, đạt được Kết 1 và Kết 2; từ save `CP_Hall` đạt Kết ẩn; không kết nào mâu thuẫn 4 Luật (viết checklist đối chiếu trong báo cáo).

### Phase 5 — Đánh bóng & playtest (≈ cuối "Tháng 4")
- Menu chính đơn giản (Chơi mới / Tiếp tục / Thoát + số kết đã mở), pause menu, chỉnh độ nhạy chuột & âm lượng.
- Thay greybox bằng prop từ các pack ở mục 1, tối ưu ánh sáng/occlusion, sửa kẹt địa hình (chạy `SnapToGroundTool` nếu hợp), cân chỉnh thời lượng truy đuổi.
- Thêm `KH_Menu`, `KH_Main`, `KH_End_Asylum` vào Build Settings (giữ các scene cũ nhưng không đặt làm scene đầu).
- Chạy một lần chơi đầy đủ bằng cheat nhanh + một lần chơi bình thường đoạn tầng 2 (ghi lại thời gian, số lần chết).
- **Nghiệm thu:** build Windows chạy được, không lỗi Console, cả 3 kết đều đạt được trong build.

---

## 10. NHỮNG ĐIỀU KHÔNG LÀM
- Không làm vai Minh Khoa/livestream, Night Vision, sơ đồ mạng nhện, lái xe tự do (để bản sau — Phần 7 GDD).
- Không sửa `GhostAI.cs`, `MapKinhDi_Ver1.unity`, IK/pose của Slot 1–2, file của asset pack bên thứ ba.
- Không đổi Asset Serialization Mode, render pipeline, phiên bản package, Input System backend nếu chưa hỏi tôi.
- Không xóa file/asset hiện có. Cái gì không dùng nữa thì để nguyên.
- Không push lên remote, không merge nhánh.
- Không đánh dấu hoàn thành khi chưa có bằng chứng (log Console, ảnh MCP, kết quả test).

---

## 11. MẪU BÁO CÁO CUỐI MỖI PHASE
```
## Phase N — <tên>
Đã làm: (gạch đầu dòng ngắn, kèm đường dẫn file/scene chính)
Bằng chứng: Console 0 lỗi (ghi giờ kiểm tra) · Test: x/y pass · Ảnh: Temp/....png (mô tả ngắn những gì thấy)
Quyết định thiết kế tự đưa ra: ...
Lệch so với GDD/prompt (nếu có) & lý do: ...
Asset placeholder mới thêm vào ASSETS_NEEDED.md: ...
Cần tôi quyết định: ...
Commit: <hash> <message>
```

## 12. ĐIỂM BỔ SUNG SO VỚI GDD (đã chốt mặc định — tôi có thể đổi)
1. **Quân vào hầm bằng cách nào?** GDD cho Quân chết trong xà lim nhưng hầm chỉ mở khi đặt đủ 5 bùa. Mặc định: Quân lọt xuống qua lỗ khoan của đội thợ 2009 (Biên niên sử 2009), vách sập sau lưng, kẹt chân và chết vì mất máu. Quân **chưa từng** mở được két hay lên tầng 2.
2. **Bà Quản Gia chỉ ở tầng 2 + cầu thang** (GDD chỉ nói bà xuất hiện khi lên tầng 2).
3. **Kết ẩn chỉ mở trước khi hầm mở** (GDD: "sau khi nhận balo", không nói đến khi nào).
4. **Có thể xuống xe ngay ở vòng 1** thay vì bắt buộc 3 vòng (GDD: "người chơi được chọn quay xe lại").
5. Tên người trên biển báo ở vòng lặp 2 (mục 8.4) và toàn bộ văn bản mục 8 ngoài các câu trích từ GDD là **nội dung viết mới** theo đúng 4 Luật.

Bắt đầu bằng **Phase 0**.
