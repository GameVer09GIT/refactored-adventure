using System.Collections.Generic;
using Blackwood.Inventory;
using Blackwood.Core;
using Blackwood.Items;
using Blackwood.Notebook;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Tạo/cập nhật toàn bộ dữ liệu ScriptableObject (văn bản song ngữ lấy từ BlackwoodContent — kịch bản Blackwood Manor),
    /// config, font style, ItemData + prefab cầm tay placeholder. Chạy lại an toàn (idempotent).
    /// </summary>
    public static class BlackwoodDataBuilder
    {
        public const string Root = "Assets/BlackwoodManor";
        public const string DataDir = Root + "/Data";

        public class Result
        {
            public TextTable text;
            public UIStyle style;
            public ToolsConfig tools;
            public BlackwoodDatabase db;
            public Dictionary<string, NotebookSource> sources = new Dictionary<string, NotebookSource>();
            public RelicData[] relics = new RelicData[5];
            public ItemData recorder, filmCamera, crowbar, flashlight, phone, nightVision;
            public CharacterProfile evelyn, alex;
            public Material[] mats;
        }

        [MenuItem("Blackwood/Build/1. Data Assets")]
        public static void BuildMenu() => Build();

        public static Result Build()
        {
            foreach (var d in new[] { "Data", "Data/Configs", "Data/Documents", "Data/Tapes", "Data/Talismans", "Data/Relics", "Data/Items",
                                      "Data/Characters", "Prefabs", "Prefabs/Items", "Prefabs/Characters", "Materials", "Scenes" })
                EnsureFolder(Root + "/" + d);

            var r = new Result();
            r.text = LoadOrCreate<TextTable>(DataDir + "/Configs/BW_TextTable.asset");
            FillText(r.text);
            EditorUtility.SetDirty(r.text);

            r.style = LoadOrCreate<UIStyle>(DataDir + "/Configs/BW_UIStyle.asset");
            r.style.sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/BeVietnamPro-Regular SDF.asset");
            r.style.sansBoldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/BeVietnamPro-SemiBold SDF.asset");
            r.style.serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/NotoSerif-Regular SDF.asset");
            r.style.serifBoldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/NotoSerif-Bold SDF.asset");
            r.style.paperColor = new Color(0.86f, 0.80f, 0.66f, 1f); // giấy đục để phụ đề không xuyên qua
            EditorUtility.SetDirty(r.style);

            r.tools = LoadOrCreate<ToolsConfig>(DataDir + "/Configs/BW_ToolsConfig.asset");

            BuildDocuments(r);
            BuildTapes(r);
            BuildRelics(r);

            r.db = LoadOrCreate<BlackwoodDatabase>(DataDir + "/BW_Database.asset");
            r.db.sources = new List<NotebookSource>(r.sources.Values);
            EditorUtility.SetDirty(r.db);

            BuildItems(r);
            BuildCharacters(r);
            r.mats = BuildMaterials();

            AssetDatabase.SaveAssets();
            return r;
        }

        // ---------------------------------------------------------------- Text
        private static void FillText(TextTable t)
        {
            var keep = new HashSet<string>();
            foreach (var (k, vi, en) in BlackwoodContent.Text) { t.Set(k, vi, en); keep.Add(k); }
            foreach (var (k, vi, en) in BlackwoodContent.TextCharacters) { t.Set(k, vi, en); keep.Add(k); }
            foreach (var (k, vi, en) in BlackwoodContent.TextSequences) { t.Set(k, vi, en); keep.Add(k); }
            foreach (var (k, vi, en) in BlackwoodContent.TextUI) { t.Set(k, vi, en); keep.Add(k); }
            t.RemoveAllExcept(keep);
        }

        // ---------------------------------------------------------------- Documents
        private static void BuildDocuments(Result r)
        {
            foreach (var d in BlackwoodContent.Documents)
            {
                var doc = LoadOrCreate<DocumentData>(DataDir + "/Documents/" + d.id + ".asset");
                doc.SetBasic(d.id, d.titleVi, d.year, d.bodyVi, d.serif);
                doc.SetEnglish(d.titleEn, d.bodyEn);
                EditorUtility.SetDirty(doc);
                r.sources[d.id] = doc;
            }

            var p = BlackwoodContent.Photo;
            var photo = LoadOrCreate<PhotoClueData>(DataDir + "/Documents/" + p.id + ".asset");
            photo.SetBasic(p.id, p.titleVi, p.year, p.bodyVi, p.serif);
            photo.SetEnglish(p.titleEn, p.bodyEn);
            EditorUtility.SetDirty(photo);
            r.sources[photo.Id] = photo;
        }

        // ---------------------------------------------------------------- Tapes
        private static void BuildTapes(Result r)
        {
            foreach (var def in BlackwoodContent.Tapes)
            {
                var t = LoadOrCreate<TapeData>(DataDir + "/Tapes/" + def.id + ".asset");
                t.SetBasic(def.id, def.titleVi, ChronoYear.Present, def.descVi, false);
                t.SetEnglish(def.titleEn, def.descEn);
                t.lines = new List<TapeData.Line>();
                float time = 0.5f;
                foreach (var (vi, en) in def.lines)
                {
                    // Thời lượng theo câu dài hơn trong 2 ngôn ngữ để phụ đề kịp đọc
                    int len = Mathf.Max(vi.Length, en.Length);
                    float dur = Mathf.Clamp(len * 0.065f, 2.2f, 7f);
                    t.lines.Add(new TapeData.Line { time = time, duration = dur, text = vi, textEn = en });
                    time += dur + 0.4f;
                }
                EditorUtility.SetDirty(t);
                r.sources[def.id] = t;
            }
        }

        // ---------------------------------------------------------------- Relics & Vademecum pages
        private static void BuildRelics(Result r)
        {
            string[] ids = { "Moc", "Thuy", "Tho", "Kim", "Hoa" };
            for (int i = 0; i < 5; i++)
            {
                var def = BlackwoodContent.Relics[i];
                var tal = LoadOrCreate<TalismanData>(DataDir + "/Talismans/TAL_" + ids[i] + ".asset");
                tal.SetBasic("TAL_" + ids[i], def.pageTitleVi, ChronoYear.Y1924, def.pageBodyVi);
                tal.SetEnglish(def.pageTitleEn, def.pageBodyEn);
                tal.hanh = (Hanh)i;
                tal.chantLines = new List<string>(def.chantVi);
                tal.chantLinesEn = new List<string>(def.chantEn);
                EditorUtility.SetDirty(tal);
                r.sources[tal.Id] = tal;

                var rel = LoadOrCreate<RelicData>(DataDir + "/Relics/REL_" + ids[i] + ".asset");
                rel.SetBasic("REL_" + ids[i], def.titleVi, ChronoYear.Y1924, def.bodyVi);
                rel.SetEnglish(def.titleEn, def.bodyEn);
                rel.hanh = (Hanh)i;
                rel.talisman = tal;
                rel.pickupLine = def.lineVi;
                rel.pickupLineEn = def.lineEn;
                EditorUtility.SetDirty(rel);
                r.sources[rel.Id] = rel;
                r.relics[i] = rel;
            }
        }

        // ---------------------------------------------------------------- Characters
        private static void BuildCharacters(Result r)
        {
            r.evelyn = Character("CHAR_Evelyn", PlayerCharacter.Evelyn, "evelyn", new Color(0.72f, 0.55f, 0.32f),
                new[] { r.recorder, r.filmCamera, r.crowbar });
            r.alex = Character("CHAR_Alex", PlayerCharacter.Alex, "alex", new Color(0.35f, 0.75f, 0.95f),
                new[] { r.phone, r.nightVision, r.crowbar });
        }

        private static CharacterProfile Character(string file, PlayerCharacter c, string key, Color accent, ItemData[] hotbar)
        {
            var p = LoadOrCreate<CharacterProfile>(DataDir + "/Characters/" + file + ".asset");
            p.character = c;
            p.nameKey = "char." + key + ".name";
            p.roleKey = "char." + key + ".role";
            p.motiveKey = "char." + key + ".motive";
            p.toolsKey = "char." + key + ".tools";
            p.introKeys = new List<string> { "intro." + key + ".1", "intro." + key + ".2", "intro." + key + ".3" };
            p.accent = accent;
            p.hotbar = hotbar;
            EditorUtility.SetDirty(p);
            return p;
        }

        // ---------------------------------------------------------------- Items
        private static void BuildItems(Result r)
        {
            r.flashlight = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/BlackwoodManor/Data/Items/Item_Flashlight.asset");
            var mats = BuildMaterials();
            // displayName là tên nội bộ (tiếng Anh); UI hiển thị lấy key item.* trong TextTable
            r.recorder = Item("Item_Recorder", "Cassette Recorder", ItemType.Recorder, BuildRecorderPrefab(mats));
            r.filmCamera = Item("Item_FilmCamera", "Film Camera", ItemType.FilmCamera, BuildCameraPrefab(mats));
            r.crowbar = Item("Item_Crowbar", "Crowbar", ItemType.Crowbar, BuildCrowbarPrefab(mats));
            r.phone = Item("Item_Phone", "Smartphone", ItemType.Phone, BuildSimplePrefab<Blackwood.Characters.PhoneItem>("BW_Phone", mats));
            r.nightVision = Item("Item_NightVision", "Night Vision", ItemType.NightVision, BuildSimplePrefab<Blackwood.Characters.NightVisionItem>("BW_NightVision", mats));
        }

        private static ItemData Item(string file, string display, ItemType type, GameObject prefab)
        {
            string path = DataDir + "/Items/" + file + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null)
            {
                // Sao chép Item_Radar để mượn pose Pose_HoldDevice và thông số IK (không sửa asset gốc)
                AssetDatabase.CopyAsset("Assets/BlackwoodManor/Data/Items/Item_Radar.asset", path);
                data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            }
            var so = new SerializedObject(data);
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("itemType").enumValueIndex = (int)type;
            so.FindProperty("heldPrefab").objectReferenceValue = prefab;
            so.FindProperty("icon").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            var held = prefab.GetComponent<HeldItem>();
            var hso = new SerializedObject(held);
            hso.FindProperty("itemData").objectReferenceValue = data;
            hso.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SavePrefabAsset(prefab);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject BuildRecorderPrefab(Material[] m)
        {
            var root = new GameObject("BW_Recorder");
            root.AddComponent<AudioSource>();
            var item = root.AddComponent<RecorderItem>();
            Part(root.transform, PrimitiveType.Cube, "Body", new Vector3(0, 0, 0), new Vector3(0.13f, 0.035f, 0.085f), m[6]);
            Part(root.transform, PrimitiveType.Cylinder, "ReelL", new Vector3(-0.03f, 0.019f, 0.005f), new Vector3(0.032f, 0.002f, 0.032f), m[7]);
            Part(root.transform, PrimitiveType.Cylinder, "ReelR", new Vector3(0.03f, 0.019f, 0.005f), new Vector3(0.032f, 0.002f, 0.032f), m[7]);
            var led = Part(root.transform, PrimitiveType.Cube, "LED", new Vector3(0.05f, 0.019f, -0.033f), new Vector3(0.008f, 0.004f, 0.008f), m[8]);
            var so = new SerializedObject(item);
            so.FindProperty("ledRenderer").objectReferenceValue = led.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, Root + "/Prefabs/Items/BW_Recorder.prefab");
        }

        private static GameObject BuildCameraPrefab(Material[] m)
        {
            var root = new GameObject("BW_FilmCamera");
            root.AddComponent<AudioSource>();
            root.AddComponent<FilmCameraItem>();
            Part(root.transform, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.13f, 0.08f, 0.045f), m[6]);
            var lens = Part(root.transform, PrimitiveType.Cylinder, "Lens", new Vector3(0, -0.005f, 0.04f), new Vector3(0.05f, 0.02f, 0.05f), m[7]);
            lens.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Part(root.transform, PrimitiveType.Cube, "Flash", new Vector3(0.04f, 0.05f, 0.0f), new Vector3(0.035f, 0.02f, 0.02f), m[9]);
            return SavePrefab(root, Root + "/Prefabs/Items/BW_FilmCamera.prefab");
        }

        private static GameObject BuildCrowbarPrefab(Material[] m)
        {
            var root = new GameObject("BW_Crowbar");
            root.AddComponent<AudioSource>();
            root.AddComponent<CrowbarItem>();
            var shaft = Part(root.transform, PrimitiveType.Cylinder, "Shaft", new Vector3(0, 0.1f, 0), new Vector3(0.022f, 0.22f, 0.022f), m[7]);
            var hook = Part(root.transform, PrimitiveType.Cube, "Hook", new Vector3(0, 0.33f, 0.03f), new Vector3(0.02f, 0.02f, 0.07f), m[7]);
            hook.transform.localRotation = Quaternion.Euler(-30, 0, 0);
            return SavePrefab(root, Root + "/Prefabs/Items/BW_Crowbar.prefab");
        }

        /// <summary>Prefab cầm tay tối giản (model Blender được gắn lại bởi converter).</summary>
        private static GameObject BuildSimplePrefab<TItem>(string name, Material[] m) where TItem : HeldItem
        {
            var root = new GameObject(name);
            root.AddComponent<AudioSource>();
            root.AddComponent<TItem>();
            Part(root.transform, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.07f, 0.14f, 0.01f), m[6]);
            return SavePrefab(root, Root + "/Prefabs/Items/" + name + ".prefab");
        }

        private static GameObject Part(Transform parent, PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return p;
        }

        // ---------------------------------------------------------------- Materials
        /// <summary>0 tường, 1 sàn, 2 gỗ cửa, 3 đá hầm, 4 đất, 5 đường, 6 nhựa đen, 7 kim loại, 8 LED, 9 flash, 10 tương tác, 11 đỏ máu</summary>
        public static Material[] BuildMaterials()
        {
            var defs = new (string, Color, float, float)[]
            {
                ("M_GB_Wall", new Color(0.55f, 0.53f, 0.50f), 0f, 0.2f),
                ("M_GB_Floor", new Color(0.36f, 0.28f, 0.22f), 0f, 0.3f),
                ("M_GB_Door", new Color(0.30f, 0.18f, 0.11f), 0f, 0.35f),
                ("M_GB_Stone", new Color(0.30f, 0.30f, 0.31f), 0f, 0.15f),
                ("M_GB_Ground", new Color(0.16f, 0.18f, 0.13f), 0f, 0.1f),
                ("M_GB_Road", new Color(0.12f, 0.12f, 0.13f), 0f, 0.35f),
                ("M_GB_Plastic", new Color(0.06f, 0.06f, 0.06f), 0f, 0.45f),
                ("M_GB_Metal", new Color(0.42f, 0.42f, 0.44f), 0.8f, 0.5f),
                ("M_GB_LED", new Color(0.2f, 0.02f, 0.02f), 0f, 0.6f),
                ("M_GB_Flash", new Color(0.85f, 0.85f, 0.85f), 0f, 0.8f),
                ("M_GB_Interact", new Color(0.75f, 0.62f, 0.30f), 0f, 0.4f),
                ("M_GB_Blood", new Color(0.30f, 0.02f, 0.02f), 0f, 0.6f),
            };
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var list = new Material[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var (n, c, met, sm) = defs[i];
                string path = Root + "/Materials/" + n + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Metallic", met);
                m.SetFloat("_Smoothness", sm);
                EditorUtility.SetDirty(m);
                list[i] = m;
            }
            return list;
        }

        // ---------------------------------------------------------------- Helpers
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        /// <summary>
        /// Ghi file vào project an toàn: bỏ qua nếu nội dung không đổi (không reimport thừa), thử lại khi Windows báo file đang
        /// bị khóa/ánh xạ (IOException 1224 — Unity hoặc trình quét đang mở texture). Trả về true nếu đã ghi.
        /// </summary>
        public static bool WriteBytesIfChanged(string path, byte[] data)
        {
            string full = System.IO.Path.GetFullPath(path.StartsWith("Assets") ? System.IO.Path.Combine(Application.dataPath, "../" + path) : path);
            if (System.IO.File.Exists(full))
            {
                var old = System.IO.File.ReadAllBytes(full);
                if (old.Length == data.Length && System.Linq.Enumerable.SequenceEqual(old, data)) return false;
            }
            for (int i = 0; ; i++)
            {
                try { System.IO.File.WriteAllBytes(full, data); return true; }
                catch (System.IO.IOException) when (i < 40) { System.Threading.Thread.Sleep(150); }
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, i));
            AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
        }
    }
}
