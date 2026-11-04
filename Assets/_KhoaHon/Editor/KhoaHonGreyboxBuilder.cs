using System.Collections.Generic;
using System.Linq;
using HorrorGame.Inventory;
using KhoaHon.Core;
using KhoaHon.DebugTools;
using KhoaHon.Interaction;
using KhoaHon.Items;
using KhoaHon.Notebook;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Dựng scene greybox KH_Main (Phase 1): sân + cổng + đường, biệt thự 2 tầng (10 phòng), hầm 2 khu,
    /// hệ thống game, Player sao chép từ MapKinhDi_Ver1 (không lưu scene nguồn), NavMesh có area Floor2.
    /// Mặt bằng: xem Docs/KhoaHon/PLAN.md mục 3.
    /// </summary>
    public static class KhoaHonGreyboxBuilder
    {
        public const string ScenePath = KhoaHonDataBuilder.Root + "/Scenes/KH_Main.unity";
        const string SourceSceneName = "MapKinhDi_Ver1";
        const float Wt = 0.2f;     // độ dày tường
        const float H1 = 3.4f;     // trần tầng trệt
        const float F2 = 3.6f;     // sàn tầng 2
        const float H2 = 3.4f;     // chiều cao tường tầng 2

        internal static Material mWall, mFloor, mDoor, mStone, mGround, mRoad, mInteract, mBlood, mMetal;
        internal static KhoaHonDataBuilder.Result data;
        internal static readonly List<string> log = new List<string>();


        /// <summary>Khởi tạo vật liệu + dữ liệu để các công cụ khác (VillaConverter) dùng lại hàm dựng.</summary>
        internal static void Init(KhoaHonDataBuilder.Result r)
        {
            data = r;
            var m = r.mats;
            mWall = m[0]; mFloor = m[1]; mDoor = m[2]; mStone = m[3]; mGround = m[4]; mRoad = m[5]; mMetal = m[7]; mInteract = m[10]; mBlood = m[11];
        }

        internal static List<string> Log => log;

        [MenuItem("KhoaHon/Build/2. Greybox Scene KH_Main")]
        public static void BuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            log.Clear();
            if (EditorApplication.isPlaying) return "Đang Play — dừng trước khi dựng.";
            var source = SceneManager.GetSceneByName(SourceSceneName);
            if (!source.IsValid() || !source.isLoaded) return "Cần mở scene " + SourceSceneName + " để sao chép Player.";

            data = KhoaHonDataBuilder.Build();
            var m = data.mats;
            mWall = m[0]; mFloor = m[1]; mDoor = m[2]; mStone = m[3]; mGround = m[4]; mRoad = m[5]; mMetal = m[7]; mInteract = m[10]; mBlood = m[11];
            int floor2Area = EnsureNavArea("Floor2", 3);

            var prevActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var env = new GameObject("Environment").transform;
            var level = new GameObject("Level").transform;
            BuildLighting(env);
            BuildExterior(level);
            var doors = BuildGroundFloor(level);
            BuildFloor2(level, doors);
            BuildCellar(level);

            var systems = BuildSystems();
            var player = CopyPlayerRig(source, scene, systems);
            BuildCheckpointsAndTriggers(level, doors);
            BakeNavMesh(scene, floor2Area);

            SceneManager.SetActiveScene(prevActive);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            log.Add("Saved " + ScenePath);
            return string.Join("\n", log);
        }

        // =====================================================================
        // Ánh sáng
        internal static void BuildLighting(Transform env)
        {
            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.transform.SetParent(env);
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.62f, 0.85f);
            moon.intensity = 0.12f;
            moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(35, -30, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.045f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.skybox = null;

            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Settings/HorrorGlobalVolumeProfile.asset");
            if (profile != null)
            {
                var vol = new GameObject("GlobalVolume").AddComponent<UnityEngine.Rendering.Volume>();
                vol.transform.SetParent(env);
                vol.isGlobal = true;
                vol.sharedProfile = profile;
            }
            log.Add("Lighting ok");
        }

        internal static void WorkLight(Transform parent, string name, Vector3 pos, float range = 9f, float intensity = 1.6f)
        {
            var l = new GameObject("WorkLight_" + name).AddComponent<Light>();
            l.transform.SetParent(parent);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = new Color(1f, 0.86f, 0.7f);
            l.shadows = LightShadows.None;
        }

        // =====================================================================
        // Ngoại thất
        internal static void BuildExterior(Transform level)
        {
            var ext = Group(level, "Exterior");
            // Mặt đất quanh nhà (chừa đúng footprint nhà để không chắn hầm)
            Box(ext, "Ground_S", new Vector3(0, -0.25f, -172f), new Vector3(80, 0.5f, 326), mGround);
            Box(ext, "Ground_N", new Vector3(0, -0.25f, 22f), new Vector3(80, 0.5f, 26), mGround);
            Box(ext, "Ground_W", new Vector3(-26f, -0.25f, 0), new Vector3(28, 0.5f, 18), mGround);
            Box(ext, "Ground_E", new Vector3(26f, -0.25f, 0), new Vector3(28, 0.5f, 18), mGround);
            // Đường sương mù
            Box(ext, "Road", new Vector3(0, 0.01f, -170f), new Vector3(7, 0.02f, 270), mRoad, false);
            // Hàng rào + cổng
            var fence = Group(ext, "Fence");
            Box(fence, "Fence_W", new Vector3(-25, 1.1f, -10), new Vector3(0.2f, 2.2f, 50), mMetal);
            Box(fence, "Fence_E", new Vector3(25, 1.1f, -10), new Vector3(0.2f, 2.2f, 50), mMetal);
            Box(fence, "Fence_N", new Vector3(0, 1.1f, 15), new Vector3(50, 2.2f, 0.2f), mMetal);
            Box(fence, "Fence_S_W", new Vector3(-13.75f, 1.1f, -35), new Vector3(22.5f, 2.2f, 0.2f), mMetal);
            Box(fence, "Fence_S_E", new Vector3(13.75f, 1.1f, -35), new Vector3(22.5f, 2.2f, 0.2f), mMetal);
            var gate = Group(ext, "Gate_Main");
            gate.position = new Vector3(0, 0, -35);
            Box(gate, "GateLeaf_L", new Vector3(-1.25f, 1.2f, 0), new Vector3(2.5f, 2.4f, 0.1f), mMetal, true, true);
            Box(gate, "GateLeaf_R", new Vector3(1.25f, 1.2f, 0), new Vector3(2.5f, 2.4f, 0.1f), mMetal, true, true);
            // Giai đoạn 0 (Phase 3): cổng mở bằng GateLoopSequence → tạm thời tắt 2 cánh để đi qua được
            gate.gameObject.SetActive(false);

            // Xe (prefab Flooded_Grounds, chỉ đặt instance)
            var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Props/Prop_Car_A.prefab");
            if (carPrefab != null)
            {
                var car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab, ext);
                car.name = "Car_Vy";
                car.transform.position = new Vector3(1.6f, 0, -42f);
            }
            // Thềm cửa
            Box(ext, "Porch", new Vector3(0, -0.05f, -10.5f), new Vector3(5, 0.1f, 3), mStone);
            WorkLight(ext, "Yard", new Vector3(0, 4, -22), 16, 1.2f);
            Room(ext, "Room_San", "Sân & cổng", new Vector3(0, 0, -22), new Vector3(50, 3, 26));
        }

        // =====================================================================
        // Tầng trệt — x[-12,12] z[-9,9]
        internal class Doors
        {
            public DoorInteractable front, stair, living, study, kitchen;
        }

        internal static Doors BuildGroundFloor(Transform level)
        {
            var gf = Group(level, "GroundFloor");
            var d = new Doors();
            // Sàn với lỗ nắp hầm (bếp) x[4.6,9] z[4.4,6]
            Slab(gf, "Floor_GF", new Rect(-12, -9, 24, 18), -0.2f, 0.2f, mFloor, new[] { new Rect(4.6f, 4.4f, 4.4f, 1.6f) });
            // Trần = sàn tầng 2 (lỗ cầu thang x[2.2,4] z[-5.2,0])
            Slab(gf, "Ceiling_GF", new Rect(-12, -9, 24, 18), H1, 0.2f, mFloor, new[] { new Rect(2.2f, -5.2f, 1.8f, 5.2f) });

            // Tường ngoài
            WallX(gf, "Ext_S", -12, 12, -9, 0, H1, new[] { Op(0, 1.4f, 2.4f), Op(-2.6f, 1.2f, 2.1f, 0.9f), Op(-8, 1.2f, 2.1f, 0.9f), Op(8, 1.2f, 2.1f, 0.9f) });
            WallX(gf, "Ext_N", -12, 12, 9, 0, H1, new[] { Op(-6, 1.2f, 2.1f, 0.9f) });
            WallZ(gf, "Ext_W", -9, 9, -12, 0, H1, null);
            WallZ(gf, "Ext_E", -9, 9, 12, 0, H1, null);
            // Vách trong
            WallX(gf, "Int_KitchenWall", -12, 12, 2, 0, H1, new[] { Op(-1, 1.4f, 2.4f) });
            WallZ(gf, "Int_LivingWall", -9, 2, -4, 0, H1, new[] { Op(-7, 1.4f, 2.4f) });
            WallZ(gf, "Int_StudyWall", -9, 2, 4, 0, H1, new[] { Op(-7, 1.4f, 2.4f) });
            // Hộp cầu thang: tường x=2.2 từ z -5.6 → 0, cửa ngang z=-5.6
            WallZ(gf, "Stair_SideWall", -6.0f, 2f, 2.2f, 0, H1, null);
            WallX(gf, "Stair_DoorWall", 2.2f, 4f, -6.0f, 0, H1, new[] { Op(3.1f, 1.4f, 2.4f) });

            // Cửa
            d.front = Door(gf, "Door_Front", new Vector3(-0.7f, 0, -9), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            d.living = Door(gf, "Door_Living", new Vector3(-4, 0, -6.3f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.Z);
            d.study = Door(gf, "Door_Study", new Vector3(4, 0, -6.3f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.Z);
            d.kitchen = Door(gf, "Door_Kitchen", new Vector3(-1.7f, 0, 2), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            d.stair = Door(gf, "Door_Stair", new Vector3(2.4f, 0, -6.0f), 1.4f, 2.4f, true, "key_upstairs", "prompt.stair_door_locked", "key.upstairs", Axis.X);

            // Cầu thang lên tầng 2: z -5.2 → 0, x[2.2,4]
            Stairs(gf, "Stairs_Up", new Vector3(3.1f, 0, -5.2f), new Vector3(3.1f, F2, 0f), 1.8f, 18, mFloor);

            // ---- Phòng & nội dung
            var hall = Room(gf, "Room_SanhChinh", "Sảnh Chính", new Vector3(0, 0, -3.5f), new Vector3(8, H1, 11));
            WorkLight(hall, "Hall", new Vector3(0, 2.8f, -4));
            Box(hall, "HallWindow_Pryable", new Vector3(-2.6f, 1.5f, -9), new Vector3(1.2f, 1.2f, 0.06f), mInteract, true);
            var backpack = PickupBox(hall, "Pickup_QuanBackpack", new Vector3(1.4f, 0.25f, -5.0f), new Vector3(0.45f, 0.5f, 0.3f), "item.backpack",
                rewards: new[] { StoryFlag.GotQuanBackpack, StoryFlag.HasCrowbar },
                sources: new NotebookSource[] { data.sources["TAPE_1"], data.sources["DOC_Now_TheQuan"] }, hint: "hint.backpack");
            Readable(hall, "Doc_BienBan2009", new Vector3(0.9f, 1.5f, -8.88f), new Vector3(0.3f, 0.4f, 0.02f), "DOC_2009_BienBan");
            PickupBox(hall, "Pickup_Battery_Hall", new Vector3(-3.4f, 0.8f, 1.4f), new Vector3(0.08f, 0.15f, 0.08f), "item.battery", battery: 50f);

            var kitchen = Room(gf, "Room_NhaBep", "Nhà Bếp", new Vector3(0, 0, 5.5f), new Vector3(24, H1, 7));
            WorkLight(kitchen, "Kitchen", new Vector3(0, 2.8f, 5.5f), 14);
            Box(kitchen, "BanTiec1978", new Vector3(-4, 0.4f, 5.5f), new Vector3(4, 0.8f, 1.4f), mDoor);
            Readable(kitchen, "Doc_ThiepTanGia1978", new Vector3(-5, 0.82f, 5.3f), new Vector3(0.2f, 0.02f, 0.14f), "DOC_1978_ThiepTanGia");
            Readable(kitchen, "Doc_DiThuLam1996", new Vector3(-2.6f, 0.82f, 5.7f), new Vector3(0.22f, 0.02f, 0.3f), "DOC_1996_DiThuLam");
            // Ký hiệu máu (layer HiddenInk ở Phase 3)
            Box(kitchen, "HiddenInk_KyHieu", new Vector3(-4, 0.005f, 3.6f), new Vector3(1.4f, 0.01f, 1.4f), mBlood, false);
            // Nắp hầm + 5 xích (lỗ x[4.6,9] để đủ khoảng trống đầu khi xuống dốc)
            var hatch = Box(kitchen, "CellarHatch_Closed", new Vector3(6.8f, -0.1f, 5.2f), new Vector3(4.4f, 0.2f, 1.6f), mMetal);
            var info = hatch.gameObject.AddComponent<InfoInteractable>();
            info.Configure("prompt.chains", "msg.chains");
            var chains = Group(hatch, "Chains");
            for (int i = 0; i < 5; i++)
            {
                var c = Box(chains, "Chain_" + (i + 1), new Vector3(6.2f + i * 0.65f, 0.6f, 6.0f + (i % 2) * 0.2f), new Vector3(0.08f, 1.2f, 0.08f), mMetal, false);
                c.rotation = Quaternion.Euler(-35, 0, 0);
            }
            var hatchOpen = Group(kitchen, "CellarHatch_Open");
            var fa = kitchen.gameObject.AddComponent<FlagActivator>();
            fa.Configure(StoryFlag.CellarOpened, new[] { hatchOpen.gameObject }, new[] { hatch.gameObject });
            Box(hatchOpen, "HatchLid_Open", new Vector3(6.8f, 0.8f, 6.9f), new Vector3(4.4f, 1.6f, 0.1f), mMetal);

            var living = Room(gf, "Room_PhongKhach", "Phòng Khách", new Vector3(-8, 0, -3.5f), new Vector3(8, H1, 11));
            WorkLight(living, "Living", new Vector3(-8, 2.8f, -3.5f));
            Box(living, "AnhGiaToc_Trinh", new Vector3(-11.88f, 1.9f, -3.5f), new Vector3(0.04f, 1.4f, 1.0f), mDoor, false);
            Readable(living, "Doc_BangDong1921", new Vector3(-11.86f, 1.0f, -3.5f), new Vector3(0.04f, 0.25f, 0.5f), "DOC_1921_BangDong");
            Box(living, "Sofa", new Vector3(-8, 0.4f, -1), new Vector3(2.4f, 0.8f, 0.9f), mDoor);

            var study = Room(gf, "Room_PhongLamViec", "Phòng Làm Việc", new Vector3(8, 0, -3.5f), new Vector3(8, H1, 11));
            WorkLight(study, "Study", new Vector3(8, 2.8f, -3.5f));
            Box(study, "BanLamViec", new Vector3(8, 0.4f, -1.5f), new Vector3(2, 0.8f, 1), mDoor);
            var safe = Box(study, "KetSat", new Vector3(11.4f, 0.5f, 0.6f), new Vector3(0.8f, 1.0f, 0.8f), mMetal);
            // TẠM THỜI (Phase 2 thay bằng SafeKeypad mã 1921): nhặt trực tiếp nội dung két
            PickupBox(study, "Placeholder_SafeContents", new Vector3(11.4f, 1.1f, 0.6f), new Vector3(0.3f, 0.12f, 0.22f), "item.safe_placeholder",
                key: "key_upstairs", rewards: new[] { StoryFlag.SafeOpened, StoryFlag.HasUpstairsKey },
                sources: new NotebookSource[] { data.sources["DOC_1924_SoKhoaHon"], data.sources["DOC_1924_BanThietKeHam"] });
            PickupBox(study, "Pickup_Tape2", new Vector3(10.6f, 0.05f, 0.4f), new Vector3(0.1f, 0.02f, 0.065f), "item.tape",
                sources: new NotebookSource[] { data.sources["TAPE_2"] });
            PickupBox(study, "Pickup_MauBao1953", new Vector3(7.6f, 0.82f, -1.6f), new Vector3(0.3f, 0.01f, 0.22f), "item.newspaper",
                sources: new NotebookSource[] { data.sources["DOC_1953_MauBao"] }, openDoc: true);
            PickupBox(study, "Pickup_Film_Study", new Vector3(8.6f, 0.82f, -1.4f), new Vector3(0.05f, 0.05f, 0.05f), "item.film", film: 6);
            log.Add("Ground floor ok");
            return d;
        }

        // =====================================================================
        // Tầng 2 — hành lang z[-1.5,1.5]
        internal static void BuildFloor2(Transform level, Doors doors)
        {
            var f2 = Group(level, "Floor2");
            float y = F2;
            Slab(f2, "Roof", new Rect(-12, -9, 24, 18), y + H2, 0.2f, mWall, null);
            WallX(f2, "Ext_S", -12, 12, -9, y, H2, new[] { Op(-8, 1.2f, 2.0f, 0.9f), Op(8, 1.2f, 2.0f, 0.9f), Op(-1, 1.2f, 2.0f, 0.9f) });
            WallX(f2, "Ext_N", -12, 12, 9, y, H2, new[] { Op(-8, 1.2f, 2.0f, 0.9f), Op(0, 1.2f, 2.0f, 0.9f), Op(8, 1.2f, 2.0f, 0.9f) });
            WallZ(f2, "Ext_W", -9, 9, -12, y, H2, null);
            WallZ(f2, "Ext_E", -9, 9, 12, y, H2, null);
            // Hành lang
            WallX(f2, "Corr_S_W", -12, 2, -1.5f, y, H2, new[] { Op(-8, 1.4f, 2.4f), Op(-1, 1.4f, 2.4f) });
            WallX(f2, "Corr_S_E", 4, 12, -1.5f, y, H2, new[] { Op(6, 1.4f, 2.4f), Op(10, 1.4f, 2.4f) });
            WallX(f2, "Corr_N", -12, 12, 1.5f, y, H2, new[] { Op(-8, 1.4f, 2.4f), Op(0, 1.6f, 2.4f), Op(6, 1.4f, 2.4f), Op(10, 1.4f, 2.4f) });
            // Vách phòng
            WallZ(f2, "N_Div_W", 1.5f, 9, -4, y, H2, null);
            WallZ(f2, "N_Div_E", 1.5f, 9, 4, y, H2, null);
            WallZ(f2, "S_Div_W", -9, -1.5f, -4, y, H2, null);
            WallZ(f2, "Stairwell_W", -9, -1.5f, 2, y, H2, null);
            WallZ(f2, "Stairwell_E", -9, -1.5f, 4, y, H2, null);
            // Lan can lỗ cầu thang trong hành lang
            Box(f2, "Rail_W", new Vector3(2.15f, y + 0.5f, -0.75f), new Vector3(0.08f, 1.0f, 1.5f), mDoor);
            Box(f2, "Rail_E", new Vector3(4.05f, y + 0.5f, -0.75f), new Vector3(0.08f, 1.0f, 1.5f), mDoor);

            // Cửa phòng (không khóa)
            Door(f2, "Door_GiaNhan", new Vector3(-8.7f, y, -1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            Door(f2, "Door_ThuVien", new Vector3(-1.7f, y, -1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            Door(f2, "Door_NguChuNhan_A", new Vector3(5.3f, y, -1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            Door(f2, "Door_NguChuNhan_B", new Vector3(9.3f, y, -1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            Door(f2, "Door_TreEm", new Vector3(-8.7f, y, 1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            Door(f2, "Door_KhachTro_A", new Vector3(5.3f, y, 1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);
            Door(f2, "Door_KhachTro_B", new Vector3(9.3f, y, 1.5f), 1.4f, 2.4f, false, "", "prompt.locked", "", Axis.X);

            var corr = Room(f2, "Room_HanhLang", "Hành lang tầng 2", new Vector3(0, y, 0), new Vector3(24, H2, 3));
            WorkLight(corr, "Corr_W", new Vector3(-7, y + 2.8f, 0), 9, 1.2f);
            WorkLight(corr, "Corr_E", new Vector3(7, y + 2.8f, 0), 9, 1.2f);

            var shrine = Room(f2, "Room_PhongTho", "Phòng Thờ Gia Tộc", new Vector3(0, y, 5.25f), new Vector3(8, H2, 7.5f));
            WorkLight(shrine, "Shrine", new Vector3(0, y + 2.8f, 5.25f), 8, 1.0f);
            var altar = Box(shrine, "BanTho", new Vector3(0, y + 0.5f, 8.2f), new Vector3(6, 1.0f, 1.0f), mDoor);
            altar.gameObject.AddComponent<InfoInteractable>().Configure("prompt.altar", "msg.altar");
            for (int i = 0; i < 13; i++)
                Box(shrine, "BaiVi_" + (i + 1), new Vector3(-2.7f + i * 0.45f, y + 1.25f, 8.45f), new Vector3(0.22f, 0.5f, 0.06f), i % 3 == 1 ? mInteract : mDoor, false);
            string[] hanh = { "Moc", "Thuy", "Tho", "Kim", "Hoa" };
            for (int i = 0; i < 5; i++)
                Box(shrine, "DaiSen_" + hanh[i], new Vector3(-2.4f + i * 1.2f, y + 1.05f, 7.85f), new Vector3(0.45f, 0.1f, 0.45f), mInteract, false);

            // 5 phòng Mắt Trận: kỷ vật + chỗ trốn placeholder
            RelicRoom(f2, "Room_TreEm", "Phòng Trẻ Em (Mộc)", new Vector3(-8, y, 5.25f), new Vector3(8, H2, 7.5f), 0, new Vector3(-11.2f, y + 0.9f, 8.3f), "item.relic_moc",
                wardrobe: new Vector3(-11.2f, y, 8.3f), bed: new Vector3(-6, y, 7.5f));
            RelicRoom(f2, "Room_KhachTro", "Phòng Khách Trọ (Thủy)", new Vector3(8, y, 5.25f), new Vector3(8, H2, 7.5f), 1, new Vector3(10.5f, y + 0.8f, 6.5f), "item.relic_thuy",
                wardrobe: null, bed: new Vector3(8, y, 7.6f));
            RelicRoom(f2, "Room_GiaNhan", "Phòng Gia Nhân (Thổ)", new Vector3(-8, y, -5.25f), new Vector3(8, H2, 7.5f), 2, new Vector3(-11.3f, y + 0.8f, -8.3f), "item.relic_tho",
                wardrobe: null, bed: new Vector3(-9, y, -7.6f));
            RelicRoom(f2, "Room_ThuVien", "Thư Viện Cổ (Kim)", new Vector3(-1, y, -5.25f), new Vector3(6, H2, 7.5f), 3, new Vector3(1.3f, y + 1.0f, -8.4f), "item.relic_kim",
                wardrobe: new Vector3(-3.2f, y, -8.3f), bed: null);
            RelicRoom(f2, "Room_NguChuNhan", "Phòng Ngủ Chủ Nhân (Hỏa)", new Vector3(8, y, -5.25f), new Vector3(8, H2, 7.5f), 4, new Vector3(11.2f, y + 0.1f, -4.0f), "item.relic_hoa",
                wardrobe: new Vector3(11.2f, y, -8.3f), bed: new Vector3(8, y, -7.6f));
            log.Add("Floor 2 ok");
        }

        internal static void RelicRoom(Transform parent, string id, string label, Vector3 center, Vector3 size, int hanh, Vector3 relicPos, string nameKey, Vector3? wardrobe, Vector3? bed)
        {
            var room = Room(parent, id, label, center, size);
            WorkLight(room, id, center + Vector3.up * 2.8f, 8, 1.0f);
            var relic = data.relics[hanh];
            PickupBox(room, "Relic_" + relic.hanh, relicPos, new Vector3(0.25f, 0.2f, 0.18f), nameKey, sources: new NotebookSource[] { relic });
            if (wardrobe.HasValue) Box(room, "HidingSpot_Wardrobe", wardrobe.Value + new Vector3(0, 1.1f, 0), new Vector3(1.2f, 2.2f, 0.65f), mDoor);
            if (bed.HasValue) Box(room, "HidingSpot_Bed", bed.Value + new Vector3(0, 0.35f, 0), new Vector3(2.0f, 0.12f, 1.4f), mDoor);
        }

        // =====================================================================
        // Hầm: cầu thang đá từ nắp hầm (x 9 → -5, y 0 → -10), chiếu nghỉ, hành lang, xà lim, sảnh tế bát giác
        internal static void BuildCellar(Transform level)
        {
            var c = Group(level, "Cellar");
            float yb = -10f;
            // Đường hầm cầu thang z[4.4,6.0]
            Stairs(c, "Stairs_Cellar", new Vector3(9f, 0f, 5.2f), new Vector3(-5f, yb, 5.2f), 1.6f, 40, mStone);
            Box(c, "StairTunnel_WallS", new Vector3(2, yb / 2f, 4.3f), new Vector3(14.6f, -yb, 0.2f), mStone);
            Box(c, "StairTunnel_WallN", new Vector3(2, yb / 2f, 6.1f), new Vector3(14.6f, -yb, 0.2f), mStone);
            Box(c, "StairTunnel_End_E", new Vector3(9.6f, -1.5f, 5.2f), new Vector3(0.2f, 3f, 1.6f), mStone);
            // Chiếu nghỉ đáy x[-8,-5] z[3,7]
            Box(c, "Landing_Floor", new Vector3(-6.5f, yb - 0.1f, 5.0f), new Vector3(3.2f, 0.2f, 4.4f), mStone);
            Box(c, "Landing_Ceiling", new Vector3(-6.5f, yb + 3.1f, 5.0f), new Vector3(3.2f, 0.2f, 4.4f), mStone);
            Box(c, "Landing_WallW", new Vector3(-8.1f, yb + 1.5f, 5.0f), new Vector3(0.2f, 3f, 4.4f), mStone);
            Box(c, "Landing_WallN", new Vector3(-6.5f, yb + 1.5f, 7.3f), new Vector3(3.2f, 3f, 0.2f), mStone);
            Box(c, "Landing_WallE_S", new Vector3(-4.9f, yb + 1.5f, 3.7f), new Vector3(0.2f, 3f, 1.4f), mStone);
            Box(c, "Landing_WallE_N", new Vector3(-4.9f, yb + 1.5f, 6.65f), new Vector3(0.2f, 3f, 1.1f), mStone);
            var rubble = Box(c, "Rubble_2009Tunnel", new Vector3(-6.5f, yb + 0.6f, 6.7f), new Vector3(2.4f, 1.2f, 0.9f), mStone);
            rubble.gameObject.AddComponent<InfoInteractable>().Configure("prompt.inspect", "msg.rubble");
            PickupBox(c, "Pickup_Tape3", new Vector3(-5.6f, yb + 0.05f, 4.4f), new Vector3(0.1f, 0.02f, 0.065f), "item.tape",
                sources: new NotebookSource[] { data.sources["TAPE_3"] });
            WorkLight(c, "Landing", new Vector3(-6.5f, yb + 2.5f, 5), 7, 0.8f);

            // Hành lang x[-7.5,-5.5] z[-6,3]
            Box(c, "Corr_Floor", new Vector3(-6.5f, yb - 0.1f, -1.5f), new Vector3(2.2f, 0.2f, 9.2f), mStone);
            Box(c, "Corr_Ceiling", new Vector3(-6.5f, yb + 3.1f, -1.5f), new Vector3(2.2f, 0.2f, 9.2f), mStone);
            Box(c, "Corr_WallE", new Vector3(-5.4f, yb + 1.5f, -1.5f), new Vector3(0.2f, 3f, 9.2f), mStone);
            Box(c, "Corr_WallW_S", new Vector3(-7.6f, yb + 1.5f, -3.6f), new Vector3(0.2f, 3f, 5f), mStone);
            Box(c, "Corr_WallW_N", new Vector3(-7.6f, yb + 1.5f, 2.0f), new Vector3(0.2f, 3f, 2f), mStone);
            Box(c, "Corr_WallW_Lintel", new Vector3(-7.6f, yb + 2.7f, -0.05f), new Vector3(0.2f, 0.6f, 1.9f), mStone);
            Box(c, "Bones_Wall", new Vector3(-5.5f, yb + 1.4f, -1.5f), new Vector3(0.05f, 1.6f, 6f), mInteract, false);

            // Xà lim x[-13,-7.5] z[-3,3]
            var cell = Room(c, "Room_XaLim", "Xà lim ngầm", new Vector3(-10.3f, yb, 0), new Vector3(5.6f, 3, 6));
            Box(cell, "Cell_Floor", new Vector3(-10.3f, yb - 0.1f, 0), new Vector3(5.6f, 0.2f, 6), mStone);
            Box(cell, "Cell_Ceiling", new Vector3(-10.3f, yb + 3.1f, 0), new Vector3(5.6f, 0.2f, 6), mStone);
            Box(cell, "Cell_WallW", new Vector3(-13.1f, yb + 1.5f, 0), new Vector3(0.2f, 3f, 6.2f), mStone);
            Box(cell, "Cell_WallS", new Vector3(-10.3f, yb + 1.5f, -3.1f), new Vector3(5.6f, 3f, 0.2f), mStone);
            Box(cell, "Cell_WallN", new Vector3(-10.3f, yb + 1.5f, 3.1f), new Vector3(5.6f, 3f, 0.2f), mStone);
            var body = Box(cell, "Body_Quan_Placeholder", new Vector3(-11.6f, yb + 0.25f, 1.6f), new Vector3(1.8f, 0.5f, 0.5f), mDoor);
            body.gameObject.AddComponent<InfoInteractable>().Configure("prompt.body", "");
            WorkLight(cell, "Cell", new Vector3(-10.3f, yb + 2.5f, 0), 7, 0.7f);

            // Sảnh tế bát giác, tâm (-6.5, yb, -12), bán kính 6, cửa phía bắc z=-6
            var hall = Room(c, "Room_SanhTe", "Sảnh Tế Khóa Hồn", new Vector3(-6.5f, yb, -12), new Vector3(12, 5, 12));
            var center = new Vector3(-6.5f, yb, -12f);
            Box(hall, "Ritual_Floor", center + new Vector3(0, -0.1f, 0), new Vector3(13, 0.2f, 13), mStone);
            Box(hall, "Ritual_Ceiling", center + new Vector3(0, 5.1f, 0), new Vector3(13, 0.2f, 13), mStone);
            float apothem = 6f;
            float side = 2f * apothem * Mathf.Tan(Mathf.PI / 8f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                var dir = Quaternion.Euler(0, ang, 0) * Vector3.forward;
                if (i == 0)
                {
                    // Cạnh bắc có lối vào rộng 2 m
                    float half = (side - 2f) / 2f;
                    Box(hall, "Oct_Wall_0_L", center + dir * apothem + new Vector3(-(1f + half / 2f), 2.5f, 0), new Vector3(half, 5f, 0.3f), mStone);
                    Box(hall, "Oct_Wall_0_R", center + dir * apothem + new Vector3(1f + half / 2f, 2.5f, 0), new Vector3(half, 5f, 0.3f), mStone);
                    Box(hall, "Oct_Wall_0_Top", center + dir * apothem + new Vector3(0, 4f, 0), new Vector3(2f, 2f, 0.3f), mStone);
                    continue;
                }
                var w = Box(hall, "Oct_Wall_" + i, center + dir * apothem + Vector3.up * 2.5f, new Vector3(side + 0.3f, 5f, 0.3f), mStone);
                w.rotation = Quaternion.Euler(0, ang, 0);
            }
            Box(hall, "Ritual_Entry_Floor", new Vector3(-6.5f, yb - 0.1f, -5.9f), new Vector3(2.2f, 0.2f, 0.6f), mStone);
            Box(hall, "BeDa", center + new Vector3(0, 0.5f, -1.5f), new Vector3(2, 1, 2), mStone);
            Box(hall, "VacMauDen", center + new Vector3(0, 0.45f, 1.8f), new Vector3(2.2f, 0.9f, 2.2f), mBlood);
            Box(hall, "AnDong_Placeholder", center + new Vector3(0, 1.6f, 1.8f), new Vector3(0.4f, 0.4f, 0.4f), mInteract, false);
            Box(hall, "GheGoMun", center + new Vector3(0, 0.5f, -4.2f), new Vector3(0.8f, 1.0f, 0.8f), mDoor);
            var red = new GameObject("CauldronGlow").AddComponent<Light>();
            red.transform.SetParent(hall);
            red.transform.position = center + new Vector3(0, 1.2f, 1.8f);
            red.type = LightType.Point; red.range = 9; red.intensity = 2.2f; red.color = new Color(0.9f, 0.1f, 0.05f); red.shadows = LightShadows.None;
            WorkLight(hall, "Ritual", center + new Vector3(0, 4.2f, 0), 12, 0.6f);
            log.Add("Cellar ok");
        }

        // =====================================================================
        // Hệ thống
        internal static GameObject BuildSystems()
        {
            var sys = new GameObject("KH_GameSystems");
            var gm = sys.AddComponent<GameManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("textTable").objectReferenceValue = data.text;
            so.FindProperty("uiStyle").objectReferenceValue = data.style;
            so.FindProperty("database").objectReferenceValue = data.db;
            so.FindProperty("toolsConfig").objectReferenceValue = data.tools;
            so.ApplyModifiedPropertiesWithoutUndo();

            sys.AddComponent<CheckpointManager>();
            var hud = sys.AddComponent<KhoaHonHUD>();
            hud.SetStyle(data.style);
            sys.AddComponent<DocumentReaderUI>();
            var nm = sys.AddComponent<NotebookManager>();
            var nso = new SerializedObject(nm);
            var start = nso.FindProperty("startingEntries");
            start.arraySize = 1;
            start.GetArrayElementAtIndex(0).objectReferenceValue = data.sources["PHOTO_QuanSent"];
            nso.ApplyModifiedPropertiesWithoutUndo();
            sys.AddComponent<NotebookUI>();
            var ri = sys.AddComponent<RelicInventory>();
            SetArray(ri, "relics", data.relics);
            sys.AddComponent<KeyRing>();
            sys.AddComponent<BatteryPool>();
            sys.AddComponent<FilmStock>();
            var cheat = sys.AddComponent<KhoaHonCheatPanel>();
            SetArray(cheat, "relics", data.relics);
            log.Add("Systems ok");
            return sys;
        }

        // =====================================================================
        // Sao chép Player + UI từ MapKinhDi (KHÔNG sửa/lưu scene nguồn)
        internal static GameObject CopyPlayerRig(Scene source, Scene target, GameObject systems)
        {
            string[] names = { "Player", "Canvas", "DeathScreenManager", "EventSystem", "SpawnPoint" };
            var originals = new List<GameObject>();
            foreach (var root in source.GetRootGameObjects())
                if (names.Contains(root.name)) originals.Add(root);

            var map = new Dictionary<Object, Object>();
            var copies = new List<GameObject>();
            foreach (var o in originals)
            {
                var c = Object.Instantiate(o);
                c.name = o.name;
                SceneManager.MoveGameObjectToScene(c, target);
                copies.Add(c);
                var ot = o.GetComponentsInChildren<Transform>(true);
                var ct = c.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < ot.Length && i < ct.Length; i++)
                {
                    map[ot[i].gameObject] = ct[i].gameObject;
                    var oc = ot[i].GetComponents<Component>();
                    var ccs = ct[i].GetComponents<Component>();
                    for (int k = 0; k < oc.Length && k < ccs.Length; k++) if (oc[k] != null && ccs[k] != null) map[oc[k]] = ccs[k];
                }
            }
            // Sửa tham chiếu chéo trỏ về bản gốc
            int fixedRefs = 0;
            foreach (var c in copies)
                foreach (var comp in c.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var p = so.GetIterator();
                    bool changed = false;
                    while (p.Next(true))
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                        if (map.TryGetValue(p.objectReferenceValue, out var repl)) { p.objectReferenceValue = repl; changed = true; fixedRefs++; }
                    }
                    if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                }
            log.Add("Copied rig: " + string.Join(",", copies.Select(x => x.name)) + " fixedRefs=" + fixedRefs);

            var player = copies.First(x => x.name == "Player");
            var spawn = copies.FirstOrDefault(x => x.name == "SpawnPoint");
            var canvas = copies.FirstOrDefault(x => x.name == "Canvas");
            ConfigurePlayer(player, canvas, spawn, systems, new Vector3(0, 0, -32f), 0f, true);
            return player;
        }

        /// <summary>Gắn hệ thống Khóa Hồn lên Player có sẵn: hotbar chiến dịch, tương tác, pin, khóa ô; đặt tại vị trí cho trước.</summary>
        internal static void ConfigurePlayer(GameObject player, GameObject canvas, GameObject spawn, GameObject systems, Vector3 groundPos, float yaw, bool savePrefab)
        {
            // Font tiếng Việt cho UI cũ (chỉ sửa bản sao trong scene mới)
            if (canvas != null)
                foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true)) t.font = data.style.sansFont;

            // Hotbar chiến dịch: Đèn pin · Máy ghi âm · Máy ảnh · Xà beng (Radar gỡ khỏi hotbar, asset giữ nguyên)
            var hb = player.GetComponent<HotbarInventory>();
            var hso = new SerializedObject(hb);
            var slots = hso.FindProperty("slots");
            slots.arraySize = 4;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = data.flashlight;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = data.recorder;
            slots.GetArrayElementAtIndex(2).objectReferenceValue = data.filmCamera;
            slots.GetArrayElementAtIndex(3).objectReferenceValue = data.crowbar;
            hso.FindProperty("selectedIndex").intValue = 0;
            hso.ApplyModifiedPropertiesWithoutUndo();
            // Gỡ instance Radar có sẵn dưới holder (nếu có) để không bị khớp nhầm
            foreach (var radar in player.GetComponentsInChildren<RadarItem>(true)) Object.DestroyImmediate(radar.gameObject);

            if (player.GetComponent<PlayerInteractor>() == null) player.AddComponent<PlayerInteractor>();
            if (player.GetComponent<FlashlightBatteryDrain>() == null) player.AddComponent<FlashlightBatteryDrain>();
            var gate = player.GetComponent<HotbarGate>();
            if (gate == null) gate = player.AddComponent<HotbarGate>();
            var gso = new SerializedObject(gate);
            var g = gso.FindProperty("gated");
            g.arraySize = 1;
            var e = g.GetArrayElementAtIndex(0);
            e.FindPropertyRelative("index").intValue = 3;
            e.FindPropertyRelative("item").objectReferenceValue = data.crowbar;
            e.FindPropertyRelative("unlockFlag").enumValueIndex = (int)StoryFlag.HasCrowbar;
            gso.ApplyModifiedPropertiesWithoutUndo();

            // Đặt Player ở CP_Gate (chân chạm đất)
            var cc = player.GetComponent<CharacterController>();
            float footOffset = cc != null ? -(cc.center.y - cc.height * 0.5f) + 0.05f : 0.05f;
            player.transform.SetPositionAndRotation(groundPos + Vector3.up * footOffset, Quaternion.Euler(0, yaw, 0));
            if (spawn != null)
            {
                spawn.transform.SetPositionAndRotation(player.transform.position, Quaternion.identity);
                var cm = systems.GetComponent<CheckpointManager>();
                var cmso = new SerializedObject(cm);
                cmso.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
                cmso.ApplyModifiedPropertiesWithoutUndo();
            }
            log.Add($"Player CC h={cc?.height} c={cc?.center} step={cc?.stepOffset} slope={cc?.slopeLimit}");

            if (!savePrefab) return;
            // Prefab KH_Player
            string prefabPath = KhoaHonDataBuilder.Root + "/Prefabs/Characters/KH_Player.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, prefabPath, InteractionMode.AutomatedAction);
            log.Add("Prefab " + prefabPath);
        }

        // =====================================================================
        internal static void BuildCheckpointsAndTriggers(Transform level, Doors doors)
        {
            var cps = Group(level, "Checkpoints");
            Cp(cps, "CP_Gate", new Vector3(0, 0, -32f), 0, false, GameStage.Stage0_Gate);
            Cp(cps, "CP_Hall", new Vector3(0, 0, -6.5f), 0, true, GameStage.Stage1_GroundFloor);
            Cp(cps, "CP_Floor2Landing", new Vector3(3.1f, F2, 0.8f), -90, true, GameStage.Stage2_Floor2);
            var shrine = Cp(cps, "CP_Shrine", new Vector3(0, F2, 3.2f), 0, false, GameStage.Stage2_Floor2);
            shrine.GetComponent<BoxCollider>().enabled = false; // kích hoạt bằng code khi đặt bùa đầu tiên (Phase 2)
            Cp(cps, "CP_CellarStairs", new Vector3(-6.5f, -10f, 4.0f), 180, true, GameStage.Stage3_Cellar);
            Cp(cps, "CP_RitualHall", new Vector3(-6.5f, -10f, -7.2f), 180, true, GameStage.Stage4_Ritual);

            // Cửa chính sập khóa sau lưng khi vào Sảnh
            var t = new GameObject("Trigger_FrontDoorSlam");
            t.transform.SetParent(level);
            t.transform.position = new Vector3(0, 0, -6.8f);
            var bc = t.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.size = new Vector3(7, 2.5f, 1.2f); bc.center = new Vector3(0, 1.25f, 0);
            var st = t.AddComponent<StoryTrigger>();
            st.Configure(new[] { StoryFlag.EnteredHouse }, StoryFlag.EnteredHouse, true, GameStage.Stage1_GroundFloor, doors.front, "sub.front_door_slam");
            var dso = new SerializedObject(doors.front);
            dso.FindProperty("lockedTextKey").stringValue = "prompt.main_door_locked";
            dso.ApplyModifiedPropertiesWithoutUndo();

            // Lên tầng 2 lần đầu
            var t2 = new GameObject("Trigger_Floor2Entered");
            t2.transform.SetParent(level);
            t2.transform.position = new Vector3(3.1f, F2, 0.8f);
            var bc2 = t2.AddComponent<BoxCollider>();
            bc2.isTrigger = true; bc2.size = new Vector3(2, 2.5f, 1.4f); bc2.center = new Vector3(0, 1.25f, 0);
            t2.AddComponent<StoryTrigger>().Configure(new[] { StoryFlag.Floor2Entered }, StoryFlag.Floor2Entered, true, GameStage.Stage2_Floor2, null, "");
            log.Add("Checkpoints ok");
        }

        internal static Checkpoint Cp(Transform parent, string id, Vector3 pos, float yaw, bool onEnter, GameStage stage)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.size = new Vector3(3, 2.5f, 3); bc.center = new Vector3(0, 1.25f, 0);
            var cp = go.AddComponent<Checkpoint>();
            cp.Configure(id, onEnter, onEnter, stage);
            return cp;
        }

        // =====================================================================
        // NavMesh
        internal static int EnsureNavArea(string name, int index)
        {
            var settings = Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings");
            var so = new SerializedObject(settings);
            var areas = so.FindProperty("areas");
            if (areas == null) { log.Add("WARN: không tìm thấy NavMesh areas"); return index; }
            var a = areas.GetArrayElementAtIndex(index);
            var n = a.FindPropertyRelative("name");
            if (n.stringValue != name)
            {
                if (!string.IsNullOrEmpty(n.stringValue)) log.Add("WARN: area " + index + " đang là '" + n.stringValue + "', ghi đè thành " + name);
                n.stringValue = name;
                a.FindPropertyRelative("cost").floatValue = 1f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            log.Add("NavMesh area " + index + " = " + NavMesh.GetAreaFromName(name));
            return index;
        }

        internal static void BakeNavMesh(Scene scene, int floor2Area)
        {
            var go = new GameObject("KH_NavMesh");
            var surf = go.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.All;
            surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.layerMask = ~(1 << 5);
            // Bỏ cửa, vật nhặt, trigger khỏi bake
            foreach (var d in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (d.gameObject.scene == scene) d.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (p.gameObject.scene == scene) p.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.gameObject.scene == scene && c.isTrigger && c.GetComponent<NavMeshModifier>() == null)
                    c.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var pl = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Player");
            if (pl != null) pl.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            // Area Floor2: tầng 2 + nửa trên cầu thang (bà Quản Gia chỉ đi được ở đây)
            var vol = new GameObject("NavArea_Floor2").AddComponent<NavMeshModifierVolume>();
            vol.transform.SetParent(go.transform);
            vol.transform.position = new Vector3(0, 4.8f, 0);
            vol.size = new Vector3(24.4f, 6.0f, 18.4f);
            vol.area = floor2Area;

            surf.BuildNavMesh();
            string path = KhoaHonDataBuilder.Root + "/Scenes/KH_Main_NavMesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surf.navMeshData, path);
            surf.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            var tri = NavMesh.CalculateTriangulation();
            int f2 = tri.areas.Count(a => a == floor2Area);
            log.Add($"NavMesh baked: tris={tri.areas.Length} floor2Tris={f2}");
        }

        internal static void AddToBuildSettings(string path)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == path)) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            log.Add("Added to Build Settings");
        }

        // =====================================================================
        // Hình học
        internal enum Axis { X, Z }

        internal struct Opening { public float center, width, height, sill; }
        internal static Opening Op(float center, float width, float height, float sill = 0f) => new Opening { center = center, width = width, height = height, sill = sill };

        internal static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            if (parent != null) g.SetParent(parent, false);
            return g;
        }

        internal static Transform Room(Transform parent, string id, string label, Vector3 center, Vector3 size)
        {
            var g = Group(parent, id);
            g.position = center;
            var rm = g.gameObject.AddComponent<RoomMarker>();
            rm.displayName = label; rm.size = size;
            return g;
        }

        internal static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true, bool keepWorld = false)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, keepWorld);
            g.transform.position = keepWorld ? parent.TransformPoint(pos) : pos;
            if (keepWorld) g.transform.localPosition = pos;
            g.transform.localScale = size;
            g.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            g.isStatic = collider;
            return g.transform;
        }

        /// <summary>Tường dọc trục X (từ x0 tới x1 tại z), có ô cửa/cửa sổ.</summary>
        internal static void WallX(Transform parent, string name, float x0, float x1, float z, float y, float h, Opening[] ops)
        {
            var g = Group(parent, name);
            foreach (var seg in Segments(x0, x1, y, h, ops))
                Box(g, name + "_seg", new Vector3((seg.x + seg.z) / 2f, (seg.y + seg.w) / 2f, z), new Vector3(seg.z - seg.x, seg.w - seg.y, Wt), mWall);
        }

        /// <summary>Tường dọc trục Z (từ z0 tới z1 tại x).</summary>
        internal static void WallZ(Transform parent, string name, float z0, float z1, float x, float y, float h, Opening[] ops)
        {
            var g = Group(parent, name);
            foreach (var seg in Segments(z0, z1, y, h, ops))
                Box(g, name + "_seg", new Vector3(x, (seg.y + seg.w) / 2f, (seg.x + seg.z) / 2f), new Vector3(Wt, seg.w - seg.y, seg.z - seg.x), mWall);
        }

        /// <summary>Chia tường thành các khối (a0, y0, a1, y1) tránh ô mở.</summary>
        internal static List<Vector4> Segments(float a0, float a1, float y, float h, Opening[] ops)
        {
            var res = new List<Vector4>();
            var sorted = (ops ?? new Opening[0]).OrderBy(o => o.center).ToList();
            float cur = a0;
            foreach (var o in sorted)
            {
                float l = o.center - o.width / 2f, r = o.center + o.width / 2f;
                if (l > cur) res.Add(new Vector4(cur, y, l, y + h));
                if (o.sill > 0f) res.Add(new Vector4(l, y, r, y + o.sill));
                if (o.sill + o.height < h) res.Add(new Vector4(l, y + o.sill + o.height, r, y + h));
                cur = r;
            }
            if (cur < a1) res.Add(new Vector4(cur, y, a1, y + h));
            return res;
        }

        /// <summary>Tấm sàn (Rect x,z,w,d) tại cao độ y (mặt dưới), chừa các lỗ.</summary>
        internal static void Slab(Transform parent, string name, Rect area, float y, float thick, Material mat, Rect[] holes)
        {
            var g = Group(parent, name);
            var xs = new List<float> { area.xMin, area.xMax };
            var zs = new List<float> { area.yMin, area.yMax };
            if (holes != null) foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); zs.Add(h.yMin); zs.Add(h.yMax); }
            xs = xs.Distinct().OrderBy(v => v).ToList();
            zs = zs.Distinct().OrderBy(v => v).ToList();
            // Gộp theo dải x để giảm số khối
            for (int i = 0; i < xs.Count - 1; i++)
            {
                float zStart = float.NaN;
                for (int j = 0; j < zs.Count - 1; j++)
                {
                    var cell = new Rect(xs[i], zs[j], xs[i + 1] - xs[i], zs[j + 1] - zs[j]);
                    bool inHole = holes != null && holes.Any(h => h.Contains(cell.center));
                    if (!inHole && float.IsNaN(zStart)) zStart = zs[j];
                    bool endRun = inHole || j == zs.Count - 2;
                    if (endRun && !float.IsNaN(zStart))
                    {
                        float zEnd = inHole ? zs[j] : zs[j + 1];
                        Box(g, name + "_part", new Vector3((xs[i] + xs[i + 1]) / 2f, y + thick / 2f, (zStart + zEnd) / 2f),
                            new Vector3(xs[i + 1] - xs[i], thick, zEnd - zStart), mat);
                        zStart = float.NaN;
                    }
                }
            }
        }

        /// <summary>Cầu thang: bậc hiển thị (không collider) + dốc collider vô hình để đi mượt.</summary>
        internal static void Stairs(Transform parent, string name, Vector3 bottom, Vector3 top, float width, int steps, Material mat)
        {
            var g = Group(parent, name);
            var flat = new Vector3(top.x - bottom.x, 0, top.z - bottom.z);
            float run = flat.magnitude, rise = top.y - bottom.y;
            var dir = flat.normalized;
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            float stepRun = run / steps, stepRise = rise / steps;
            for (int i = 0; i < steps; i++)
            {
                float yTop = bottom.y + stepRise * (i + 1);
                float yBase = Mathf.Min(bottom.y, top.y) - 0.1f;
                if (rise < 0) yBase = yTop - 0.3f;
                var center = bottom + dir * (stepRun * (i + 0.5f));
                float hgt = rise > 0 ? yTop - yBase : 0.3f;
                var s = Box(g, "Step_" + i, new Vector3(center.x, yBase + hgt / 2f, center.z), Vector3.one, mat, false);
                s.rotation = rot;
                s.localScale = new Vector3(width, hgt, stepRun + 0.01f);
            }
            // Dốc
            float len = Mathf.Sqrt(run * run + rise * rise);
            float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var mid = (bottom + top) / 2f;
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name + "_Ramp";
            ramp.transform.SetParent(g, false);
            ramp.transform.rotation = rot * Quaternion.Euler(-ang, 0, 0);
            ramp.transform.localScale = new Vector3(width, 0.1f, len + 0.2f);
            ramp.transform.position = mid - ramp.transform.up * 0.05f + Vector3.up * (rise > 0 ? 0f : 0.02f);
            Object.DestroyImmediate(ramp.GetComponent<MeshRenderer>());
            ramp.isStatic = true;
        }

        internal static DoorInteractable Door(Transform parent, string id, Vector3 hingePos, float width, float height, bool locked, string key, string lockedKey, string keyNameKey, Axis axis)
        {
            var pivot = new GameObject(id);
            pivot.transform.SetParent(parent, true);
            pivot.transform.position = hingePos;
            pivot.transform.rotation = axis == Axis.X ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(pivot.transform, false);
            leaf.transform.localPosition = new Vector3(width / 2f, height / 2f, 0);
            leaf.transform.localScale = new Vector3(width - 0.02f, height - 0.02f, 0.07f);
            leaf.GetComponent<MeshRenderer>().sharedMaterial = locked ? mInteract : mDoor;
            var door = pivot.AddComponent<DoorInteractable>();
            door.Configure(id, locked, key, lockedKey, keyNameKey);
            var so = new SerializedObject(door);
            so.FindProperty("openClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/wood_creak_01.wav") ?? FindClip("wood_creak_01");
            so.FindProperty("closeClip").objectReferenceValue = FindClip("wood_creak_02");
            so.FindProperty("lockedClip").objectReferenceValue = FindClip("wood_creak_03");
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        internal static AudioClip FindClip(string name)
        {
            var guid = AssetDatabase.FindAssets(name + " t:AudioClip").FirstOrDefault();
            return guid != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)) : null;
        }

        internal static Pickup PickupBox(Transform parent, string id, Vector3 pos, Vector3 size, string nameKey,
            string key = "", float battery = 0f, int film = 0, StoryFlag[] rewards = null, NotebookSource[] sources = null, bool openDoc = false, string hint = "")
        {
            var t = Box(parent, id, pos, size, mInteract);
            t.gameObject.isStatic = false;
            var col = t.GetComponent<BoxCollider>();
            col.isTrigger = true;
            // Collider to hơn hình để dễ nhắm
            col.size = new Vector3(Mathf.Max(1f, 0.25f / size.x), Mathf.Max(1f, 0.25f / size.y), Mathf.Max(1f, 0.25f / size.z));
            var p = t.gameObject.AddComponent<Pickup>();
            p.Configure(id, nameKey);
            p.SetRewards(key, battery, film, rewards, sources, openDoc, hint);
            return p;
        }

        internal static ReadableDocument Readable(Transform parent, string name, Vector3 pos, Vector3 size, string docId)
        {
            var t = Box(parent, name, pos, size, mInteract);
            var col = t.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(Mathf.Max(1f, 0.25f / size.x), Mathf.Max(1f, 0.25f / size.y), Mathf.Max(1f, 0.25f / size.z));
            var r = t.gameObject.AddComponent<ReadableDocument>();
            r.SetDocument((DocumentData)data.sources[docId]);
            return r;
        }

        internal static void SetArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
