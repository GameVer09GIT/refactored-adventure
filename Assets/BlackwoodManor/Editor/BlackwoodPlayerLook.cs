using System.Collections.Generic;
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
    ///  • Alex: áo nỉ xám than dài tay + găng tay da (vùng bàn tay sơn màu da thuộc, bóng hơn, vân da) — bàn tay nữ của model gốc
    ///    không còn lộ ra; tay áo/găng 3D gắn thêm bởi <see cref="BlackwoodPlayerGarments"/> dùng chung vật liệu này.
    /// Vật liệu ra ở Materials/Player, component <see cref="Blackwood.Characters.PlayerLook"/> đổi theo nhân vật lúc chạy.
    /// </summary>
    public static class BlackwoodPlayerLook
    {
        const string TexDir = "Assets/BlackwoodManor/Textures/Player";
        const string MatDir = "Assets/BlackwoodManor/Materials/Player";
        const string GirlPath = "Assets/ThirdParty/civilian_girl/Prefabs/civilian_girl.prefab";
        const int S = 2048;
        static readonly Color Coat = new Color(0.125f, 0.105f, 0.092f);
        static readonly Color Sweat = new Color(0.082f, 0.086f, 0.084f);
        static readonly Color Glove = new Color(0.042f, 0.036f, 0.033f);

        /// <summary>Tô từng điểm ảnh UV của một tam giác (nới nhẹ mép), gọi f(i, wa, wb, wc).</summary>
        static void Raster(Vector2 a, Vector2 b, Vector2 c, int size, System.Action<int, float, float, float> f)
        {
            a *= size; b *= size; c *= size;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))) - 1), x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))) + 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))) - 1), y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))) + 1);
            float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(den) < 1e-6f) return;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float wa = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / den;
                    float wb = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / den;
                    float wc = 1f - wa - wb;
                    if (wa < -0.02f || wb < -0.02f || wc < -0.02f) continue;
                    f(y * size + x, wa, wb, wc);
                }
        }

        /// <summary>
        /// Mặt nạ găng tay trên texture thân: vùng lưới tay có trọng số xương bàn tay ≥ 0,5 (đúng phần ClothMask để là da),
        /// nở thêm 3 px vào vùng UV trống để mip không lộ viền da.
        /// </summary>
        static float[] HandMask(GameObject girl, int size)
        {
            var m = new float[size * size];
            var used = new bool[size * size];
            foreach (var smr in girl.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || smr.sharedMaterial == null || (smr.sharedMaterial.name != "body" && smr.sharedMaterial.name != "body_skin")) continue;
                var uv = mesh.uv; var tris = mesh.triangles;
                if (uv == null || uv.Length == 0) continue;
                float[] k = null;
                if (smr.name.StartsWith("hand_"))
                {
                    var bones = smr.bones; var bw = mesh.boneWeights;
                    var hb = new bool[bones.Length];
                    for (int b = 0; b < bones.Length; b++)
                    {
                        string bn = bones[b] != null ? bones[b].name : "";
                        hb[b] = bn.StartsWith("hand_") || bn.StartsWith("thumb") || bn.StartsWith("index") || bn.StartsWith("middle") || bn.StartsWith("ring") || bn.StartsWith("pinky");
                    }
                    k = new float[uv.Length];
                    for (int v = 0; v < k.Length && v < bw.Length; v++)
                    {
                        var w = bw[v]; float s = 0f;
                        if (hb[w.boneIndex0]) s += w.weight0; if (hb[w.boneIndex1]) s += w.weight1;
                        if (hb[w.boneIndex2]) s += w.weight2; if (hb[w.boneIndex3]) s += w.weight3;
                        k[v] = s;
                    }
                }
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int ia = tris[t], ib = tris[t + 1], ic = tris[t + 2];
                    var kk = k;
                    Raster(uv[ia], uv[ib], uv[ic], size, (i, wa, wb, wc) =>
                    {
                        used[i] = true;
                        if (kk != null && kk[ia] * wa + kk[ib] * wb + kk[ic] * wc >= 0.5f) m[i] = 1f;
                    });
                }
            }
            for (int pass = 0; pass < 3; pass++)
            {
                var add = new List<int>();
                for (int y = 1; y < size - 1; y++)
                    for (int x = 1; x < size - 1; x++)
                    {
                        int i = y * size + x;
                        if (used[i] || m[i] > 0f) continue;
                        if (m[i - 1] > 0f || m[i + 1] > 0f || m[i - size] > 0f || m[i + size] > 0f) add.Add(i);
                    }
                foreach (int i in add) m[i] = 1f;
            }
            return m;
        }

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

        /// <summary>
        /// Sơn bộ đồ dài tay (màu <paramref name="clothColor"/>) vào bản sao texture thân; <paramref name="gloves"/> thì sơn thêm găng da ở bàn tay.
        /// Trả về albedo/normal/mask + mặt nạ chi tiết da (chỉ còn phần da thật: mặt, cổ, bàn tay trần).
        /// </summary>
        static void PaintOutfit(GameObject girl, Material srcBody, string who, Color clothColor, bool gloves, bool force,
            out Texture2D albedo, out Texture2D normal, out Texture2D mask, out Texture2D skinMask)
        {
            Texture2D Load(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/T_PL_" + who + "_" + n + ".png");
            albedo = Load("A"); normal = Load("N"); mask = Load("M"); skinMask = Load("SkinMask");
            if (!force && albedo != null && normal != null && mask != null && skinMask != null) return;

            BlackwoodHousekeeperLook.ClothMask(girl, S, out var cloth, out var cuff, new[] { "shirt_t", "hand_", "chest" });
            var glove = gloves ? HandMask(girl, S) : null;
            var a = Read(srcBody.GetTexture("_BaseMap"), S, true);
            var n = Read(srcBody.GetTexture("_BumpMap"), S, false);
            var k = Read(srcBody.GetTexture("_MetallicGlossMap"), S, false);
            var wv = BlackwoodPropTextures.ClassHeight("cloth"); int W = BlackwoodPropTextures.Size;
            var lv = BlackwoodPropTextures.ClassHeight("leather");
            float Weave(int x, int y) => wv[((y * 2) % W + W) % W * W + ((x * 2) % W + W) % W];
            float Grain(int x, int y) => lv[((y * 7) % W + W) % W * W + ((x * 7) % W + W) % W];
            var Coat = clothColor;

            var sm = new Color[512 * 512];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int i = y * S + x;
                    // normal gốc (giải nén RG hoặc AG)
                    float nx = n[i].r * n[i].a * 2f - 1f, ny = n[i].g * 2f - 1f;
                    float c = cloth[i];
                    float g = glove != null ? glove[i] * (1f - c) : 0f;
                    if (g > 0f)
                    {
                        // Găng da thuộc: tối, vân da, bóng vừa, giữ chút nếp gấp từ normal gốc của bàn tay
                        float h = Grain(x, y);
                        float t = 0.92f + 0.16f * h;
                        a[i] = new Color(Glove.r * t, Glove.g * t, Glove.b * t, 1f);
                        float gx = (Grain(x - 1, y) - Grain(x + 1, y)) * 0.35f, gy = (Grain(x, y - 1) - Grain(x, y + 1)) * 0.35f;
                        nx = nx * 0.85f + gx; ny = ny * 0.85f + gy;
                        k[i] = new Color(0f, k[i].g, 0f, 0.36f + 0.06f * h);
                    }
                    else if (c > 0f)
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
                    if ((x & 3) == 0 && (y & 3) == 0) { float s = 1f - Mathf.Max(c, g); sm[(y >> 2) * 512 + (x >> 2)] = new Color(s, s, s, s); }
                }
            albedo = Save("T_PL_" + who + "_A", a, S, true, false);
            normal = Save("T_PL_" + who + "_N", n, S, false, true);
            mask = Save("T_PL_" + who + "_M", k, S, false, false);
            skinMask = Save("T_PL_" + who + "_SkinMask", sm, 512, false, false);
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
            PaintOutfit(girl, srcBody, "Evelyn", Coat, false, force, out var eA, out var eN, out var eM, out var eSkinMask);
            PaintOutfit(girl, srcBody, "Alex", Sweat, true, force, out var aA, out var aN, out var aM, out var aSkinMask);

            var alexBody = Variant("M_PL_Alex_Body", srcBody);
            var alexSkin = Variant("M_PL_Alex_Skin", srcSkin);
            var evBody = Variant("M_PL_Evelyn_Body", srcBody);
            var evSkin = Variant("M_PL_Evelyn_Skin", srcSkin);
            void Use(Material m, Texture2D A, Texture2D N, Texture2D M)
            {
                m.SetTexture("_BaseMap", A); m.SetTexture("_MainTex", A);
                m.SetTexture("_BumpMap", N); m.SetTexture("_MetallicGlossMap", M); m.SetTexture("_OcclusionMap", M);
            }
            Use(evBody, eA, eN, eM); Use(evSkin, eA, eN, eM);
            Use(alexBody, aA, aN, aM); Use(alexSkin, aA, aN, aM);
            Detail(evBody, weave, 26f, 0.4f, null);
            Detail(evSkin, pores, 30f, 0.6f, eSkinMask);
            Detail(alexBody, weave, 26f, 0.45f, null);
            Detail(alexSkin, pores, 30f, 0.6f, aSkinMask);
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
            return "PlayerLook ok (Evelyn: áo khoác len, Alex: áo nỉ + găng da; chi tiết da/vải)";
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
                    var gl = BlackwoodPlayerGarments.Dress(g, out _);
                    foreach (var r in gl) r.enabled = c == 1;
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
