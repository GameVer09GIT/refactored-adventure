# PLAN — Lời Nguyền Khóa Hồn (Phase 0)

Trạng thái: **chờ duyệt**. Nhánh `feature/khoa-hon-v2`. Ngày khảo sát: 2026-11-05.

---

## 1. Kết quả xác minh hiện trạng (so với PROMPT mục 1)

Kết nối: Unity MCP (HTTP 127.0.0.1:8080) hoạt động, Unity `6000.3.21f1`, Edit Mode, Console 0 lỗi. Có 2 cảnh báo cũ: "referenced script (Unknown) … is missing" và "Account API did not become accessible".

| Mục | Mô tả trong PROMPT | Thực tế | Ghi chú |
|---|---|---|---|
| Package | URP 17.3, Input System 1.20, AI Navigation 2.0.14, Cinemachine 3.1.7, Timeline 1.8.12, Animation Rigging 1.3.1, Test Framework 1.6, MCP for Unity | **Đúng** | Không có ProBuilder → placeholder dựng bằng primitive. |
| Serialization | Force Text | **ForceText** | — |
| Git | 2 file chưa commit | Đã commit lên `main` (`8def707 chore: save MapKinhDi WIP`) theo yêu cầu, rồi tạo nhánh. | Scene MapKinhDi đang mở trong Editor **vẫn có thay đổi chưa lưu**. Không lưu, không đụng tới. |
| Build Settings | chỉ `SampleScene` | **Đúng** | — |
| Layers | — | Chỉ có layer mặc định (0,1,2,4,5) | Sẽ thêm `HiddenInk`, `PlayerBody`, `Interactable` ở Phase 1/3. |
| Tags | — | Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController, Ghost | Thêm tag `Housekeeper`. |
| MapKinhDi_Ver1 | Player, Ghost, Canvas+RespawnButton, NavMesh_Manager, terrain, Villa, 10 flicker light, 278 FootstepSurface, 7 FootstepZone | **Đúng**: flicker=10, surfaces=278, zones=7. Có thêm `SpawnPoint` (RespawnManager), `DeathScreenManager`, `Door`, `EventSystem` (InputSystemUIInputModule). | `Player` **không phải prefab**. Components: CharacterController, PlayerController, PlayerFootsteps, PlayerBreathing, PlayerHeartbeat, HotbarInventory, PlayerArmIK, PlayerArmIKTuningUI, FlashlightDebugOverlay. Camera con tên `Camera`. Hotbar: 0=`Item_Flashlight`, 1=`Item_Radar`, 2–3 trống. |
| Villa trong MapKinhDi | khối nhà Villa1/Villa2 | 356 mảnh Villa, một khối nhà **3 tầng** (y 10→34), trải x 50→115, z 7→32 (~65 × 25 m). Cầu thang trong `Villa1_IntStairs_A` (103,11,15). Có cụm nội thất thứ hai quanh z −80…−100 với `Villa2_IntStairs_B` và đèn "Basement" (y −0.5). | Xem quyết định scene ở mục 2. |
| `PlayerController` | walk 3.5, run 6.5, `SetControlEnabled`, `SetPitch`, `IsRunning`, `HorizontalSpeed` | **Đúng**. `walkSpeed`/`runSpeed` là `public float`. Có thêm `ResetStamina()`, `LockCursor()`, `UnlockCursor()`. Đọc input trực tiếp từ `Keyboard.current`/`Mouse.current`. | Đọc `runSpeed` lúc Start cho AI. |
| `GhostAI` | FSM Patrol/Chase/Lost/Kill… | **Đúng** (683 dòng), singleton `Instance`, `OnPlayerCaught`, `RespawnGhost(pos, cam)`, jumpscare quay camera 0.4 s. | Không sửa. `HousekeeperAI` viết mới, mượn logic LOS và jumpscare. |
| `DeathScreenManager` | màn hình chết | Singleton, static event `OnPlayerDied`/`OnPlayerRespawned` (PlayerFootsteps/Breathing/Heartbeat đang nghe). `TriggerJumpscareSequence()` public. Tự `FindFirstObjectByType<GhostAI>()` khi respawn; hồi sinh về `spawnPoint`. | **Nối checkpoint mà không sửa file**: `CheckpointManager` dời Transform `spawnPoint` tới checkpoint gần nhất trước khi gọi `TriggerJumpscareSequence()`. Trong KH_Main không có `GhostAI` nên nhánh respawn ghost bị bỏ qua (null-safe). Bà Quản Gia reset qua `OnPlayerRespawned`. |
| `HotbarInventory` | 4 ô, `HeldItem.OnPrimaryUse()` | **Đúng**. `SelectSlot(int)`, `SetSlotData(int, ItemData)`, event `OnSelectionChanged`. | Gỡ Radar khỏi hotbar của chiến dịch bằng `SetSlotData` trên Player của KH_Main, không sửa code. |
| `ItemData.ItemType` | `{None, Flashlight, Radar, Generic}` | **Đúng** | Thêm `Recorder, FilmCamera, Crowbar` ở cuối. Đây là thay đổi duy nhất vào file inventory dùng chung. |
| `Pose_HoldDevice` | pose của Radar | `Assets/Items/Poses/Pose_HoldDevice.asset` (+ `Pose_GripCylinder`, `Pose_OpenRelaxed`) | `ItemData` mới nằm ở `Assets/_KhoaHon/Data/Items/`. |
| FpsHorrorKit | các script nêu tên | Có đủ (2443 dòng tổng), phụ thuộc `FpsController`/`PlayerInteract` của kit. | Port có chọn lọc: HidingSystem, DoorSystem, LetterUIManager, PhotoCaptureSystem, DialogueSystem, PlayerInteract. |
| Model / prefab | civilian_girl, CivilianGirlHands, HandRig, Ghost.prefab, X Bot, Prop_Car_A, Prop_Car1_DM, Struct_Fence1_Gate_A(_Door/_DM), Prop_Cabinet_A, MirrorShelf, Asylum Show.unity, ZNS3D prefabs, DOTween, HorrorGlobalVolumeProfile | **Đều có** | `Show.unity` nằm ở `Assets/Abandoned_Asylum/Show.unity`. |
| AudioMixer | — | **Chưa có** `.mixer` nào | Tạo `KH_Mixer` ở Phase 3. |
| Font | TMP mặc định | Chỉ có `LiberationSans SDF`, **thiếu 15 glyph** tiếng Việt trong chuỗi thử | Đã thêm Be Vietnam Pro + Noto Serif (mục 6). |
| asmdef | — | Project **không có asmdef** nào (ngoài package) | Xem mục 5. |

