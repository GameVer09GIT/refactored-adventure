using System.Collections.Generic;
using HorrorGame.Inventory;
using KhoaHon.Core;
using KhoaHon.Items;
using KhoaHon.Notebook;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Tạo/cập nhật toàn bộ dữ liệu ScriptableObject của Khóa Hồn (văn bản theo PROMPT mục 8),
    /// config, font style, ItemData + prefab cầm tay placeholder. Chạy lại an toàn (idempotent).
    /// </summary>
    public static class KhoaHonDataBuilder
    {
        public const string Root = "Assets/_KhoaHon";
        public const string DataDir = Root + "/Data";

        public class Result
        {
            public TextTable text;
            public UIStyle style;
            public ToolsConfig tools;
            public KhoaHonDatabase db;
            public Dictionary<string, NotebookSource> sources = new Dictionary<string, NotebookSource>();
            public RelicData[] relics = new RelicData[5];
            public ItemData recorder, filmCamera, crowbar, flashlight;
            public Material[] mats;
        }

        [MenuItem("KhoaHon/Build/1. Data Assets")]
        public static void BuildMenu() => Build();

        public static Result Build()
        {
            foreach (var d in new[] { "Data", "Data/Configs", "Data/Documents", "Data/Tapes", "Data/Talismans", "Data/Relics", "Data/Items",
                                      "Prefabs", "Prefabs/Items", "Prefabs/Characters", "Materials", "Scenes" })
                EnsureFolder(Root + "/" + d);

            var r = new Result();
            r.text = LoadOrCreate<TextTable>(DataDir + "/Configs/KH_TextTable.asset");
            FillText(r.text);
            EditorUtility.SetDirty(r.text);

            r.style = LoadOrCreate<UIStyle>(DataDir + "/Configs/KH_UIStyle.asset");
            r.style.sansFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/BeVietnamPro-Regular SDF.asset");
            r.style.sansBoldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/BeVietnamPro-SemiBold SDF.asset");
            r.style.serifFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/NotoSerif-Regular SDF.asset");
            r.style.serifBoldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/NotoSerif-Bold SDF.asset");
            r.style.paperColor = new Color(0.86f, 0.80f, 0.66f, 1f); // giấy đục để phụ đề không xuyên qua
            EditorUtility.SetDirty(r.style);

            r.tools = LoadOrCreate<ToolsConfig>(DataDir + "/Configs/KH_ToolsConfig.asset");

            BuildDocuments(r);
            BuildTapes(r);
            BuildRelics(r);

            r.db = LoadOrCreate<KhoaHonDatabase>(DataDir + "/KH_Database.asset");
            r.db.sources = new List<NotebookSource>(r.sources.Values);
            EditorUtility.SetDirty(r.db);

            BuildItems(r);
            r.mats = BuildMaterials();

            AssetDatabase.SaveAssets();
            return r;
        }

        // ---------------------------------------------------------------- Text
        private static void FillText(TextTable t)
        {
            var e = new (string, string)[]
            {
                ("prompt.open_door", "[E] Mở cửa"),
                ("prompt.close_door", "[E] Đóng cửa"),
                ("prompt.locked", "Cửa bị khóa"),
                ("prompt.unlock_with", "[E] Mở khóa bằng {0}"),
                ("prompt.pickup", "[E] Nhặt {0}"),
                ("prompt.read", "[E] Đọc: {0}"),
                ("prompt.inspect", "[E] Xem"),
                ("prompt.main_door_locked", "Cửa chính đã bị then cài, không mở được"),
                ("prompt.stair_door_locked", "Cửa lên tầng hai bị khóa"),
                ("prompt.chains", "Nắp hầm bị khóa bằng 5 sợi xích"),
                ("prompt.altar", "Bàn thờ gia tộc — 13 bài vị, 5 đài sen"),
                ("prompt.body", "[E] Xem"),
                ("msg.chains", "Năm sợi xích lớn nối ngầm vào tường. Không có ổ khóa nào cả."),
                ("msg.altar", "Năm đài sen trống, mỗi đài nằm dưới một bài vị khắc tên một hành."),
                ("msg.rubble", "Đá sập kín một đường hầm khoan hẹp. Không đi tiếp được."),
                ("key.upstairs", "chìa khóa cửa cầu thang"),
                ("item.backpack", "balo của Quân"),
                ("item.battery", "pin tiểu"),
                ("item.film", "cuộn phim"),
                ("item.safe_placeholder", "đồ trong két sắt (tạm)"),
                ("item.tape", "cuộn băng cassette"),
                ("item.newspaper", "mẩu báo cũ"),
                ("item.relic_moc", "búp bê vải"),
                ("item.relic_thuy", "huyết thư"),
                ("item.relic_tho", "lược sừng gãy"),
                ("item.relic_kim", "búa đóng đinh"),
                ("item.relic_hoa", "đôi hài thêu"),
                ("item.generic", "vật phẩm"),
                ("toast.got_item", "Đã nhặt: {0}"),
                ("toast.notebook_added", "Đã ghi vào Sổ tay: {0}"),
                ("toast.saved", "Đã lưu tiến trình"),
                ("toast.no_save", "Chưa có bản lưu"),
                ("hint.backpack", "Có thể nghe cuộn băng bằng máy ghi âm (Tab → Băng ghi âm)"),
                ("sub.front_door_slam", "(Cửa chính sập lại sau lưng. Then cài rơi xuống.)"),
                ("ui.close", "Đóng [E]"),
                ("notebook.title", "Sổ tay điều tra"),
                ("notebook.hint", "Tab / J: đóng  ·  Q / E: đổi tab"),
                ("notebook.tab.documents", "Tài liệu"),
                ("notebook.tab.tapes", "Băng ghi âm"),
                ("notebook.tab.photos", "Ảnh"),
                ("notebook.tab.talismans", "Bùa chú"),
                ("notebook.tab.relics", "Kỷ vật"),
                ("notebook.empty", "Chưa có gì."),
                ("notebook.new", "mới"),
                ("notebook.tape_hint", "Cầm máy ghi âm để nghe cuộn băng này."),
                ("intro.1", "Quân mất tích ở biệt thự Thanh Vân đã mười bốn ngày."),
                ("intro.2", "Đêm cuối cùng, nó gửi cho tôi một tấm ảnh chụp sàn nhà bếp. Cảnh sát nói đó chỉ là vết bẩn."),
                ("intro.3", "Tôi không tin."),
            };
            foreach (var (k, v) in e) t.Set(k, v);
        }

        // ---------------------------------------------------------------- Documents
        private static void BuildDocuments(Result r)
        {
            Doc(r, "DOC_1921_BangDong", "Bảng đồng", ChronoYear.Y1921,
                "KHỞI CÔNG THANH VÂN — 1921.\nGia chủ Trịnh Đại Quan.");
            Doc(r, "DOC_1924_SoKhoaHon", "Sổ Khóa Hồn — tập 1", ChronoYear.Y1924,
                "Ngũ Hành Khóa Hồn. Lấy năm người thân cận làm Mắt Trận, mỗi người giữ một hành: Mộc, Thủy, Thổ, Kim, Hỏa. " +
                "Lấy tám phu hồ chôn sống trong vách làm mạch dẫn khí. Mười ba hồn khóa vào Ấn Đồng, kẻ giữ Ấn hưởng phúc lộc không cùng. " +
                "Năm đài sen là năm cửa của trận. Trận mở thì cửa hầm mở.\n\n(Kèm sơ đồ năm đài sen.)");
            Doc(r, "DOC_1924_BanThietKeHam", "Bản thiết kế hầm", ChronoYear.Y1924,
                "Cầu thang đá — sâu năm trượng. Xà lim. Sảnh tế bát giác. Vạc đặt chính giữa.\n\n(Chữ ký Thợ Cả bị gạch xóa bằng mực đỏ.)");
            Doc(r, "DOC_1953_MauBao", "Mẩu báo cũ", ChronoYear.Y1953,
                "THẢM KỊCH Ở BIỆT THỰ THANH VÂN: toàn bộ gia tộc họ Trịnh được phát hiện tử vong trong phòng ngủ. " +
                "Nhà chức trách chưa xác định nguyên nhân.");
            Doc(r, "DOC_1978_ThiepTanGia", "Thiệp tân gia gia đình Trần", ChronoYear.Y1978,
                "Trân trọng kính mời quý khách đến dự tiệc tân gia của gia đình chúng tôi tại biệt thự Thanh Vân.\n\n" +
                "Mặt sau, nét chữ run:\n\"Nửa cuốn sổ nói rằng ta sẽ có tất cả. Chỉ cần một bữa tối.\"");
            Doc(r, "DOC_1996_DiThuLam", "Di thư Lâm Gia Huy", ChronoYear.Y1996,
                "Mọi cơ quan của ngôi nhà đều điều khiển từ phòng làm việc và phòng thờ gia tộc. " +
                "Mật mã két sắt nằm ở năm khai sinh cơ nghiệp này. Ta đã mở được két, nhưng không qua được bàn thờ. " +
                "Người đàn bà cầm chùm chìa khóa đêm nào cũng đứng ở chân giường ta. Ta không chịu nổi nữa.");
            Doc(r, "DOC_2009_BienBan", "Biên bản niêm phong", ChronoYear.Y2009,
                "Biên bản niêm phong hiện trường. Sáu công nhân đội cải tạo mất tích sau khi thi công khoan tường tầng hầm. " +
                "Cấm mọi hành vi xâm nhập.");
            Doc(r, "DOC_Now_TheQuan", "Thẻ phóng viên của Quân", ChronoYear.Present,
                "THẺ PHÓNG VIÊN — Hạ Quân — Phóng viên ảnh.", false);

            var photo = LoadOrCreate<PhotoClueData>(DataDir + "/Documents/PHOTO_QuanSent.asset");
            photo.SetBasic("PHOTO_QuanSent", "Ảnh Quân gửi", ChronoYear.Present,
                "Ảnh chụp flash sàn nhà bếp, gửi đêm cuối cùng. Trên sàn hiện một ký hiệu Ngũ Hành bằng máu mà mắt thường không thấy.");
            EditorUtility.SetDirty(photo);
            r.sources[photo.Id] = photo;
        }

        private static DocumentData Doc(Result r, string id, string title, ChronoYear y, string body, bool serif = true)
        {
            var d = LoadOrCreate<DocumentData>(DataDir + "/Documents/" + id + ".asset");
            d.SetBasic(id, title, y, body, serif);
            EditorUtility.SetDirty(d);
            r.sources[id] = d;
            return d;
        }

        // ---------------------------------------------------------------- Tapes
        private static void BuildTapes(Result r)
        {
            Tape(r, "TAPE_1", "Băng #1 — balo ở Sảnh", ChronoYear.Present, "Cuộn băng tìm thấy trong balo của Quân.", new[]
            {
                "Ngày thứ… ba? Chị Vy, nếu chị nghe được cái này thì em xin lỗi vì không nghe máy.",
                "Em tìm ra rồi — lối xuống hầm nằm dưới nhà bếp, ngay dưới cái bàn tiệc.",
                "Có năm sợi xích, không có ổ khóa nào cả…",
                "(tiếng kim loại leng keng xa xa)",
                "…Chị nghe thấy không? Tiếng chìa khóa.",
                "Và tiếng bước chân, một bên nặng một bên nhẹ, như người đi khập khiễng.",
                "Nó ở trên lầu. Em tắt máy đây.",
            });
            Tape(r, "TAPE_2", "Băng #2 — cạnh két sắt", ChronoYear.Present, "Cuộn băng để cạnh két sắt trong Phòng Làm Việc.", new[]
            {
                "Két khóa số. Ông Lâm viết mã nằm ở năm khai sinh cơ nghiệp…",
                "Em thử năm xây xong, 1924 — không được.",
                "(tiếng chìa khóa leng keng trên trần)",
                "Bà ta ở ngay trên đầu em.",
                "Đêm qua em thấy bà ta đứng ở chiếu nghỉ cầu thang nhìn xuống, nhưng không bước xuống.",
                "Hình như bà ta không xuống được dưới này.",
            });
            Tape(r, "TAPE_3", "Băng #3 — cầu thang hầm", ChronoYear.Present, "Cuộn băng rơi trên bậc thang đá, cạnh đống đá sập.", new[]
            {
                "(giọng yếu, thở gấp)",
                "Em tìm được cái lỗ đội thợ khoan năm 2009, sau vách bếp…",
                "chui lọt xuống được thì vách sập. Chân em kẹt dưới đá… chảy máu nhiều quá.",
                "Có ai đó dưới này, ngồi trên ghế, nói chuyện rất nhẹ nhàng.",
                "Lão bảo lão có thể cho em mọi thứ.",
                "Chị ơi… nếu em không ra được — đừng nghe lão.",
            });
        }

        private static void Tape(Result r, string id, string title, ChronoYear y, string desc, string[] lines)
        {
            var t = LoadOrCreate<TapeData>(DataDir + "/Tapes/" + id + ".asset");
            t.SetBasic(id, title, y, desc, false);
            t.lines = new List<TapeData.Line>();
            float time = 0.5f;
            foreach (var l in lines)
            {
                float dur = Mathf.Clamp(l.Length * 0.065f, 2.2f, 7f);
                t.lines.Add(new TapeData.Line { time = time, duration = dur, text = l });
                time += dur + 0.4f;
            }
            EditorUtility.SetDirty(t);
            r.sources[id] = t;
        }

        // ---------------------------------------------------------------- Relics & talismans
        private static void BuildRelics(Result r)
        {
            string[] hanhName = { "Mộc", "Thủy", "Thổ", "Kim", "Hỏa" };
            string[] ids = { "Moc", "Thuy", "Tho", "Kim", "Hoa" };
            string[] titles = { "Búp bê vải của bé An", "Huyết thư chưa gửi", "Lược sừng gãy", "Búa đóng đinh của Thợ Cả", "Đôi hài thêu của Nhị nương" };
            string[] bodies =
            {
                "Con búp bê vải cũ của bé An — con nuôi nhà họ Trịnh, bị nhốt và bỏ đói trong tủ áo.\nTrong bụng búp bê có khâu một trang bùa.",
                "\"Ai đọc được thư này xin báo cho gia đình tôi ở phương xa: tôi không bỏ đi. Bữa tối nay có vị lạ. Chủ nhà mời rượu mãi không thôi…\"\n(Chữ nhòe máu ở cuối.)\nKẹp trong thư là một trang bùa.",
                "Chiếc lược sừng gãy của bà Quản Gia — người bị trói hồn vào Ấn đầu tiên.\nQuấn quanh lược là một trang bùa.",
                "Chiếc búa đẫm máu. Thợ Cả bị Đại Quan giết bằng chính chiếc búa này ngay khi hầm hoàn tất.\nQuấn quanh cán búa là một trang bùa.",
                "Đôi hài thêu của Nhị nương, vợ bé của Đại Quan, bị thiêu sống để cúng sao hạn.\nLót trong hài là một trang bùa.",
            };
            string[] lines = { "Cháu lạnh quá…", "(Tiếng nước nhỏ giọt.)", "Chạy đi… ta không kiềm chế được…", "(Tiếng gõ búa vọng trong tường.)", "(Mùi khét thoảng qua.)" };

            for (int i = 0; i < 5; i++)
            {
                var tal = LoadOrCreate<TalismanData>(DataDir + "/Talismans/TAL_" + ids[i] + ".asset");
                tal.SetBasic("TAL_" + ids[i], "Trang bùa hành " + hanhName[i], ChronoYear.Y1924,
                    "Bài chú Vy đã chép lại từ trang bùa hành " + hanhName[i] + ".");
                tal.hanh = (Hanh)i;
                // TODO(ASSETS_NEEDED): GDD chưa có nội dung bài chú — để chỗ trống đánh dấu rõ ràng.
                tal.chantLines = new List<string> { "[Câu chú 1 — chờ nội dung]", "[Câu chú 2 — chờ nội dung]", "[Câu chú 3 — chờ nội dung]", "[Câu chú 4 — chờ nội dung]" };
                EditorUtility.SetDirty(tal);
                r.sources[tal.Id] = tal;

                var rel = LoadOrCreate<RelicData>(DataDir + "/Relics/REL_" + ids[i] + ".asset");
                rel.SetBasic("REL_" + ids[i], titles[i], ChronoYear.Y1924, bodies[i]);
                rel.hanh = (Hanh)i;
                rel.talisman = tal;
                rel.pickupLine = lines[i];
                EditorUtility.SetDirty(rel);
                r.sources[rel.Id] = rel;
                r.relics[i] = rel;
            }
        }

        // ---------------------------------------------------------------- Items
        private static void BuildItems(Result r)
        {
            r.flashlight = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/Item_Flashlight.asset");
            var mats = BuildMaterials();
            r.recorder = Item("Item_Recorder", "Máy ghi âm", ItemType.Recorder, BuildRecorderPrefab(mats));
            r.filmCamera = Item("Item_FilmCamera", "Máy ảnh film", ItemType.FilmCamera, BuildCameraPrefab(mats));
            r.crowbar = Item("Item_Crowbar", "Xà beng", ItemType.Crowbar, BuildCrowbarPrefab(mats));
        }

        private static ItemData Item(string file, string display, ItemType type, GameObject prefab)
        {
            string path = DataDir + "/Items/" + file + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null)
            {
                // Sao chép Item_Radar để mượn pose Pose_HoldDevice và thông số IK (không sửa asset gốc)
                AssetDatabase.CopyAsset("Assets/Items/Item_Radar.asset", path);
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
            var root = new GameObject("KH_Recorder");
            root.AddComponent<AudioSource>();
            var item = root.AddComponent<RecorderItem>();
            Part(root.transform, PrimitiveType.Cube, "Body", new Vector3(0, 0, 0), new Vector3(0.13f, 0.035f, 0.085f), m[6]);
            Part(root.transform, PrimitiveType.Cylinder, "ReelL", new Vector3(-0.03f, 0.019f, 0.005f), new Vector3(0.032f, 0.002f, 0.032f), m[7]);
            Part(root.transform, PrimitiveType.Cylinder, "ReelR", new Vector3(0.03f, 0.019f, 0.005f), new Vector3(0.032f, 0.002f, 0.032f), m[7]);
            var led = Part(root.transform, PrimitiveType.Cube, "LED", new Vector3(0.05f, 0.019f, -0.033f), new Vector3(0.008f, 0.004f, 0.008f), m[8]);
            var so = new SerializedObject(item);
            so.FindProperty("ledRenderer").objectReferenceValue = led.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, Root + "/Prefabs/Items/KH_Recorder.prefab");
        }

        private static GameObject BuildCameraPrefab(Material[] m)
        {
            var root = new GameObject("KH_FilmCamera");
            root.AddComponent<AudioSource>();
            root.AddComponent<FilmCameraItem>();
            Part(root.transform, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.13f, 0.08f, 0.045f), m[6]);
            var lens = Part(root.transform, PrimitiveType.Cylinder, "Lens", new Vector3(0, -0.005f, 0.04f), new Vector3(0.05f, 0.02f, 0.05f), m[7]);
            lens.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Part(root.transform, PrimitiveType.Cube, "Flash", new Vector3(0.04f, 0.05f, 0.0f), new Vector3(0.035f, 0.02f, 0.02f), m[9]);
            return SavePrefab(root, Root + "/Prefabs/Items/KH_FilmCamera.prefab");
        }

        private static GameObject BuildCrowbarPrefab(Material[] m)
        {
            var root = new GameObject("KH_Crowbar");
            root.AddComponent<AudioSource>();
            root.AddComponent<CrowbarItem>();
            var shaft = Part(root.transform, PrimitiveType.Cylinder, "Shaft", new Vector3(0, 0.1f, 0), new Vector3(0.022f, 0.22f, 0.022f), m[7]);
            var hook = Part(root.transform, PrimitiveType.Cube, "Hook", new Vector3(0, 0.33f, 0.03f), new Vector3(0.02f, 0.02f, 0.07f), m[7]);
            hook.transform.localRotation = Quaternion.Euler(-30, 0, 0);
            return SavePrefab(root, Root + "/Prefabs/Items/KH_Crowbar.prefab");
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

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, i));
            AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
        }
    }
}
