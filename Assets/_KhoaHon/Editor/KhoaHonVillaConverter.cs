using System.Collections.Generic;
using System.IO;
using System.Linq;
using KhoaHon.Core;
using KhoaHon.Interaction;
using KhoaHon.Notebook;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using GB = KhoaHon.EditorTools.KhoaHonGreyboxBuilder;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Hướng B: chuyển MapKinhDi_Ver1 thành KH_Main (bản sao, không sửa file gốc).
    /// Biệt thự Villa1: sàn tầng trệt y=11, tầng 2 y=18, khung x[66.7,110.7] z[7.5,27.5], giếng trời x[82.7,94.7] z[11.5,23.5].
    /// Bố cục phòng: xem Docs/KhoaHon/PLAN.md mục "Hướng B".
    /// </summary>
    public static class KhoaHonVillaConverter
    {
        const string SourcePath = "Assets/MapKinhDi/MapKinhDi_Ver1.unity";
        const string GreyboxPath = "Assets/_KhoaHon/Scenes/KH_Greybox.unity";
        const string ModelsDir = "Assets/_KhoaHon/Models/Props";
        const string V1 = "Assets/Flooded_Grounds/Prefabs/Buildings/Villa1/";
        const float GF = 11f, F2 = 18f;

        static readonly List<string> log = new List<string>();
        static KhoaHonDataBuilder.Result data;
        static Transform root;
        static Material wallMat;

        [MenuItem("KhoaHon/Build/3. Convert MapKinhDi → KH_Main (Hướng B)")]
        public static void Menu() => Debug.Log(Convert());

        public static string Convert()
        {
            log.Clear();
            if (EditorApplication.isPlaying) return "Đang Play.";

            data = KhoaHonDataBuilder.Build();
            GB.Init(data);
            ImportBlenderModels();
            ImportIconsAndHeldModels();
            int floor2Area = GB.EnsureNavArea("Floor2", 3);

            // Giữ lại greybox để tham khảo
            if (File.Exists(GB.ScenePath) && !File.Exists(GreyboxPath))
            {
                var cur = SceneManager.GetActiveScene();
                if (cur.path == GB.ScenePath) EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
                log.Add("Greybox → " + GreyboxPath + ": " + (AssetDatabase.MoveAsset(GB.ScenePath, GreyboxPath) == "" ? "ok" : "fail"));
                AssetDatabase.MoveAsset("Assets/_KhoaHon/Scenes/KH_Main_NavMesh.asset", "Assets/_KhoaHon/Scenes/KH_Greybox_NavMesh.asset");
            }

            // Bản sao MapKinhDi (đọc file gốc từ đĩa, không lưu đè)
            var src = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(src, GB.ScenePath, true);
            var scene = EditorSceneManager.OpenScene(GB.ScenePath, OpenSceneMode.Single);
            root = new GameObject("KH_Conversion").transform;
            wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Materials/BLD_Villa1.mat")
                      ?? FindMaterial("BLD_Villa1");

            RemoveDemo();
            var systems = GB.BuildSystems();
            var player = GameObject.Find("Player");
            GB.ConfigurePlayer(player, GameObject.Find("Canvas"), GameObject.Find("SpawnPoint"), systems, new Vector3(80.7f, 10f, -6f), 0f, false);

            var doors = ConvertDoors();
            BuildFloor2Partitions();
            BuildExteriorBlockers();
            BuildKitchenHatchAndCellar();
            PlaceContent(doors);
            BuildCheckpoints(doors);
            AddAtmosphere();
            PatchFloorHoles();
            ClearStairBarricade();
            BuildStairRamps();
            BakeNavMesh(scene, floor2Area);
            log.Add(KhoaHonHousekeeperBuilder.Build(root, F2));
            log.Add(KhoaHonHorrorBuilder.Build(root, CellarOrigin));
            BakeReflectionProbes();

            EditorSceneManager.SaveScene(scene);
            FixBuildSettings();
            AssetDatabase.SaveAssets();
            log.Add("Saved " + GB.ScenePath);
            return string.Join("\n", log);
        }

        // =====================================================================
        static void ImportBlenderModels()
        {
            KhoaHonDataBuilder.EnsureFolder(ModelsDir);
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/Blender/out"));
            if (!Directory.Exists(outDir)) { log.Add("WARN: chưa có Tools/Blender/out"); return; }
            int n = 0;
            foreach (var f in Directory.GetFiles(outDir, "*.fbx"))
            {
                string dst = ModelsDir + "/" + Path.GetFileName(f);
                File.Copy(f, dst, true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                var imp = (ModelImporter)AssetImporter.GetAtPath(dst);
                if (imp != null)
                {
                    imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    imp.animationType = ModelImporterAnimationType.None;
                    imp.importAnimation = false;
                    imp.addCollider = false;
                    imp.SaveAndReimport();
                    // Tách vật liệu nhúng thành file .mat dùng chung (URP Lit) để chỉnh được
                    KhoaHonDataBuilder.EnsureFolder(ModelsDir + "/Materials");
                    foreach (var m in AssetDatabase.LoadAllAssetsAtPath(dst).OfType<Material>().ToArray())
                    {
                        string mp = ModelsDir + "/Materials/" + m.name + ".mat";
                        var ex = AssetDatabase.LoadAssetAtPath<Material>(mp);
                        if (ex == null) { ex = new Material(m); AssetDatabase.CreateAsset(ex, mp); }
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(m), ex);
                    }
                    imp.SaveAndReimport();
                }
                n++;
            }
            log.Add("Imported Blender models: " + n);
        }

        /// <summary>Icon hotbar render từ Blender + thay khối primitive của vật cầm tay bằng model Blender.</summary>
        static void ImportIconsAndHeldModels()
        {
            string iconSrc = Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/Blender/out/icons"));
            string iconDir = "Assets/_KhoaHon/Textures/Icons";
            KhoaHonDataBuilder.EnsureFolder(iconDir);
            if (Directory.Exists(iconSrc))
                foreach (var f in Directory.GetFiles(iconSrc, "*.png"))
                {
                    string dst = iconDir + "/" + Path.GetFileName(f);
                    File.Copy(f, dst, true);
                    AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                    var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
                    ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
            void Icon(HorrorGame.Inventory.ItemData item, string model)
            {
                if (item == null) return;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(iconDir + "/" + model + ".png");
                var so = new SerializedObject(item);
                so.FindProperty("icon").objectReferenceValue = sp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Icon(data.recorder, "KH_MayGhiAm"); Icon(data.filmCamera, "KH_MayAnh"); Icon(data.crowbar, "KH_XaBeng");

            void Held(string prefabPath, string model, Vector3 euler, string ledChild)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                var m = Model(model);
                if (m != null)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(m, root.transform);
                    g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.Euler(euler);
                    foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (ledChild != null)
                    {
                        var led = g.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name.Contains(ledChild));
                        var rec = root.GetComponent<KhoaHon.Items.RecorderItem>();
                        if (rec != null) { var so = new SerializedObject(rec); so.FindProperty("ledRenderer").objectReferenceValue = led; so.ApplyModifiedPropertiesWithoutUndo(); }
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            Held("Assets/_KhoaHon/Prefabs/Items/KH_Recorder.prefab", "KH_MayGhiAm", new Vector3(0, 180, 0), "LED");
            Held("Assets/_KhoaHon/Prefabs/Items/KH_FilmCamera.prefab", "KH_MayAnh", new Vector3(0, 180, 0), null);
            Held("Assets/_KhoaHon/Prefabs/Items/KH_Crowbar.prefab", "KH_XaBeng", Vector3.zero, null);
            log.Add("Icons + held models updated");
        }

        /// <summary>Không khí: đèn chập chờn hành lang tầng 2 (PeriodicFlickerLight có sẵn), ánh trăng lạnh qua cửa sổ sảnh.</summary>
        static void AddAtmosphere()
        {
            var g = new GameObject("KH_Atmosphere").transform;
            g.SetParent(root);
            foreach (var p in new[] { new Vector3(80.7f, 22.3f, 25.5f), new Vector3(80.7f, 22.3f, 9.5f), new Vector3(98.7f, 22.3f, 9.5f), new Vector3(98.7f, 22.3f, 25.5f) })
            {
                var l = new GameObject("Flicker_F2").AddComponent<Light>();
                l.transform.SetParent(g); l.transform.position = p;
                // Đèn treo tầng 2: bóng đổ mềm để hành lang có khối, tán xạ trong sương
                l.type = LightType.Point; l.range = 8.5f; l.intensity = 0.9f; l.color = new Color(1f, 0.70f, 0.42f);
                l.shadows = LightShadows.Soft; l.shadowStrength = 0.9f; l.shadowNearPlane = 0.2f;
                l.gameObject.AddComponent<HorrorGame.Lighting.PeriodicFlickerLight>();
            }
            foreach (var p in new[] { new Vector3(88.7f, 16f, 5.5f), new Vector3(72.7f, 16f, 5.5f), new Vector3(100.7f, 16f, 5.5f) })
            {
                var l = new GameObject("MoonShaft").AddComponent<Light>();
                l.transform.SetParent(g); l.transform.position = p;
                l.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.55f, 1f));
                l.type = LightType.Spot; l.spotAngle = 38f; l.innerSpotAngle = 12f; l.range = 16f; l.intensity = 1.6f;
                l.color = new Color(0.55f, 0.66f, 0.95f); l.shadows = LightShadows.Soft; l.shadowStrength = 0.85f;
            }
            TuneSceneLighting();
            log.Add("Atmosphere lights added");
        }

        /// <summary>
        /// Ánh sáng tổng thể kiểu "một nguồn sáng": trăng lạnh có bóng mềm (tạo tia sáng qua cửa sổ trong sương),
        /// môi trường gần như đen, phản chiếu thấp. Gỡ LightingSettings/LightingData cũ trỏ vào asset pack.
        /// </summary>
        static void TuneSceneLighting()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.color = new Color(0.52f, 0.62f, 0.86f);
                l.intensity = Mathf.Clamp(l.intensity, 0.18f, 0.35f);
                l.shadows = LightShadows.Soft; l.shadowStrength = 1f;
                l.lightmapBakeType = LightmapBakeType.Realtime;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.010f, 0.013f, 0.02f);
            RenderSettings.reflectionIntensity = 0.25f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f; // sương thể tích lo phần chính, fog chuẩn chỉ làm xa mờ
            RenderSettings.fogColor = new Color(0.02f, 0.028f, 0.04f);

            // Không ghi vào LightingSettings/LightingData của Flooded_Grounds
            string lsPath = "Assets/_KhoaHon/Settings/KH_LightingSettings.lighting";
            KhoaHonDataBuilder.EnsureFolder("Assets/_KhoaHon/Settings");
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
            if (ls == null) { ls = new LightingSettings { name = "KH_LightingSettings" }; AssetDatabase.CreateAsset(ls, lsPath); }
            ls.bakedGI = false; ls.realtimeGI = false;
            Lightmapping.lightingSettings = ls;
            Lightmapping.lightingDataAsset = null;
            log.Add("Scene lighting tuned (moon soft shadows, own LightingSettings)");
        }

        /// <summary>
        /// Bake từng reflection probe thành cubemap riêng (chế độ Custom) trong Assets/_KhoaHon/Textures/Probes,
        /// không cần LightingData và không ghi vào asset pack.
        /// </summary>
        static void BakeReflectionProbes()
        {
            const string dir = "Assets/_KhoaHon/Textures/Probes";
            KhoaHonDataBuilder.EnsureFolder(dir);
            var probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            int ok = 0;
            for (int i = 0; i < probes.Length; i++)
            {
                var p = probes[i];
                p.resolution = 128;
                p.hdr = true;
                p.boxProjection = true;
                p.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
                string path = $"{dir}/KH_Probe_{i:00}.exr";
                if (Lightmapping.BakeReflectionProbe(p, path))
                {
                    AssetDatabase.ImportAsset(path);
                    var cube = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    p.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
                    p.customBakedTexture = cube;
                    ok++;
                }
            }
            log.Add($"Reflection probes baked: {ok}/{probes.Length}");
        }

        static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModelsDir + "/" + name + ".fbx");

        static GameObject Place(GameObject prefabOrModel, Transform parent, Vector3 pos, float yaw, float scale = 1f, string rename = null)
        {
            if (prefabOrModel == null) { log.Add("WARN missing prefab for " + rename); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabOrModel, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale *= scale;
            if (rename != null) go.name = rename;
            return go;
        }

        static GameObject P(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        static Material FindMaterial(string name)
        {
            var g = AssetDatabase.FindAssets(name + " t:Material").FirstOrDefault();
            return g != null ? AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)) : null;
        }

        // =====================================================================
        static void RemoveDemo()
        {
            foreach (var n in new[] { "Ghost" })
            {
                var g = GameObject.Find(n);
                if (g != null) { Object.DestroyImmediate(g); log.Add("Removed demo " + n); }
            }
            var nm = GameObject.Find("NavMesh_Manager");
            if (nm != null) Object.DestroyImmediate(nm);
            // Collider mặt đất phẳng (y=10) được thay bằng khối hộp để khoét lỗ cầu thang hầm
            var ground = GameObject.Find("Ground_North_Full");
            if (ground != null)
            {
                var mc = ground.GetComponent<MeshCollider>();
                var b = ground.GetComponent<Renderer>().bounds;
                if (mc != null) mc.enabled = false;
                var gc = new GameObject("KH_GroundCollider").transform;
                gc.SetParent(root);
                var hole = new Rect(69.0f, 24.6f, 5.9f, 1.8f);
                void G(string n, float x0, float x1, float z0, float z1)
                {
                    var go = new GameObject(n); go.transform.SetParent(gc);
                    // Mỏng 0.1 m: khối dày sẽ thành "trần" thấp trong đường hầm cầu thang hầm
                    go.transform.position = new Vector3((x0 + x1) / 2f, 9.95f, (z0 + z1) / 2f);
                    var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(x1 - x0, 0.1f, z1 - z0);
                    go.isStatic = true;
                }
                G("W", b.min.x, hole.xMin, b.min.z, b.max.z);
                G("E", hole.xMax, b.max.x, b.min.z, b.max.z);
                G("S", hole.xMin, hole.xMax, b.min.z, hole.yMin);
                G("N", hole.xMin, hole.xMax, hole.yMax, b.max.z);
                log.Add("Ground collider replaced (hole for cellar)");
            }
        }

        // =====================================================================
        // Cửa: chuyển cánh Villa1_Door_* có sẵn thành DoorInteractable
        class DoorSet
        {
            public DoorInteractable frontA, frontB, stair;
            public List<DoorInteractable> all = new List<DoorInteractable>();
        }

        static readonly float[] xLines = { 66.7f, 78.7f, 82.7f, 94.7f, 102.7f, 106.7f, 110.7f };

        static DoorSet ConvertDoors()
        {
            var set = new DoorSet();
            var group = new GameObject("KH_Doors").transform;
            group.SetParent(root);
            var leaves = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("Villa1_Door") && t.position.x > 60 && t.position.x < 116 && t.position.z > 4 && t.position.z < 31)
                .ToList();
            var kept = new List<Transform>();
            foreach (var l in leaves)
            {
                if (kept.Any(k => Vector3.Distance(k.position, l.position) < 0.2f)) { Object.DestroyImmediate(l.gameObject); continue; }
                kept.Add(l);
            }
            var clipOpen = GB.FindClip("wood_creak_01"); var clipClose = GB.FindClip("wood_creak_02"); var clipLocked = GB.FindClip("wood_creak_03");
            foreach (var l in kept)
            {
                bool onXLine = xLines.Any(x => Mathf.Abs(l.position.x - x) < 0.4f);
                float yaw = l.eulerAngles.y;
                float closed = onXLine ? Nearest(yaw, 90f, 270f) : Nearest(yaw, 0f, 180f);
                var pivot = new GameObject("Door_" + kept.IndexOf(l)).transform;
                pivot.SetParent(group);
                pivot.SetPositionAndRotation(l.position, Quaternion.Euler(0, closed, 0));
                l.SetParent(pivot, true);
                l.localRotation = Quaternion.identity;
                l.localPosition = Vector3.zero;
                if (l.GetComponent<Collider>() is MeshCollider mc) mc.convex = false;
                var door = pivot.gameObject.AddComponent<DoorInteractable>();
                door.SetClips(clipOpen, clipClose, clipLocked);
                pivot.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                var c = l.GetComponentInChildren<Renderer>().bounds.center;
                string id = $"Door_{Mathf.RoundToInt(c.x)}_{Mathf.RoundToInt(c.y)}_{Mathf.RoundToInt(c.z)}";
                pivot.name = id;
                // Vai trò theo vị trí ô cửa
                bool gf = c.y < 16f;
                if (gf && Mathf.Abs(c.z - 7.5f) < 0.6f) // cửa chính đôi
                {
                    door.Configure(id, false, "", "prompt.main_door_locked", "");
                    if (set.frontA == null) set.frontA = door; else set.frontB = door;
                }
                else if (gf && Mathf.Abs(c.x - 94.7f) < 0.6f && c.z > 18f)
                {
                    door.Configure(id, true, "key_upstairs", "prompt.stair_door_locked", "key.upstairs");
                    set.stair = door;
                }
                else if (gf && Mathf.Abs(c.z - 15.5f) < 0.6f && c.x > 94f)
                    door.Configure(id, true, "", "prompt.door_stuck", "");
                else if (Mathf.Abs(c.z - 27.5f) < 0.7f) // cửa sau / ban công
                    door.Configure(id, true, "", "prompt.locked_outside", "");
                else
                    door.Configure(id, false, "", "prompt.locked", "");
                set.all.Add(door);
            }
            if (set.frontA != null && set.frontB != null) { set.frontA.SetLinked(set.frontB); set.frontB.SetLinked(set.frontA); }
            log.Add($"Doors converted: {set.all.Count} (front={(set.frontA != null)}/{(set.frontB != null)} stair={(set.stair != null)})");
            return set;
        }

        static float Nearest(float yaw, float a, float b)
        {
            float da = Mathf.Abs(Mathf.DeltaAngle(yaw, a)), db = Mathf.Abs(Mathf.DeltaAngle(yaw, b));
            return da <= db ? a : b;
        }

        // =====================================================================
        // Tầng 2: chia phòng bằng mảnh tường gốc Wall_Int_A (đặc) / Wall_Int_B (có ô cửa), lưới 4 m
        // Quy tắc pivot: rotY 0 → đoạn đi +x; 90 → -z; 180 → -x; 270 → +z (pivot ở đầu đoạn)
        static void BuildFloor2Partitions()
        {
            var g = new GameObject("KH_Floor2_Partitions").transform;
            g.SetParent(root);
            // Gỡ vách cũ cắt hành lang vòng phía đông nam + lan can phía đông lỗ cầu thang (thay bằng tường)
            int removed = 0;
            foreach (Transform t in GameObject.Find("Map").transform.Cast<Transform>().ToArray())
            {
                var s = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                string n = s != null ? s.name : t.name;
                var p = t.position;
                bool wallZ115 = n.StartsWith("Villa1_Wall_Int") && Mathf.Abs(p.y - F2) < 0.5f && Mathf.Abs(p.z - 11.5f) < 0.3f && (Mathf.Abs(p.x - 98.7f) < 0.3f || Mathf.Abs(p.x - 102.7f) < 0.3f);
                bool railE = n.StartsWith("Villa1_Deco_Handrail1_Mid") && Mathf.Abs(p.y - F2) < 0.5f && Mathf.Abs(p.x - 106.7f) < 0.3f && p.z > 12f && p.z < 23f;
                if (wallZ115 || railE) { Object.DestroyImmediate(t.gameObject); removed++; }
            }
            var A = P(V1 + "Villa1_Wall_Int_A.prefab");
            var B = P(V1 + "Villa1_Wall_Int_B.prefab");
            void W(GameObject pf, float x, float z, float rot) => Place(pf, g, new Vector3(x, F2, z), rot);
            // Cánh tây: Trẻ Em z[19.5,27.5] | Phòng Thờ z[15.5,19.5] | Gia Nhân z[7.5,15.5]
            foreach (var x in new[] { 66.7f, 70.7f, 74.7f }) { W(A, x, 19.5f, 0); W(A, x, 15.5f, 0); }
            // Ngủ Chủ Nhân x[102.7,110.7] z[7.5,11.5]: cửa ra hành lang vòng (x=102.7, z 9.5)
            W(B, 102.7f, 7.5f, 270);
            // Ngủ ↔ Thư Viện (z=11.5, x 108.7)
            W(B, 106.7f, 11.5f, 0);
            // Thư Viện x[106.7,110.7] z[11.5,23.5]: tường ngăn với lỗ cầu thang
            foreach (var z in new[] { 11.5f, 15.5f, 19.5f }) W(A, 106.7f, z, 270);
            // Thư Viện ↔ Khách Trọ (z=23.5, x 108.7)
            W(B, 106.7f, 23.5f, 0);
            // Khách Trọ x[106.7,110.7] z[23.5,27.5]: cửa ra chiếu nghỉ đầu cầu thang (x=106.7, z 25.5)
            W(B, 106.7f, 23.5f, 270);

            // Cánh cửa mới cho các ô cửa Wall_Int_B vừa thêm (Villa1_Door_A, cánh trải về -x cục bộ)
            var leaf = P(V1 + "Villa1_Door_A.prefab");
            var dg = GameObject.Find("KH_Doors").transform;
            void D(string id, Vector3 center, float rot)
            {
                var right = Quaternion.Euler(0, rot, 0) * Vector3.right;
                var pivot = new GameObject(id).transform;
                pivot.SetParent(dg);
                pivot.SetPositionAndRotation(center + right * 0.78f, Quaternion.Euler(0, rot, 0));
                var l = (GameObject)PrefabUtility.InstantiatePrefab(leaf, pivot);
                l.transform.localPosition = Vector3.zero; l.transform.localRotation = Quaternion.identity;
                var door = pivot.gameObject.AddComponent<DoorInteractable>();
                door.Configure(id, false, "", "prompt.locked", "");
                door.SetClips(GB.FindClip("wood_creak_01"), GB.FindClip("wood_creak_02"), GB.FindClip("wood_creak_03"));
                pivot.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            }
            D("Door_NguChuNhan", new Vector3(102.7f, F2, 9.5f), 270);
            D("Door_Ngu_ThuVien", new Vector3(108.7f, F2, 11.5f), 0);
            D("Door_ThuVien_KhachTro", new Vector3(108.7f, F2, 23.5f), 0);
            D("Door_KhachTro", new Vector3(106.7f, F2, 25.5f), 270);
            log.Add($"Floor2 partitions built (removed {removed} old pieces)");
        }

        // =====================================================================
        static void BuildExteriorBlockers()
        {
            var g = new GameObject("KH_ExteriorBlockers").transform;
            g.SetParent(root);
            // Khe hở tường đông phòng cầu thang + giới hạn sân (Luật Giữ Mồi; Phase 3 thay bằng sương mù)
            void B(string n, Vector3 c, Vector3 s)
            {
                var go = new GameObject(n); go.transform.SetParent(g); go.transform.position = c;
                go.AddComponent<BoxCollider>().size = s; go.isStatic = true;
            }
            B("Block_EastGap", new Vector3(110.9f, 13.5f, 21.5f), new Vector3(0.6f, 5f, 4.2f));
            B("Bound_W", new Vector3(42f, 14f, 12f), new Vector3(1f, 10f, 80f));
            B("Bound_E", new Vector3(124f, 14f, 12f), new Vector3(1f, 10f, 80f));
            B("Bound_N", new Vector3(83f, 14f, 50f), new Vector3(84f, 10f, 1f));
            B("Bound_S", new Vector3(83f, 14f, -26f), new Vector3(84f, 10f, 1f));
        }

        // =====================================================================
        // Nắp hầm trong Nhà Bếp (ô sàn x[70.7,74.7] z[23.5,27.5]) + hầm dời từ greybox
        static readonly Vector3 CellarOrigin = new Vector3(65.2f, GF, 20.3f);

        static void BuildKitchenHatchAndCellar()
        {
            var k = new GameObject("KH_Kitchen_Hatch").transform;
            k.SetParent(root);
            // Gỡ ô sàn 4×4 dưới nắp hầm
            foreach (Transform t in GameObject.Find("Map").transform.Cast<Transform>().ToArray())
            {
                var s = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                string n = s != null ? s.name : t.name;
                if (!n.StartsWith("Villa1_Floor")) continue;
                var r = t.GetComponentInChildren<Renderer>();
                if (r == null) continue;
                var b = r.bounds;
                if (Mathf.Abs(b.max.y - GF) < 0.3f && (b.Contains(new Vector3(72.7f, b.center.y, 25.5f)) || b.Contains(new Vector3(68.7f, b.center.y, 25.5f))))
                { Object.DestroyImmediate(t.gameObject); log.Add("Removed floor tile under hatch " + b.center.ToString("F1")); }
            }
            // Lấp lại phần ô sàn quanh lỗ x[70.7,74.7] z[24.7,26.3]
            var floorMat = wallMat;
            // Lỗ cầu thang kéo dài x[68.7,74.7] (đủ khoảng trống đầu khi xuống dốc); 2 m phía tây che bằng tấm sàn thuộc nắp hầm
            GB.Box(k, "Floor_S", new Vector3(70.7f, GF - 0.1f, 24.1f), new Vector3(8f, 0.2f, 1.2f), floorMat);
            GB.Box(k, "Floor_N", new Vector3(70.7f, GF - 0.1f, 26.9f), new Vector3(8f, 0.2f, 1.2f), floorMat);
            GB.Box(k, "Floor_W", new Vector3(67.7f, GF - 0.1f, 25.5f), new Vector3(2f, 0.2f, 1.6f), floorMat);
            GB.Box(k, "Floor_E", new Vector3(74.6f, GF - 0.1f, 25.5f), new Vector3(0.2f, 0.2f, 1.6f), floorMat);
            // Nắp hầm (model Blender) + 5 xích
            var closed = new GameObject("CellarHatch_Closed").transform;
            closed.SetParent(k);
            var lid = Place(Model("KH_NapHam"), closed, new Vector3(72.6f, GF - 0.02f, 25.5f), 0, 1f, "NapHam");
            if (lid != null) lid.transform.localScale = new Vector3(4.0f / 4.4f, 1f, 1f);
            var lidCol = closed.gameObject.AddComponent<BoxCollider>();
            lidCol.center = closed.InverseTransformPoint(new Vector3(72.7f, GF - 0.05f, 25.5f));
            lidCol.size = new Vector3(4.0f, 0.12f, 1.7f);
            closed.gameObject.AddComponent<InfoInteractable>().Configure("prompt.chains", "msg.chains");
            GB.Box(closed, "HatchCover_West", new Vector3(69.7f, GF - 0.1f, 25.5f), new Vector3(2f, 0.2f, 1.6f), floorMat);
            for (int i = 0; i < 5; i++)
            {
                var ch = Place(Model("KH_Xich"), closed, new Vector3(71.1f + i * 0.8f, GF + 0.05f, 26.2f), 0, 1f, "Chain_" + (i + 1));
                if (ch != null) ch.transform.rotation = Quaternion.Euler(-58f, 0, 0);
            }
            var open = new GameObject("CellarHatch_Open").transform;
            open.SetParent(k);
            var lidOpen = Place(Model("KH_NapHam"), open, new Vector3(72.6f, GF + 0.85f, 26.35f), 0, 1f, "NapHam_Open");
            if (lidOpen != null) { lidOpen.transform.localScale = new Vector3(4.0f / 4.4f, 1f, 1f); lidOpen.transform.rotation = Quaternion.Euler(-80, 0, 0); }
            var fa = k.gameObject.AddComponent<FlagActivator>();
            fa.Configure(StoryFlag.CellarOpened, new[] { open.gameObject }, new[] { closed.gameObject });

            // Hầm: dựng bằng greybox ở gốc tọa độ rồi dời tới dưới Nhà Bếp
            var cellarParent = new GameObject("KH_CellarRoot").transform;
            cellarParent.SetParent(root);
            GB.BuildCellar(cellarParent);
            var cellar = cellarParent.Find("Cellar");
            // Tường đường hầm chỉ cao tới mặt đất (y=10 thế giới = -1 cục bộ) để không trồi lên sân
            foreach (var n in new[] { "StairTunnel_WallS", "StairTunnel_WallN" })
            {
                var w = cellar.Find(n);
                if (w == null) continue;
                w.localScale = new Vector3(w.localScale.x, 9f, w.localScale.z);
                w.position = new Vector3(w.position.x, -10f + 4.5f, w.position.z);
            }
            // Trần đường hầm (phần ngoài nhà) để không nhìn xuyên mặt đất
            // Trần chỉ phủ đoạn nằm ngoài nhà, đủ thấp hơn mặt đất (y thế giới 9.88–9.98) và bắt đầu từ chỗ đầu người chơi đã dưới 9.0
            GB.Box(cellar, "StairTunnel_Ceiling", new Vector3(-1.0f, -1.07f, 5.2f), new Vector3(8.6f, 0.1f, 2.0f), GB.mStone);
            cellarParent.position = CellarOrigin;
            // Vật liệu đá đẹp hơn cho hầm
            var stone = FindMaterial("BLD_Villa1_Base") ?? FindMaterial("Rock") ?? GB.mStone;
            foreach (var r in cellar.GetComponentsInChildren<MeshRenderer>())
                if (r.sharedMaterial == GB.mStone) r.sharedMaterial = stone;
            // Đồ sảnh tế: thay khối xám bằng model Blender
            ReplaceWithModel(cellar, "VacMauDen", "KH_VacMau", 0f, 0.0f);
            ReplaceWithModel(cellar, "AnDong_Placeholder", "KH_AnDong", 0f, 0.0f);
            var chair = cellar.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "GheGoMun");
            if (chair != null)
            {
                var ch = Place(P("Assets/Abandoned_Asylum/Prefabs/ChairLeather.prefab"), chair.parent, chair.position - Vector3.up * 0.5f, 0, 1f, "GheGoMun");
                Object.DestroyImmediate(chair.gameObject);
            }
            log.Add("Cellar moved under kitchen, origin " + CellarOrigin);
        }

        static void ReplaceWithModel(Transform parentSearch, string placeholder, string model, float yaw, float yOffset)
        {
            var ph = parentSearch.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == placeholder);
            if (ph == null) return;
            var b = ph.GetComponent<Renderer>() != null ? ph.GetComponent<Renderer>().bounds : new Bounds(ph.position, Vector3.one);
            var go = Place(Model(model), ph.parent, new Vector3(b.center.x, b.min.y + yOffset, b.center.z), yaw, 1f, placeholder);
            if (go != null)
            {
                var col = ph.GetComponent<Collider>();
                if (col != null) { var bc = go.AddComponent<BoxCollider>(); var mb = Bounds(go); bc.center = go.transform.InverseTransformPoint(mb.center); bc.size = mb.size; }
            }
            Object.DestroyImmediate(ph.gameObject);
        }

        static Bounds Bounds(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.one * 0.1f);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        // =====================================================================
        // Đồ đạc + vật phẩm theo phòng
        static void PlaceContent(DoorSet doors)
        {
            var c = new GameObject("KH_Content").transform;
            c.SetParent(root);
            var mPaper = AssetDatabase.LoadAssetAtPath<Material>(ModelsDir + "/Materials/M_PaperOld.mat") ?? data.mats[10];

            // ---------- Phòng (nhãn)
            Room(c, "Room_SanhChinh", "Sảnh Chính", new Vector3(86.7f, GF, 17.5f), new Vector3(16, 7, 20));
            Room(c, "Room_PhongKhach", "Phòng Khách", new Vector3(72.7f, GF, 11.5f), new Vector3(12, 7, 8));
            Room(c, "Room_NhaBep", "Nhà Bếp", new Vector3(72.7f, GF, 21.5f), new Vector3(12, 7, 12));
            Room(c, "Room_PhongLamViec", "Phòng Làm Việc", new Vector3(102.7f, GF, 11.5f), new Vector3(16, 7, 8));
            Room(c, "Room_CauThang", "Phòng cầu thang", new Vector3(102.7f, GF, 21.5f), new Vector3(16, 7, 12));
            Room(c, "Room_HanhLangVong", "Hành lang vòng tầng 2", new Vector3(90.7f, F2, 17.5f), new Vector3(24, 7, 20));
            Room(c, "Room_TreEm", "Phòng Trẻ Em (Mộc)", new Vector3(72.7f, F2, 23.5f), new Vector3(12, 7, 8));
            Room(c, "Room_PhongTho", "Phòng Thờ Gia Tộc", new Vector3(72.7f, F2, 17.5f), new Vector3(12, 7, 4));
            Room(c, "Room_GiaNhan", "Phòng Gia Nhân (Thổ)", new Vector3(72.7f, F2, 11.5f), new Vector3(12, 7, 8));
            Room(c, "Room_NguChuNhan", "Phòng Ngủ Chủ Nhân (Hỏa)", new Vector3(106.7f, F2, 9.5f), new Vector3(8, 7, 4));
            Room(c, "Room_ThuVien", "Thư Viện Cổ (Kim)", new Vector3(108.7f, F2, 17.5f), new Vector3(4, 7, 12));
            Room(c, "Room_KhachTro", "Phòng Khách Trọ (Thủy)", new Vector3(108.7f, F2, 25.5f), new Vector3(4, 7, 4));

            // ---------- Sảnh
            var bp = GB.PickupBox(c, "Pickup_QuanBackpack", new Vector3(93.0f, GF + 0.25f, 23.6f), new Vector3(0.45f, 0.5f, 0.3f), "item.backpack",
                rewards: new[] { StoryFlag.GotQuanBackpack, StoryFlag.HasCrowbar },
                sources: new NotebookSource[] { data.sources["TAPE_1"], data.sources["DOC_Now_TheQuan"] }, hint: "hint.backpack");
            SetMat(bp.gameObject, AssetDatabase.LoadAssetAtPath<Material>(ModelsDir + "/Materials/M_ClothBeige.mat"));
            var bb = GB.Readable(c, "Doc_BienBan2009", new Vector3(80.0f, GF + 1.7f, 7.95f), new Vector3(0.32f, 0.42f, 0.01f), "DOC_2009_BienBan");
            SetMat(bb.gameObject, mPaper);
            GB.PickupBox(c, "Pickup_Battery_Hall", new Vector3(84.2f, GF + 0.05f, 9.0f), new Vector3(0.05f, 0.1f, 0.05f), "item.battery", battery: 50f);

            // ---------- Phòng Khách: ảnh gia tộc + bảng đồng
            var portrait = Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_Painting_A.prefab"), c, new Vector3(66.95f, GF + 2.6f, 11.5f), 90f, 1.6f, "AnhGiaToc_Trinh");
            var plaque = GB.Readable(c, "Doc_BangDong1921", new Vector3(67.0f, GF + 1.35f, 11.5f), new Vector3(0.03f, 0.22f, 0.5f), "DOC_1921_BangDong");
            SetMat(plaque.gameObject, AssetDatabase.LoadAssetAtPath<Material>(ModelsDir + "/Materials/M_Bronze.mat"));

            // ---------- Nhà Bếp: bàn tiệc 1978
            var table = Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_LargeTable_A.prefab"), c, new Vector3(71.2f, GF, 19.8f), 0f, 1f, "BanTiec1978");
            float tableTop = table != null ? Bounds(table).max.y : GF + 0.8f;
            var tb = table != null ? Bounds(table) : new Bounds(new Vector3(71.2f, GF + 0.4f, 19.8f), new Vector3(2.4f, 0.8f, 1.2f));
            bool alongX = tb.size.x >= tb.size.z;
            for (int i = 0; i < 6; i++)
            {
                float t = (i % 3 - 1) * 0.8f; int side = i < 3 ? -1 : 1;
                var pos = alongX ? new Vector3(tb.center.x + t, GF, tb.center.z + side * (tb.extents.z + 0.45f)) : new Vector3(tb.center.x + side * (tb.extents.x + 0.45f), GF, tb.center.z + t);
                float yaw = alongX ? (side < 0 ? 0f : 180f) : (side < 0 ? 90f : 270f);
                var chair = Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_Chair_A.prefab"), c, pos, yaw + Random.Range(-14f, 14f), 1f, "Ghe_" + i);
                if (i == 4 && chair != null) chair.transform.rotation = Quaternion.Euler(0, yaw + 35f, 82f); // ghế đổ
            }
            string[] dishes = { "Plate", "Plate_Broken", "Plate", "Mug", "Plate", "Kettle", "Plate_Broken", "Mug" };
            for (int i = 0; i < dishes.Length; i++)
            {
                float t = -0.9f + i * 0.26f;
                var pos = alongX ? new Vector3(tb.center.x + t, tableTop, tb.center.z + ((i % 2) * 2 - 1) * 0.25f) : new Vector3(tb.center.x + ((i % 2) * 2 - 1) * 0.25f, tableTop, tb.center.z + t);
                Place(P("Assets/Abandoned_Asylum/Prefabs/" + dishes[i] + ".prefab"), c, pos, Random.Range(0, 360f), 1f, "Dish_" + i);
            }
            var thiep = GB.Readable(c, "Doc_ThiepTanGia1978", new Vector3(tb.center.x - 0.5f, tableTop + 0.01f, tb.center.z), new Vector3(0.2f, 0.01f, 0.14f), "DOC_1978_ThiepTanGia");
            SetMat(thiep.gameObject, mPaper);
            var dithu = GB.Readable(c, "Doc_DiThuLam1996", new Vector3(tb.center.x + 0.7f, tableTop + 0.01f, tb.center.z + 0.15f), new Vector3(0.22f, 0.01f, 0.3f), "DOC_1996_DiThuLam");
            SetMat(dithu.gameObject, mPaper);
            GB.Box(c, "HiddenInk_KyHieu", new Vector3(74.6f, GF + 0.006f, 22.6f), new Vector3(1.4f, 0.01f, 1.4f), data.mats[11], false);

            // ---------- Phòng Làm Việc: két sắt + bàn
            var safe = Place(Model("KH_KetSat"), c, new Vector3(109.9f, GF, 13.9f), 270f, 1f, "KetSat");
            if (safe != null) { var bc = safe.AddComponent<BoxCollider>(); var sb = Bounds(safe); bc.center = safe.transform.InverseTransformPoint(sb.center); bc.size = new Vector3(0.75f, 0.92f, 0.7f); }
            GB.PickupBox(c, "Placeholder_SafeContents", new Vector3(109.6f, GF + 0.95f, 13.9f), new Vector3(0.3f, 0.12f, 0.22f), "item.safe_placeholder",
                key: "key_upstairs", rewards: new[] { StoryFlag.SafeOpened, StoryFlag.HasUpstairsKey },
                sources: new NotebookSource[] { data.sources["DOC_1924_SoKhoaHon"], data.sources["DOC_1924_BanThietKeHam"] });
            var desk = Place(P("Assets/Abandoned_Asylum/Prefabs/TableKitchen.prefab"), c, new Vector3(101.5f, GF, 9.6f), 0f, 1f, "BanLamViec");
            float deskTop = desk != null ? Bounds(desk).max.y : GF + 0.8f;
            GB.PickupBox(c, "Pickup_MauBao1953", new Vector3(101.2f, deskTop + 0.01f, 9.6f), new Vector3(0.3f, 0.01f, 0.22f), "item.newspaper",
                sources: new NotebookSource[] { data.sources["DOC_1953_MauBao"] }, openDoc: true);
            GB.PickupBox(c, "Pickup_Film_Study", new Vector3(101.9f, deskTop + 0.03f, 9.4f), new Vector3(0.05f, 0.05f, 0.05f), "item.film", film: 6);
            GB.PickupBox(c, "Pickup_Tape2", new Vector3(109.0f, GF + 0.02f, 12.9f), new Vector3(0.1f, 0.02f, 0.065f), "item.tape",
                sources: new NotebookSource[] { data.sources["TAPE_2"] });
            Place(P("Assets/Scenes/ZNS3D/Prefabs/antique_light.prefab"), c, new Vector3(102.2f, deskTop, 9.9f), 0, 1f, "DenBan");

            // ---------- Tầng 2: 5 phòng Mắt Trận + Phòng Thờ
            // Trẻ Em (Mộc): tủ áo + giường + búp bê
            Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_Cabinet_A.prefab"), c, new Vector3(67.3f, F2, 26.3f), 90f, 1f, "HidingSpot_Wardrobe_TreEm");
            Anchor(Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_Bed_A.prefab"), c, new Vector3(72.5f, F2, 26.0f), 0f, 0.8f, "Giuong_TreEm"), maxZ: 27.2f, minX: 69.3f);
            Relic(c, 0, "KH_BupBe", new Vector3(68.3f, F2 + 0.01f, 25.2f), 110f, "item.relic_moc");
            // Gia Nhân (Thổ): giường sắt + tủ + lược
            Place(P("Assets/Abandoned_Asylum/Prefabs/Bed.prefab"), c, new Vector3(69.0f, F2, 9.0f), 90f, 1f, "HidingSpot_Bed_GiaNhan");
            Place(P("Assets/Abandoned_Asylum/Prefabs/Cupboard.prefab"), c, new Vector3(76.8f, F2, 8.1f), 180f, 1f, "HidingSpot_Cupboard_GiaNhan");
            var nightstand = Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_SmallTable_A.prefab"), c, new Vector3(67.6f, F2, 14.4f), 90f, 1f, "BanNho_GiaNhan");
            float nsTop = nightstand != null ? Bounds(nightstand).max.y : F2 + 0.7f;
            Relic(c, 2, "KH_Luoc", new Vector3(67.6f, nsTop + 0.01f, 14.4f), 30f, "item.relic_tho");
            // Phòng Thờ: bàn thờ dọc tường tây, 13 bài vị, 5 đài sen, nến
            BuildShrine(c);
            // Ngủ Chủ Nhân (Hỏa): giường + gương + tủ + hài thêu
            Anchor(Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_Bed_B.prefab"), c, new Vector3(107.6f, F2, 9.4f), 270f, 1f, "HidingSpot_Bed_NguChuNhan"), maxX: 110.45f, minZ: 7.8f);
            Place(P("Assets/Abandoned_Asylum/Prefabs/MirrorShelf.prefab"), c, new Vector3(103.3f, F2, 8.0f), 0f, 1f, "Guong_NguChuNhan");
            Anchor(Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_Cabinet_B.prefab"), c, new Vector3(104.6f, F2, 8.2f), 0f, 1f, "HidingSpot_Wardrobe_NguChuNhan"), minZ: 7.85f);
            Relic(c, 4, "KH_HaiTheu", new Vector3(105.0f, F2 + 0.01f, 10.6f), 200f, "item.relic_hoa");
            // Thư Viện (Kim): kệ sách + búa
            for (int i = 0; i < 3; i++) Place(P("Assets/Scenes/ZNS3D/Prefabs/bookshelf.prefab"), c, new Vector3(110.25f, F2, 13.6f + i * 3.6f), 270f, 1f, "KeSach_" + i);
            Relic(c, 3, "KH_Bua", new Vector3(107.8f, F2 + 0.02f, 17.8f), 75f, "item.relic_kim");
            // Khách Trọ (Thủy): giường + bàn + huyết thư
            Anchor(Place(P("Assets/Abandoned_Asylum/Prefabs/Bed.prefab"), c, new Vector3(109.3f, F2, 25.8f), 90f, 1f, "HidingSpot_Bed_KhachTro"), maxX: 110.45f, maxZ: 27.25f);
            var gtable = Place(P("Assets/Flooded_Grounds/Prefabs/Props/Prop_SmallTable_A.prefab"), c, new Vector3(107.6f, F2, 24.3f), 0f, 1f, "Ban_KhachTro");
            float gtTop = gtable != null ? Bounds(gtable).max.y : F2 + 0.7f;
            Relic(c, 1, "KH_BaiVi", new Vector3(107.6f, gtTop + 0.01f, 24.3f), 0f, "item.relic_thuy", paperInstead: mPaper);
            log.Add("Content placed");
        }

        static void BuildShrine(Transform c)
        {
            var g = new GameObject("PhongTho_BanTho").transform;
            g.SetParent(c);
            var altar = GB.Box(g, "BanTho", new Vector3(67.35f, F2 + 0.45f, 17.5f), new Vector3(0.9f, 0.9f, 3.4f),
                AssetDatabase.LoadAssetAtPath<Material>(ModelsDir + "/Materials/M_LacquerRed.mat") ?? GB.mDoor);
            var step = GB.Box(g, "BanTho_Tang2", new Vector3(67.15f, F2 + 1.05f, 17.5f), new Vector3(0.5f, 0.3f, 3.2f),
                AssetDatabase.LoadAssetAtPath<Material>(ModelsDir + "/Materials/M_WoodDark.mat") ?? GB.mDoor);
            altar.gameObject.AddComponent<InfoInteractable>().Configure("prompt.altar", "msg.altar");
            // 13 bài vị: 5 Mắt Trận (mạ vàng) xen giữa 8 phu hồ
            int[] marked = { 1, 4, 6, 8, 11 };
            for (int i = 0; i < 13; i++)
            {
                bool m = marked.Contains(i);
                Place(Model(m ? "KH_BaiVi_MatTran" : "KH_BaiVi"), g, new Vector3(67.12f, F2 + 1.2f, 15.95f + i * 0.26f), 90f, 1.1f, (m ? "BaiVi_MatTran_" : "BaiVi_") + i);
            }
            string[] hanh = { "Moc", "Thuy", "Tho", "Kim", "Hoa" };
            for (int i = 0; i < 5; i++)
            {
                var ds = Place(Model("KH_DaiSen"), g, new Vector3(67.62f, F2 + 0.9f, 15.95f + marked[i] * 0.26f), 0f, 1f, "DaiSen_" + hanh[i]);
            }
            for (int i = 0; i < 6; i++)
            {
                var nen = Place(Model("KH_Nen"), g, new Vector3(67.7f, F2 + 0.9f, 16.0f + i * 0.6f), 0f, 1f, "Nen_" + i);
                if (i % 2 == 0)
                {
                    var l = new GameObject("CandleLight_" + i).AddComponent<Light>();
                    l.transform.SetParent(g); l.transform.position = new Vector3(67.75f, F2 + 1.15f, 16.0f + i * 0.6f);
                    l.type = LightType.Point; l.range = 4.5f; l.intensity = 0.9f; l.color = new Color(1f, 0.62f, 0.3f); l.shadows = LightShadows.None;
                    l.gameObject.AddComponent<KhoaHon.Lighting.CandleFlicker>();
                }
            }
        }

        static void Relic(Transform parent, int hanh, string model, Vector3 pos, float yaw, string nameKey, Material paperInstead = null)
        {
            var relic = data.relics[hanh];
            GameObject go;
            if (paperInstead != null)
            {
                go = GB.Box(parent, "Relic_" + relic.hanh, pos + Vector3.up * 0.005f, new Vector3(0.22f, 0.01f, 0.3f), paperInstead).gameObject;
            }
            else
            {
                go = Place(Model(model), parent, pos, yaw, 1f, "Relic_" + relic.hanh);
                if (go == null) return;
            }
            var b = Bounds(go);
            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = go.transform.InverseTransformPoint(b.center);
            var ls = go.transform.lossyScale;
            col.size = new Vector3(Mathf.Max(0.3f, b.size.x) / ls.x, Mathf.Max(0.3f, b.size.y) / ls.y, Mathf.Max(0.3f, b.size.z) / ls.z);
            var p = go.AddComponent<Pickup>();
            p.Configure("Relic_" + relic.hanh, nameKey);
            p.SetRewards("", 0, 0, null, new NotebookSource[] { relic }, false, "");
        }

        /// <summary>Dời đồ đạc theo bounds thật để mép chạm các giới hạn cho trước (đẩy sát tường, chừa lối cửa).</summary>
        static void Anchor(GameObject go, float? maxX = null, float? minX = null, float? maxZ = null, float? minZ = null)
        {
            if (go == null) return;
            var b = Bounds(go);
            var d = Vector3.zero;
            if (maxX.HasValue) d.x = maxX.Value - b.max.x; else if (minX.HasValue) d.x = minX.Value - b.min.x;
            if (maxZ.HasValue) d.z = maxZ.Value - b.max.z; else if (minZ.HasValue) d.z = minZ.Value - b.min.z;
            go.transform.position += d;
        }

        static void SetMat(GameObject g, Material m)
        {
            if (m == null) return;
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m;
        }

        static void Room(Transform parent, string id, string label, Vector3 center, Vector3 size)
        {
            var g = new GameObject(id).transform;
            g.SetParent(parent); g.position = center;
            var rm = g.gameObject.AddComponent<RoomMarker>(); rm.displayName = label; rm.size = size;
        }

        // =====================================================================
        static void BuildCheckpoints(DoorSet doors)
        {
            var cps = new GameObject("KH_Checkpoints").transform;
            cps.SetParent(root);
            GB.Cp(cps, "CP_Gate", new Vector3(80.7f, 10f, -6f), 0, false, GameStage.Stage0_Gate);
            GB.Cp(cps, "CP_Hall", new Vector3(80.7f, GF, 11.5f), 0, true, GameStage.Stage1_GroundFloor);
            GB.Cp(cps, "CP_Floor2Landing", new Vector3(103.5f, F2, 25.6f), 270, true, GameStage.Stage2_Floor2);
            var shrine = GB.Cp(cps, "CP_Shrine", new Vector3(80.7f, F2, 17.5f), 270, false, GameStage.Stage2_Floor2);
            shrine.GetComponent<BoxCollider>().enabled = false;
            GB.Cp(cps, "CP_CellarStairs", CellarOrigin + new Vector3(-6.5f, -10f, 4.0f), 180, true, GameStage.Stage3_Cellar);
            GB.Cp(cps, "CP_RitualHall", CellarOrigin + new Vector3(-6.5f, -10f, -7.2f), 180, true, GameStage.Stage4_Ritual);

            var t = new GameObject("Trigger_FrontDoorSlam");
            t.transform.SetParent(cps); t.transform.position = new Vector3(80.7f, GF, 10.2f);
            var bc = t.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(3.6f, 2.5f, 1.2f); bc.center = new Vector3(0, 1.25f, 0);
            t.AddComponent<StoryTrigger>().Configure(new[] { StoryFlag.EnteredHouse }, StoryFlag.EnteredHouse, true, GameStage.Stage1_GroundFloor, doors.frontA, "sub.front_door_slam");

            var t2 = new GameObject("Trigger_Floor2Entered");
            t2.transform.SetParent(cps); t2.transform.position = new Vector3(103.5f, F2, 25.6f);
            var bc2 = t2.AddComponent<BoxCollider>(); bc2.isTrigger = true; bc2.size = new Vector3(2.5f, 2.5f, 2.5f); bc2.center = new Vector3(0, 1.25f, 0);
            t2.AddComponent<StoryTrigger>().Configure(new[] { StoryFlag.Floor2Entered }, StoryFlag.Floor2Entered, true, GameStage.Stage2_Floor2, null, "");
            log.Add("Checkpoints ok");
        }

        // =====================================================================
        /// <summary>
        /// Sàn hư Villa1_Floor_Mid_C có lỗ thủng thật (rơi 7 m xuống sảnh, cắt đứt hành lang vòng).
        /// Dò lỗ trên từng mảnh và bắc ván gỗ ngang qua (giữ vẻ đổ nát, đi được, NavMesh liền).
        /// </summary>
        static void PatchFloorHoles()
        {
            var g = new GameObject("KH_FloorPatches").transform;
            g.SetParent(root);
            var wood = AssetDatabase.LoadAssetAtPath<Material>(ModelsDir + "/Materials/M_WoodDark.mat") ?? GB.mDoor;
            int patched = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Villa1_Floor_Mid_C")).ToArray())
            {
                var b = r.bounds;
                if (b.center.x < 60 || b.center.x > 116 || b.center.z < 4 || b.center.z > 31) continue;
                float top = b.max.y;
                float minx = 1e9f, maxx = -1e9f, minz = 1e9f, maxz = -1e9f; int holes = 0;
                for (float x = b.min.x + 0.15f; x < b.max.x; x += 0.25f)
                    for (float z = b.min.z + 0.15f; z < b.max.z; z += 0.25f)
                        if (!Physics.Raycast(new Vector3(x, top + 0.6f, z), Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                        { holes++; minx = Mathf.Min(minx, x); maxx = Mathf.Max(maxx, x); minz = Mathf.Min(minz, z); maxz = Mathf.Max(maxz, z); }
                if (holes < 4) continue;
                minx -= 0.35f; maxx += 0.35f; minz -= 0.35f; maxz += 0.35f;
                // Collider liền phía dưới ván (đi êm, không vấp)
                var col = new GameObject("Patch_Collider"); col.transform.SetParent(g);
                col.transform.position = new Vector3((minx + maxx) / 2f, top - 0.05f, (minz + maxz) / 2f);
                col.AddComponent<BoxCollider>().size = new Vector3(maxx - minx, 0.1f, maxz - minz);
                col.isStatic = true;
                // Ván gỗ: chạy dọc trục dài của lỗ, xếp sát nhau có khe nhỏ, hơi lệch cho tự nhiên
                bool alongX = (maxx - minx) >= (maxz - minz);
                float across = alongX ? maxz - minz : maxx - minx, len = (alongX ? maxx - minx : maxz - minz) + 0.4f;
                int n = Mathf.Max(3, Mathf.CeilToInt(across / 0.3f));
                var rnd = new System.Random((int)(b.center.x * 10));
                for (int i = 0; i < n; i++)
                {
                    float o = -across / 2f + (i + 0.5f) * (across / n);
                    var c = new Vector3((minx + maxx) / 2f, top + 0.02f, (minz + maxz) / 2f) + (alongX ? new Vector3(0, 0, o) : new Vector3(o, 0, 0));
                    var size = alongX ? new Vector3(len * (0.9f + (float)rnd.NextDouble() * 0.15f), 0.04f, across / n - 0.03f) : new Vector3(across / n - 0.03f, 0.04f, len * (0.9f + (float)rnd.NextDouble() * 0.15f));
                    var plank = GB.Box(g, "Van_" + i, c, size, wood, false);
                    plank.rotation = Quaternion.Euler(0, ((float)rnd.NextDouble() - 0.5f) * 5f, ((float)rnd.NextDouble() - 0.5f) * 2f);
                }
                patched++;
                log.Add($"Floor hole patched at {b.center:F1} hole x[{minx:F1},{maxx:F1}] z[{minz:F1},{maxz:F1}]");
            }
            if (patched == 0) log.Add("No floor holes found");
        }

        // =====================================================================
        /// <summary>Bản demo cũ chặn cầu thang bằng _BLOCKERCUBE + ghế/tủ trên chiếu nghỉ → gỡ (cửa phòng cầu thang đã khóa bằng chìa).</summary>
        static void ClearStairBarricade()
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (t == null) continue;
                var p = t.position;
                bool inStair = p.x > 101.5f && p.x < 108f && p.z > 14f && p.z < 24.5f && p.y > 11.5f && p.y < 17.5f;
                bool blocker = t.name.StartsWith("_BLOCKERCUBE") && p.x > 60 && p.x < 116 && p.z > 4 && p.z < 31;
                bool barricade = inStair && t.parent != null && t.parent.name == "Map" && (t.name.StartsWith("Prop_ChurchBench") || t.name.StartsWith("Prop_Chair") || t.name.StartsWith("Prop_Cabinet"));
                if (blocker || barricade) { log.Add("Removed barricade " + t.name + " " + p.ToString("F1")); Object.DestroyImmediate(t.gameObject); n++; }
            }
        }

        /// <summary>
        /// Cầu thang chữ U gốc có gờ ~0.4 m (cao hơn stepOffset 0.3) → phủ 2 dốc + chiếu nghỉ vô hình để đi mượt và nối NavMesh.
        /// Đợt 1 (x≈103.6) lên phía nam tới chiếu nghỉ y≈14.7; đợt 2 (x≈105.75) lên phía bắc tới sàn tầng 2.
        /// </summary>
        static void BuildStairRamps()
        {
            var g = new GameObject("KH_StairRamps").transform;
            g.SetParent(root);
            void Ramp(string n, Vector3 a, Vector3 b, float w)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = n; go.transform.SetParent(g);
                Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
                var flat = new Vector3(b.x - a.x, 0, b.z - a.z); float run = flat.magnitude, rise = b.y - a.y;
                go.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, 0, 0);
                go.transform.localScale = new Vector3(w, 0.1f, Mathf.Sqrt(run * run + rise * rise) + 0.2f);
                go.transform.position = (a + b) / 2f - go.transform.up * 0.05f;
                go.isStatic = true;
            }
            Ramp("Ramp_Flight1", new Vector3(103.6f, GF, 23.6f), new Vector3(103.6f, 14.7f, 18.3f), 1.9f);
            Ramp("Ramp_Flight2", new Vector3(105.75f, 14.7f, 18.3f), new Vector3(105.75f, F2, 23.6f), 1.9f);
            var land = GameObject.CreatePrimitive(PrimitiveType.Cube); land.name = "Landing"; land.transform.SetParent(g);
            Object.DestroyImmediate(land.GetComponent<MeshRenderer>());
            land.transform.position = new Vector3(104.7f, 14.65f, 17.8f); land.transform.localScale = new Vector3(4.2f, 0.1f, 1.6f);
            land.isStatic = true;
        }

        // =====================================================================
        static void BakeNavMesh(Scene scene, int floor2Area)
        {
            var go = new GameObject("KH_NavMesh");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(85f, 7.5f, 15f);
            var surf = go.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.Volume;
            surf.center = Vector3.zero;
            surf.size = new Vector3(80f, 38f, 70f);
            surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.layerMask = ~(1 << 5);
            foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (p.GetComponent<NavMeshModifier>() == null) p.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.isTrigger && c.GetComponent<NavMeshModifier>() == null) c.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var pl = GameObject.Find("Player");
            if (pl != null && pl.GetComponent<NavMeshModifier>() == null) pl.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (var n in new[] { "KH_ExteriorBlockers" })
            {
                var b = GameObject.Find(n);
                if (b != null) foreach (Transform t in b.transform) if (t.name.StartsWith("Bound_")) t.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            }
            // Area Floor2: tầng 2 + nửa trên cầu thang (từ chiếu nghỉ y≈14.5 trở lên)
            var vol = new GameObject("NavArea_Floor2").AddComponent<NavMeshModifierVolume>();
            vol.transform.SetParent(go.transform);
            vol.transform.position = new Vector3(88.7f, 20.5f, 17.5f);
            vol.size = new Vector3(48.5f, 11f, 20.5f);
            vol.area = floor2Area;

            // Collider vừa tạo trong lần chạy này phải được đồng bộ vào physics trước khi thu nguồn bake
            Physics.SyncTransforms();
            surf.BuildNavMesh();
            string path = "Assets/_KhoaHon/Scenes/KH_Main_NavMesh.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surf.navMeshData, path);
            surf.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            var tri = NavMesh.CalculateTriangulation();
            log.Add($"NavMesh baked: tris={tri.areas.Length} floor2Tris={tri.areas.Count(a => a == floor2Area)}");
        }

        static void FixBuildSettings()
        {
            var list = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToList();
            list.RemoveAll(s => s.path == GB.ScenePath || s.path == GreyboxPath);
            list.Add(new EditorBuildSettingsScene(GB.ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            log.Add("Build settings: " + string.Join(", ", list.Select(s => s.path)));
        }
    }
}