---

## 2. Quyết định scene: **(b) tạo scene mới `KH_Main`**

Lý do:
1. Khối Villa trong MapKinhDi là vỏ **3 tầng, ~65 × 25 m**, lớn gấp nhiều lần nhu cầu (2 tầng + hầm, 10 phòng). Nội thất không theo bố cục GDD; phải gỡ phần lớn để dựng lại.
2. MapKinhDi đang có thay đổi chưa lưu trong Editor. Save As sẽ chép luôn các thay đổi đó, và việc đóng/mở scene dễ làm người dùng mất dữ liệu.
3. Scene cũ gắn chặt với `GhostAI` (DeathScreenManager tự tìm GhostAI, Ghost tuần tra theo bán kính). Scene mới tránh việc hai AI chạy song song.
4. Scene 4 MB với 872 object con trong `Map`: greybox, bake NavMesh và tối ưu đều chậm hơn.

Cách làm ở Phase 1 (không mở/lưu MapKinhDi):
- Tạo `Assets/_KhoaHon/Scenes/KH_Main.unity` mới, load additive tạm hoặc dùng `PrefabUtility.SaveAsPrefabAsset` trên **bản sao** `Player` để tạo `Assets/_KhoaHon/Prefabs/Characters/KH_Player.prefab`. Bản sao giữ đủ tay CivilianGirlHands, IK, hotbar, âm thanh. Không lưu lại MapKinhDi.
- Tương tự với `DeathScreenManager`, `EventSystem`, `Canvas` (nếu cần).
- Terrain mới nhỏ (~200 × 200 m) dùng các TerrainLayer của Flooded_Grounds (Asphalt cho đường), không sửa `Scene_A_Terrain.asset`.
- Vỏ ngoài biệt thự: ghép `Villa1_*` / `Villa2_*` ở Phase 5. Phase 1 dùng greybox khối xám.

---

## 3. Mặt bằng (greybox) — kích thước dự kiến

Một ô ≈ 1 m. Trần tầng trệt 3.5 m, tầng 2 cao +3.6 m. Hầm sâu ~10 m dưới bếp.

