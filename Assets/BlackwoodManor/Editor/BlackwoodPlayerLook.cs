using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Ngoại hình cơ thể người chơi (model civilian_girl, không sửa file của asset pack):
    ///  • normal chi tiết lặp dày cho da (lỗ chân lông, nếp da) và vải (sợi dệt) — bàn tay/tay áo nhìn sát camera không còn bị bệt;
    ///  • Evelyn: áo khoác len dài tay sơn thẳng vào bản sao texture thân (albedo + normal + mask), viền cổ tay;
    ///  • Alex: giữ áo thun tay trần của model gốc.
    /// Vật liệu ra ở Materials/Player, component <see cref="Blackwood.Characters.PlayerLook"/> đổi theo nhân vật lúc chạy.
    /// </summary>
    public static class BlackwoodPlayerLook
    {
        const string TexDir = "Assets/BlackwoodManor/Textures/Player";
        const string MatDir = "Assets/BlackwoodManor/Materials/Player";
        const string GirlPath = "Assets/ThirdParty/civilian_girl/Prefabs/civilian_girl.prefab";
        const int S = 2048;
        static readonly Color Coat = new Color(0.125f, 0.105f, 0.092f);

        static Color[] Read(Texture src, int size, bool srgb)
        {
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, !srgb);
            t.ReadPixels(new Rect(0, 0, size, size), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var px = t.GetPixels(); Object.DestroyImmediate(t);
            return px;
        }

        static Texture2D Save(string name, Color[] px, int size, bool srgb, bool normal)
        {
            BlackwoodDataBuilder.EnsureFolder(TexDir);
            string path = TexDir + "/" + name + ".png";
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            BlackwoodDataBuilder.WriteBytesIfChanged(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb; ti.mipmapEnabled = true; ti.alphaIsTransparency = false; ti.maxTextureSize = 4096;
            ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Variant(string name, Material src)
        {
            BlackwoodDataBuilder.EnsureFolder(MatDir);
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, p); }
            else { m.shader = src.shader; m.CopyPropertiesFromMaterial(src); }
            return m;
        }

        static void Detail(Material m, Texture2D normal, float tiling, float scale, Texture2D mask)
        {
            m.SetTexture("_DetailNormalMap", normal); m.SetFloat("_DetailNormalMapScale", scale); m.SetFloat("_DetailAlbedoMapScale", 1f);
            m.SetTextureScale("_DetailAlbedoMap", new Vector2(tiling, tiling));
            m.SetTexture("_DetailMask", mask);
            BlackwoodMaterialUtil.Validate(m); // URP chọn _DETAIL_MULX2/_DETAIL_SCALED theo thang đo — để nó quyết từ đầu
        }

        /// <summary>Sơn áo khoác dài tay của Evelyn vào bản sao texture thân. Trả về albedo/normal/mask + mặt nạ chi tiết da.</summary>
        static void PaintCoat(GameObject girl, Material srcBody, bool force, out Texture2D albedo, out Texture2D normal, out Texture2D mask, out Texture2D skinMask)
        {
            Texture2D Load(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + n + ".png");
            albedo = Load("T_PL_Evelyn_A"); normal = Load("T_PL_Evelyn_N"); mask = Load("T_PL_Evelyn_M"); skinMask = Load("T_PL_Evelyn_SkinMask");
            if (!force && albedo != null && normal != null && mask != null && skinMask != null) return;

            BlackwoodHousekeeperLook.ClothMask(girl, S, out var cloth, out var cuff, new[] { "shirt_t", "hand_", "chest" });
            var a = Read(srcBody.GetTexture("_BaseMap"), S, true);
            var n = Read(srcBody.GetTexture("_BumpMap"), S, false);
            var k = Read(srcBody.GetTexture("_MetallicGlossMap"), S, false);
            var wv = BlackwoodPropTextures.ClassHeight("cloth"); int W = BlackwoodPropTextures.Size;
            float Weave(int x, int y) => wv[((y * 2) % W + W) % W * W + ((x * 2) % W + W) % W];

            var sm = new Color[512 * 512];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int i = y * S + x;
                    // normal gốc (giải nén RG hoặc AG)
                    float nx = n[i].r * n[i].a * 2f - 1f, ny = n[i].g * 2f - 1f;
                    float c = cloth[i];
                    if (c > 0f)
                    {
                        float h = Weave(x, y), cf = cuff[i];
                        float lum = a[i].grayscale;
                        float t = Mathf.Lerp(0.9f, 1.08f, Mathf.Clamp01(lum * 1.5f)) * (0.78f + 0.4f * h) * (1f - 0.5f * cf);
                        a[i] = new Color(Coat.r * t, Coat.g * t, Coat.b * t, 1f);
                        float wx = (Weave(x - 1, y) - Weave(x + 1, y)) * 2.2f, wy = (Weave(x, y - 1) - Weave(x, y + 1)) * 2.2f;
                        nx = nx * 0.45f + wx; ny = ny * 0.45f + wy;
                        k[i] = new Color(0f, k[i].g * (0.75f + 0.25f * h), 0f, 0.07f + 0.1f * h);
                    }
                    float nz = Mathf.Sqrt(Mathf.Max(0.05f, 1f - nx * nx - ny * ny));
                    float inv = 1f / Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                    n[i] = new Color(nx * inv * 0.5f + 0.5f, ny * inv * 0.5f + 0.5f, nz * inv * 0.5f + 0.5f, 1f);
                    if ((x & 3) == 0 && (y & 3) == 0) { float s = 1f - c; sm[(y >> 2) * 512 + (x >> 2)] = new Color(s, s, s, s); }
                }
            albedo = Save("T_PL_Evelyn_A", a, S, true, false);
            normal = Save("T_PL_Evelyn_N", n, S, false, true);
            mask = Save("T_PL_Evelyn_M", k, S, false, false);
            skinMask = Save("T_PL_Evelyn_SkinMask", sm, 512, false, false);
        }

        [MenuItem("Blackwood/Build/Player Look (regenerate)")]
        public static void Menu() => Debug.Log(Build(GameObject.Find("Player"), true));

        /// <summary>Tạo texture + vật liệu và gắn <see cref="Blackwood.Characters.PlayerLook"/> vào Player (nếu có).</summary>
        public static string Build(GameObject player, bool force = false)
        {
            if (EditorApplication.isPlaying) return "Đang Play.";
            var girl = AssetDatabase.LoadAssetAtPath<GameObject>(GirlPath);
            if (girl == null) return "PlayerLook: thiếu civilian_girl";
            Material srcBody = null, srcSkin = null;
            foreach (var r in girl.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.name == "body") srcBody = m; else if (m.name == "body_skin") srcSkin = m;
                }
            if (srcBody == null || srcSkin == null) return "PlayerLook: thiếu vật liệu body/body_skin";

            BlackwoodDataBuilder.EnsureFolder(TexDir);
            var pores = BlackwoodPropTextures.DetailNormal("skin", TexDir + "/T_PL_SkinDetail_N.png", 5f);
            var weave = BlackwoodPropTextures.DetailNormal("cloth", TexDir + "/T_PL_FabricDetail_N.png", 4f);
            PaintCoat(girl, srcBody, force, out var eA, out var eN, out var eM, out var eSkinMask);

            var alexBody = Variant("M_PL_Alex_Body", srcBody); Detail(alexBody, weave, 26f, 0.55f, null);
            var alexSkin = Variant("M_PL_Alex_Skin", srcSkin); Detail(alexSkin, pores, 30f, 0.6f, null);
            var evBody = Variant("M_PL_Evelyn_Body", srcBody);
            var evSkin = Variant("M_PL_Evelyn_Skin", srcSkin);
            foreach (var m in new[] { evBody, evSkin })
            {
                m.SetTexture("_BaseMap", eA); m.SetTexture("_MainTex", eA);
                m.SetTexture("_BumpMap", eN); m.SetTexture("_MetallicGlossMap", eM); m.SetTexture("_OcclusionMap", eM);
            }
            Detail(evBody, weave, 26f, 0.4f, null);
            Detail(evSkin, pores, 30f, 0.6f, eSkinMask);
            AssetDatabase.SaveAssets();

            if (player != null)
            {
                var look = player.GetComponent<Blackwood.Characters.PlayerLook>();
                if (look == null) look = player.AddComponent<Blackwood.Characters.PlayerLook>();
                var so = new SerializedObject(look);
                var b = so.FindProperty("bodyMaterials"); b.arraySize = 2;
                b.GetArrayElementAtIndex(0).objectReferenceValue = evBody; b.GetArrayElementAtIndex(1).objectReferenceValue = alexBody;
                var s = so.FindProperty("skinMaterials"); s.arraySize = 2;
                s.GetArrayElementAtIndex(0).objectReferenceValue = evSkin; s.GetArrayElementAtIndex(1).objectReferenceValue = alexSkin;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(look);
            }
            return "PlayerLook ok (Evelyn: áo khoác dài tay, Alex: tay trần; chi tiết da/vải)";
        }

        /// <summary>Xem trước hai nhân vật cạnh nhau (trái Evelyn, phải Alex) + cận tay: Temp/Blackwood/pl_*.png.</summary>
        [MenuItem("Blackwood/Build/Preview Player Look")]
        public static void PreviewMenu() => Debug.Log(Preview());

        public static string Preview()
        {
            var girl = AssetDatabase.LoadAssetAtPath<GameObject>(GirlPath);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject New(string name) { var g = new GameObject(name); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scene); return g; }
                for (int c = 0; c < 2; c++)
                {
                    string who = c == 0 ? "Evelyn" : "Alex";
                    var body = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_PL_" + who + "_Body.mat");
                    var skin = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/M_PL_" + who + "_Skin.mat");
                    var g = Object.Instantiate(girl);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scene);
                    g.transform.SetPositionAndRotation(new Vector3(c == 0 ? -0.75f : 0.75f, 0f, 0f), Quaternion.identity);
                    foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                    {
                        var arr = r.sharedMaterials;
                        for (int i = 0; i < arr.Length; i++)
                            if (arr[i] != null) { if (arr[i].name == "body") arr[i] = body; else if (arr[i].name == "body_skin") arr[i] = skin; }
                        r.sharedMaterials = arr;
                    }
                }
                var key = New("L").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.5f; key.color = new Color(1f, 0.96f, 0.9f);
                key.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
                var fill = New("F").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = 0.45f; fill.color = new Color(0.6f, 0.7f, 0.9f);
                fill.transform.rotation = Quaternion.Euler(15f, -40f, 0f);
                var camGo = New("C"); var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.21f, 0.23f);
                cam.fieldOfView = 30f; cam.nearClipPlane = 0.02f; cam.farClipPlane = 50f;
                Directory.CreateDirectory("Temp/Blackwood");
                { var warm = RenderTexture.GetTemporary(64, 64, 24); cam.targetTexture = warm; cam.Render(); cam.targetTexture = null; RenderTexture.ReleaseTemporary(warm); }
                var shots = new (string name, Vector3 pos, Vector3 look)[]
                {
                    ("full", new Vector3(0f, 1.2f, 4.6f), new Vector3(0f, 0.95f, 0f)),
                    ("evelyn_arm", new Vector3(-1.05f, 1.15f, 0.75f), new Vector3(-1.38f, 1.02f, 0f)),
                    ("alex_arm", new Vector3(0.45f, 1.15f, 0.75f), new Vector3(0.12f, 1.02f, 0f)),
                };
                foreach (var s in shots)
                {
                    camGo.transform.position = s.pos; camGo.transform.LookAt(s.look);
                    var rt = RenderTexture.GetTemporary(1100, 1000, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt; cam.Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var tex = new Texture2D(1100, 1000, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1100, 1000), 0, 0); tex.Apply();
                    RenderTexture.active = prev; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                    File.WriteAllBytes("Temp/Blackwood/pl_" + s.name + ".png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            return "Preview player ok";
        }
    }
}