### 3.1. Sân, cổng, đường (Giai đoạn 0)
```
  [Đường lộ / Kết ẩn]  ◄────────── đường sương mù ~250 m (on-rails, DOPath) ──────────┐
                                                                                     │
                                                          biển báo ×4 (vòng lặp 2)   │
                                                                                     ▼
                                                                         ╔═══ CỔNG SẮT ═══╗  CP_Gate
                                                                         ║   sân ~20 m    ║
                                                                         ║   ┌────────┐   ║
                                                                         ║   │BIỆT THỰ│   ║
                                                                         ╚═══└────────┘═══╝
```

### 3.2. Tầng trệt (~24 × 16 m)
```
 ┌───────────────┬────────────────┐
 │  PHÒNG KHÁCH  │  PHÒNG LÀM VIỆC│
 │  ảnh gia tộc  │  két 1921      │
 │  bảng đồng    │  băng #2       │
 │      [D]      │       [D]      │
 ├──────┴───────────────┴────────┤
 │          SẢNH CHÍNH            │ CP_Hall
 │ [W]cửa sổ nạy   ═cầu thang═▲   │  (cửa cầu thang khóa → chìa trong két)
 │ balo Quân (băng #1, thẻ, xà beng)
 │        [CỬA CHÍNH — then cài]  │
 ├──────────────[D]──────────────┤
 │           NHÀ BẾP              │
 │  bàn tiệc 1978 · thiệp 1978    │
 │  di thư Lâm 1996               │
 │  ✶ ký hiệu HiddenInk trên sàn  │
 │  ▣ NẮP HẦM + 5 xích            │
 │  (vách khoan 2009 phía sau)    │
 └───────────────────────────────┘
```

### 3.3. Tầng 2 — hành lang vòng (~24 × 16 m)
```
 ┌─────────┬──────────┬──────────┐
 │TRẺ EM   │ PHÒNG THỜ│ KHÁCH TRỌ│
 │(Mộc)    │13 bài vị │(Thủy)    │
 │tủ áo ■  │5 đài sen │gầm giường■│
 │  [D]    │ [D]  [D] │  [D]  [D]│◄ 2 cửa
 ├──┴──────┴──┴────┴──┴──┴────┴─┤
 │  HÀNH LANG VÒNG (patrol loop)  │
 │  ▼ chiếu nghỉ / cầu thang      │ CP_Floor2Landing
 ├──┬──────┬──┬────┬──┬──────┬──┤
 │  [D]    │  [D]     │  [D]  [D]│◄ 2 cửa
 │GIA NHÂN │THƯ VIỆN  │NGỦ CHỦ   │
 │(Thổ)    │CỔ (Kim)  │NHÂN (Hỏa)│
 │lược ★xa │kệ sách   │gương, tủ ■│
 │gầm giường■│tủ ■    │          │
 └─────────┴──────────┴──────────┘
```
- Mỗi phòng có ít nhất 1 chỗ trốn (■). Khách Trọ và Ngủ Chủ Nhân có 2 cửa.
- Phòng Gia Nhân (lược) đặt ở góc xa cầu thang nhất.
- NavMesh area `Floor2` gồm hành lang, 6 phòng và cầu thang tới chiếu nghỉ. Bà Quản Gia chỉ được đi trên area này.

### 3.3b. Điều chỉnh khi dựng (Phase 1)
- Mọi ô cửa rộng **1.4 m**, cao **2.4 m**. Lý do: CharacterController của Vy cao 2 m, và agent NavMesh bán kính 0.5 m không lọt cửa 1.1 m.
- Cửa cầu thang ở z = −6.0, cách chân dốc 0.8 m. Lỗ nắp hầm x[4.6, 9] để đủ khoảng trống đầu khi xuống.
- Cửa mở **ra xa người chơi** để không chắn đường.
- Ánh sáng: dùng đèn làm việc `WorkLight_*` (point light, không đổ bóng) để greybox nhìn được. Phase 3 thay bằng ánh sáng thật.
- Két sắt: tạm thời là Pickup `Placeholder_SafeContents` (chìa, Sổ Khóa Hồn, bản thiết kế) để chơi xuyên suốt được. Phase 2 thay bằng `SafeKeypad` mã 1921.

### 3.4. Hầm
```
 NẮP HẦM (bếp)
   │ cầu thang đá dốc ~10 m, vách lộ xương 8 phu hồ   CP_CellarStairs
   │ băng #3 · đống đá sập (lỗ khoan 2009)
   ▼
 ┌──────────┐   hành lang hẹp
 │ XÀ LIM   │──────────────┐
 │ thi thể  │              ▼
 │ Quân, còi│       ╱‾‾‾‾‾‾‾‾‾‾╲
 └──────────┘      │ SẢNH TẾ    │ CP_RitualHall
                   │ bát giác   │
                   │ bệ đá · Ấn │
                   │ vạc máu đen│
                   │ ghế gỗ mun │
                    ╲__________╱
```

---

## 4. Danh sách script theo Phase

Tất cả trong `Assets/_KhoaHon/Scripts/`, namespace `KhoaHon.<Folder>`.

**Phase 1 — Greybox & điều khiển**
- Core: `StoryFlag`/`StoryFlags` (thuần C#), `GameStage`, `GameEvents`, `GameManager`, `SaveData` + `SaveSystem`, `Checkpoint` + `CheckpointManager`.
- Interaction: `IInteractable`, `PlayerInteractor`, `InteractPrompt`, `DoorInteractable`, `Pickup`, `ReadableDocument` + `DocumentReaderUI`.
- Items: `BatteryPool`, `FlashlightBatteryDrain` (component riêng, đọc trạng thái đèn mà không sửa `FlashlightItem`), `RecorderItem`/`FilmCameraItem`/`CrowbarItem` (bản rỗng: equip được), `RelicInventory`.
- Notebook: `NotebookEntry` (SO), `NotebookManager`, `NotebookUI` (5 tab).
- Data: `DocumentData`, `TapeData`, `TalismanData`, `RelicData`, `ToolsConfig`.
- Debug: `KhoaHonCheatPanel` (F10, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
- Editor: `KhoaHonGreyboxBuilder` (dựng greybox từ code để tái tạo được).

**Phase 2 — Giải đố & AI**
- Puzzles: `SafeCode` (thuần) + `SafeKeypad` UI, `LotusAltarLogic` (thuần) + `LotusAltar`, `CellarChains`.
- AI: `HousekeeperConfig`, `HousekeeperStateMachine` (bảng chuyển trạng thái thuần), `HousekeeperAI`, `NoiseSystem`, `PlayerNoiseEmitter`, `HidingSpot`, `BreathHold`.

**Phase 3 — Công cụ, âm thanh, sanity**
- Items: logic đầy đủ của `RecorderItem` và `FilmCameraItem`, `PhotoClue`, `PhotoResult`.
- Narrative: `EvpLine` (SO), `EvpWhisperZone`, `EvpDirector`, `TapePlayer`, `SubtitleUI`.
- Sanity: `SanityConfig`, `SanitySystem`, `SanityEffects`, `FakeAudioEvents`.
- Sequences: `GateLoopSequence`.
- Audio: `KH_Mixer` + snapshots, `AudioMixConfig`, mưa/sấm.

**Phase 4 — Hầm, đối thoại, kết**
- Narrative: `DialogueData`, `DialogueRunner`, `ChoiceUI`.
- Sequences: `EndingDirector`, `Ending1Sequence`, `Ending2Sequence`, `SecretEndingSequence`, `MirrorNoReflection`, các Timeline signal receiver.
- Scene `KH_End_Asylum.unity`.

**Phase 5 — Đánh bóng**
- `KH_Menu.unity`, `MainMenuUI`, `PauseMenuUI`, `SettingsData`. Thay greybox bằng prop.

**Thay đổi duy nhất vào code có sẵn:** thêm 3 giá trị cuối enum `ItemType` (`Scripts/Inventory/ItemData.cs`). Mọi thứ khác dùng API public/event có sẵn.

---

## 5. Test

- Logic thuần (StoryFlags, SafeCode, LotusAltarLogic, HousekeeperStateMachine, SaveData round-trip) không phụ thuộc Unity API ngoài `JsonUtility`.
- Vì code nằm trong Assembly-CSharp (không asmdef), Phase 1 sẽ thử theo thứ tự:
  1. Đặt test trong `Assets/_KhoaHon/Tests/Editor/` (thuộc Assembly-CSharp-Editor, tự tham chiếu Assembly-CSharp), dùng `nunit` từ Test Framework 1.6.
  2. Nếu Test Runner không nhận: tạo `KhoaHon.Tests.EditMode.asmdef` và tách logic thuần sang `KhoaHon.Logic.asmdef` (không phụ thuộc gì) để cả game lẫn test cùng tham chiếu.
- Cách chạy được sẽ ghi vào `CLAUDE.md`. Chạy bằng MCP `run_tests` (mode EditMode).

---

## 6. Font (đã xong ở Phase 0)

- `Assets/_KhoaHon/Fonts/`: `BeVietnamPro-Regular/SemiBold.ttf` (UI), `NotoSerif-Regular/Bold.ttf` (tài liệu, bùa, bảng đồng), kèm `OFL_*.txt`.
- 4 TMP Font Asset Dynamic (`* SDF.asset`, 1024², SDFAA, padding 9). `HasCharacters` cho chuỗi thử trả về true, không thiếu glyph.
- Ảnh kiểm chứng: `Temp/KhoaHon/kh_p0_font_test.png`, render trong preview scene, không đụng scene đang mở.

---

## 7. Rủi ro

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| `DeathScreenManager` hồi sinh về `spawnPoint` cố định và tự gọi GhostAI | TB | Dời Transform `spawnPoint` theo checkpoint. KH_Main không có GhostAI. Nếu vẫn vướng thì viết `KhoaHonDeathFlow` riêng, mượn UI. |
| Bản sao Player từ MapKinhDi mang theo tham chiếu tới object trong scene cũ (stamina canvas, UI) | TB | Kiểm tra từng field null sau khi tạo prefab. Thiếu thì gán lại trong KH_Main. |
| `PlayerController` tự đọc `Keyboard.current`/`Mouse.current` (Esc, click) | Thấp | `SetControlEnabled(false)` khi mở UI. Kiểm tra Esc không xung đột với pause menu. |
| Hotbar cố định 4 ô, Radar đang ở ô 2 trong dữ liệu Player | Thấp | `SetSlotData` trên prefab của KH_Main, không sửa `Item_Radar`. |
| Tay FPS cầm vật mới (máy ghi âm, máy ảnh, xà beng) cần pose | TB | Mượn `Pose_HoldDevice`/`Pose_GripCylinder`, chỉnh offset trong `ItemData` mới. Phải kiểm tra thị giác từng ô. |
| NavMesh 2 tầng: bà Quản Gia có thể đi xuống tầng trệt | Cao | Area `Floor2` + `areaMask` của agent. Test tự chạy 10 phút có log. |
| Chụp flash bằng camera phụ + ReadPixels tốn GPU | Thấp | Render 1 lần/kiểu, RenderTexture 512². |
| Hiệu năng ≥ 60 FPS khi có nhiều đèn chớp và shadow | TB | Hạn chế đèn có shadow, bake, occlusion culling ở Phase 5. |
| Đọc `mcpforunity` screenshot ở game_view khi không Play cho ảnh rất tối (scene đêm) | Thấp | Ảnh kiểm tra layout chụp ở chế độ scene_view hoặc bật đèn debug tạm trong preview scene. |
| Thiếu asset đẹp (bài vị, đài sen, Ấn, vạc, búp bê…) | Cao về hình, thấp về tiến độ | Placeholder + `ASSETS_NEEDED.md`. |

---

## 8. Các điểm bổ sung so với GDD (PROMPT mục 12, giữ mặc định)

1. Quân xuống hầm qua lỗ khoan 2009 sau vách bếp, vách sập, kẹt chân, chết vì mất máu. Quân chưa từng mở két hay lên tầng 2.
2. Bà Quản Gia chỉ hoạt động ở tầng 2 và cầu thang. Tầng trệt là vùng an toàn tương đối.
3. Kết ẩn chỉ mở khi `CellarOpened == false`.
4. Có thể xuống xe ngay ở vòng 1.
5. Văn bản mục 8 của PROMPT ngoài các câu trích từ GDD là nội dung viết mới, đã đối chiếu 4 Luật.

Không có quyết định thiết kế tự đặt thêm ở Phase 0.

---

## 9. Cần người dùng quyết định trước Phase 1

- Duyệt **scene (b)**: dựng `KH_Main` mới, tạo `KH_Player.prefab` từ bản sao Player của MapKinhDi.
- Đồng ý thay đổi duy nhất vào code dùng chung: thêm `Recorder, FilmCamera, Crowbar` vào cuối enum `ItemType`.
