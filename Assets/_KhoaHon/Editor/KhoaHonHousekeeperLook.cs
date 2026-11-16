using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Ngoại hình "bà quản gia thời Victoria đã chết" cho Mrs. Peake:
    ///  • váy tang đen dài + khăn choàng + tóc đen dài rũ che mặt — lưới dựng bằng Blender (Tools/Blender/housekeeper_garments.py),
    ///    ở đây gắn lại vào đúng khung xương civilian_girl (tính lại bindpose theo xương gốc nên không lệch dù đi qua Blender);
    ///  • thân áo bó + tay áo dài + cổ cao: "vẽ" thẳng lên texture thân theo vùng UV của lưới gốc (không cần lưới mới, không xuyên hình);
    ///  • texture vải đen sờn rách (alpha gấu váy), texture lọn tóc.
    /// Không sửa file asset pack — mọi thứ ghi vào Assets/_KhoaHon.
    /// </summary>
    public static class KhoaHonHousekeeperLook
    {
        public const string MeshDir = "Assets/_KhoaHon/Models/Housekeeper";
        const string MatDir = "Assets/_KhoaHon/Materials/Housekeeper";
        const string GarmentFbxName = "KH_HK_Garments.fbx";

        static float Sm(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        static float Fbm(float x, float y, int oct = 4)
        {
            float a = 0.5f, s = 0f, f = 1f, n = 0f;
            for (int i = 0; i < oct; i++) { s += a * Mathf.PerlinNoise(x * f + 13.7f, y * f + 71.3f); n += a; a *= 0.5f; f *= 2.03f; }
            return s / n;
        }

        // =====================================================================
        // 1) Mặt nạ vùng "mặc vải" trên texture thân (UV của lưới gốc)
        /// <summary>
        /// Rasterize tam giác UV của các lưới thân: cloth[i] = 1 ở vùng phải thành vải đen (áo, quần, ngực trên, cánh tay tới cổ tay),
        /// trim[i] = 1 ở viền cổ tay / cổ áo (ren trắng ngà bẩn).
        /// </summary>
        public static void ClothMask(GameObject girl, int size, out float[] cloth, out float[] trim)
        {
            cloth = new float[size * size]; trim = new float[size * size];
            var covered = new bool[size * size];
            foreach (var smr in girl.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string n = smr.name;
                bool whole = n == "shirt_t" || n == "pant";
                bool chest = n == "chest";
                bool arm = n.StartsWith("hand_");
                bool boot = n.StartsWith("boot_");
                if (!whole && !chest && !arm && !boot) continue;
                var mesh = smr.sharedMesh;
                var uv = mesh.uv; var tris = mesh.triangles; var bw = mesh.boneWeights; var bones = smr.bones;
                if (uv == null || uv.Length == 0) continue;
                // k[v]: 0 = vải, 1 = da (bàn tay / cổ)
                var k = new float[uv.Length];
                if (arm || chest)
                {
                    var skinBone = new bool[bones.Length];
                    for (int b = 0; b < bones.Length; b++)
                    {
                        string bn = bones[b] != null ? bones[b].name : "";
                        skinBone[b] = arm
                            ? bn.StartsWith("hand_") || bn.StartsWith("thumb") || bn.StartsWith("index") || bn.StartsWith("middle") || bn.StartsWith("ring") || bn.StartsWith("pinky")
                            : bn.StartsWith("neck") || bn == "head";
                    }
                    for (int v = 0; v < k.Length && v < bw.Length; v++)
                    {
                        var w = bw[v]; float s = 0f;
                        if (skinBone[w.boneIndex0]) s += w.weight0; if (skinBone[w.boneIndex1]) s += w.weight1;
                        if (skinBone[w.boneIndex2]) s += w.weight2; if (skinBone[w.boneIndex3]) s += w.weight3;
                        k[v] = s;
                    }
                }
                float edge = arm ? 0.5f : 0.62f;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int ia = tris[t], ib = tris[t + 1], ic = tris[t + 2];
                    Vector2 a = uv[ia] * size, b = uv[ib] * size, c = uv[ic] * size;
                    int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))) - 1), x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))) + 1);
                    int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))) - 1), y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))) + 1);
                    float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                    if (Mathf.Abs(den) < 1e-6f) continue;
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                        {
                            float px = x + 0.5f, py = y + 0.5f;
                            float wa = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / den;
                            float wb = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / den;
                            float wc = 1f - wa - wb;
                            const float eps = -0.02f; // nới nhẹ để phủ kín mép tam giác
                            if (wa < eps || wb < eps || wc < eps) continue;
                            int i = y * size + x;
                            covered[i] = true;
                            if (boot) { cloth[i] = 1f; continue; } // bốt: da đen cũ (cùng tông váy), không lộ màu gốc dưới gấu váy
                            float kk = k[ia] * wa + k[ib] * wb + k[ic] * wc;
                            if (kk < edge) cloth[i] = 1f;
                            if (kk > edge - 0.2f && kk < edge) trim[i] = Mathf.Max(trim[i], Sm(edge - 0.2f, edge - 0.08f, kk));
                        }
                }
            }
            // Nở mặt nạ ra vùng trống quanh đảo UV (tránh lộ viền màu gốc ở đường may khi mip)
            for (int pass = 0; pass < 4; pass++)
            {
                var add = new List<int>();
                for (int y = 1; y < size - 1; y++)
                    for (int x = 1; x < size - 1; x++)
                    {
                        int i = y * size + x;
                        if (covered[i] || cloth[i] > 0f) continue;
                        if (cloth[i - 1] > 0f || cloth[i + 1] > 0f || cloth[i - size] > 0f || cloth[i + size] > 0f) add.Add(i);
                    }
                foreach (int i in add) cloth[i] = 1f;
            }
        }

        // =====================================================================
        // 2) Texture vải váy / khăn (alpha gấu rách) + normal, texture lọn tóc
        public static Texture2D ClothTexture(out Texture2D normal)
        {
            const int S = 1024;
            var px = new Color[S * S]; var h = new float[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S, v = (y + 0.5f) / S; // v = 0: gấu váy, v = 1: eo
                    // Sợi dệt chéo + nếp nhăn dọc
                    float weave = 0.5f + 0.25f * Mathf.Sin((x + y) * 1.9f) * Mathf.Sin((x - y) * 1.9f);
                    float fold = Fbm(u * 9f, v * 1.6f, 4);
                    float worn = Fbm(u * 5f + 3f, v * 5f, 4);
                    float b = 0.030f + 0.018f * weave + 0.035f * fold;
                    var c = new Color(b * 0.95f, b * 0.95f, b * 1.06f);
                    // Bạc màu, sờn xám ở chỗ cọ xát
                    c = Color.Lerp(c, new Color(0.13f, 0.125f, 0.12f), Sm(0.55f, 0.8f, worn) * 0.55f);
                    // Bùn đất + tro bám về phía gấu
                    float mud = Sm(0.42f, 0.0f, v) * Sm(0.3f, 0.65f, Fbm(u * 7f, v * 4f + 9f));
                    c = Color.Lerp(c, new Color(0.17f, 0.14f, 0.10f), mud * 0.8f);
                    // Máu khô thấm sẫm (đỏ nâu rất tối — chỉ lộ khi rọi đèn)
                    float bl = Sm(0.6f, 0.7f, Fbm(u * 3.2f + 5f, v * 3.2f + 1f) * 0.8f + Fbm(u * 14f, v * 14f) * 0.2f);
                    float drip = Sm(0.9f, 0.97f, 1f - Mathf.Abs(2f * Fbm(u * 34f, v * 1.8f) - 1f)) * Sm(0.4f, 0.6f, Fbm(u * 4f, v * 2f + 3f));
                    c = Color.Lerp(c, new Color(0.14f, 0.012f, 0.012f), Mathf.Max(bl, drip * 0.8f) * 0.85f);
                    // Alpha: gấu lởm chởm + vết rách dọc + lỗ thủng
                    float rag = v - (0.02f + 0.10f * Fbm(u * 14f, 0.5f, 3) + 0.04f * Fbm(u * 60f, 2.5f, 2));
                    float slit = Sm(0.93f, 0.97f, 1f - Mathf.Abs(2f * Fbm(u * 22f, 7.7f, 2) - 1f)) * Sm(0.5f, 0.12f, v);
                    float hole = Sm(0.76f, 0.8f, Fbm(u * 18f + 4f, v * 18f, 3)) * Sm(0.62f, 0.2f, v);
                    float a = rag > 0f ? 1f : 0f;
                    if (slit > 0.5f || hole > 0.5f) a = 0f;
                    if (v > 0.9f) a = 1f;
                    // Mép rách sáng hơn (sợi tưa)
                    if (a > 0f && rag < 0.015f) c = Color.Lerp(c, new Color(0.1f, 0.095f, 0.09f), 0.5f);
                    c.a = a; px[y * S + x] = c;
                    h[y * S + x] = fold * 0.7f + weave * 0.08f + worn * 0.25f;
                }
            var albedo = SaveTex("T_HK_Cloth", px, S, true, false);
            normal = SaveTex("T_HK_Cloth_N", HeightToNormal(h, S, 5f), S, false, true);
            return albedo;
        }

        public static Texture2D HairTexture()
        {
            const int W = 256, H = 1024;
            var px = new Color[W * H];
            var rnd = new System.Random(77);
            // Mỗi nửa texture (u 0–0.5 và 0.5–1) là một biến thể lọn tóc gồm nhiều sợi mảnh tách rời, dài ngắn khác nhau
            var dens = new float[W]; var len = new float[W]; var tone = new float[W];
            for (int half = 0; half < 2; half++)
            {
                int x0 = half * W / 2, n = W / 2;
                int strands = 11;
                for (int k = 0; k < strands; k++)
                {
                    float cx = x0 + n * (0.08f + 0.84f * ((k + (float)rnd.NextDouble() * 0.8f) / strands));
                    float wd = 1.6f + (float)rnd.NextDouble() * 4.2f;         // bề rộng sợi (px)
                    float ln = 0.02f + 0.42f * (float)rnd.NextDouble() * (float)rnd.NextDouble(); // v nơi sợi kết thúc
                    float tn = (float)rnd.NextDouble();
                    for (int x = Mathf.Max(x0, (int)(cx - wd)); x <= Mathf.Min(x0 + n - 1, (int)(cx + wd)); x++)
                    {
                        float d = 1f - Mathf.Abs(x - cx) / wd;
                        if (d > dens[x]) { dens[x] = d; len[x] = ln; tone[x] = tn; }
                    }
                }
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / H; // v = 1: chân tóc, v = 0: ngọn
                    // Sợi lượn nhẹ theo chiều dài: lấy mẫu cột lệch đi một chút
                    int xs = Mathf.Clamp(x + Mathf.RoundToInt(Mathf.Sin(v * 19f + (x / (W / 2)) * 2.1f) * 1.6f), (x / (W / 2)) * (W / 2), (x / (W / 2) + 1) * (W / 2) - 1);
                    float d = dens[xs];
                    // Sợi mảnh dần về ngọn rồi dứt
                    float thin = Sm(len[xs], len[xs] + 0.35f, v);
                    float a = d > 0.55f - 0.45f * thin && v > len[xs] ? 1f : 0f;
                    if (v > 0.95f) a = 1f; // chân tóc đặc (bám da đầu)
                    float g = 0.016f + 0.03f * tone[xs] + 0.018f * Mathf.PerlinNoise(x * 0.5f, v * 22f);
                    // Bết: vài mảng ướt bóng sẫm; dính bụi xám
                    var c = new Color(g * 1.0f, g * 0.95f, g * 0.92f, a);
                    float dust = Sm(0.62f, 0.8f, Fbm(u * 3f, v * 5f));
                    c = Color.Lerp(c, new Color(0.12f, 0.11f, 0.10f, a), dust * 0.4f);
                    c.a = a;
                    px[y * W + x] = c;
                }
            KhoaHonDataBuilder.EnsureFolder(KhoaHonBansheeTextures.Dir);
            string path = KhoaHonBansheeTextures.Dir + "/T_HK_HairLong.png";
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + path), t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.sRGBTexture = true; ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.4f;
            ti.wrapMode = TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Color[] HeightToNormal(float[] h, int S, float strength)
        {
            var o = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float l = h[y * S + (x + S - 1) % S], r = h[y * S + (x + 1) % S];
                    float d = h[((y + S - 1) % S) * S + x], u = h[((y + 1) % S) * S + x];
                    var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    o[y * S + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            return o;
        }

        static Texture2D SaveTex(string name, Color[] px, int size, bool alpha, bool isNormal)
        {
            KhoaHonDataBuilder.EnsureFolder(KhoaHonBansheeTextures.Dir);
            string path = KhoaHonBansheeTextures.Dir + "/" + name + ".png";
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + path), t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !isNormal; ti.alphaIsTransparency = alpha; ti.mipmapEnabled = true;
            if (alpha) { ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.5f; }
            ti.wrapMode = TextureWrapMode.Repeat; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // =====================================================================
        // 3) Vật liệu
        static Material ClipMat(string name, Texture2D albedo, Texture2D normal, float cutoff, float smooth)
        {
            KhoaHonDataBuilder.EnsureFolder(MatDir);
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
            m.shader = sh;
            m.SetTexture("_BaseMap", albedo); m.SetTexture("_MainTex", albedo); m.SetColor("_BaseColor", Color.white);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", cutoff); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f); // hai mặt: nhìn được mặt trong váy / mặt sau lọn tóc
            m.doubleSidedGI = true;
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            m.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            return m;
        }

        // =====================================================================
        // Xem trước nhanh (không đụng scene đang mở): dựng bà trong preview scene, chụp 3 góc vào Temp/KhoaHon/hk_look_*.png
        [MenuItem("KhoaHon/Build/Preview Housekeeper Look")]
        public static void PreviewMenu() => Debug.Log(Preview(true));

        public static string Preview(bool regenTextures, string clipPath = null, float clipTime = 0f)
        {
            var girlPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/civilian_girl/Prefabs/civilian_girl.prefab");
            if (girlPrefab == null) return "no civilian_girl prefab";
            var orig = new Dictionary<string, Material>();
            foreach (var r in girlPrefab.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m != null && !orig.ContainsKey(m.name)) orig[m.name] = m;
            if (regenTextures)
            {
                ClothMask(girlPrefab, 2048, out var cm, out var tm);
                KhoaHonBansheeTextures.Body(orig["body"].GetTexture("_BaseMap"), cm, tm);
                KhoaHonBansheeTextures.Head(orig["head"].GetTexture("_BaseMap"));
                KhoaHonBansheeTextures.Hair(orig["hair"].GetTexture("_BaseMap"));
            }
            var map = new Dictionary<string, string> { { "head", "M_HK_B_Head" }, { "body", "M_HK_B_Body" }, { "body_skin", "M_HK_B_Skin" }, { "hair", "M_HK_B_Hair" }, { "head_eyes", "M_HK_B_Eyes" }, { "eyelahes", "M_HK_B_Lashes" } };
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            string log;
            try
            {
                var girl = Object.Instantiate(girlPrefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(girl, scene);
                girl.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var r in girl.GetComponentsInChildren<Renderer>(true))
                {
                    var arr = r.sharedMaterials;
                    for (int i = 0; i < arr.Length; i++)
                        if (arr[i] != null && map.TryGetValue(arr[i].name, out var mn))
                        {
                            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + mn + ".mat");
                            if (m != null) arr[i] = m;
                        }
                    r.sharedMaterials = arr;
                }
                log = Dress(girl);
                girl.transform.localScale = new Vector3(0.96f, 1.08f, 0.96f);
                if (!string.IsNullOrEmpty(clipPath))
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                    if (clip != null) clip.SampleAnimation(girl, clipTime);
                }
                var lightGo = new GameObject("L"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, scene);
                var l = lightGo.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.6f; l.color = new Color(0.9f, 0.95f, 1f);
                lightGo.transform.rotation = Quaternion.Euler(28f, 160f, 0f);
                var fill = new GameObject("F"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fill, scene);
                var fl = fill.AddComponent<Light>(); fl.type = LightType.Directional; fl.intensity = 0.5f; fl.color = new Color(0.6f, 0.7f, 0.9f);
                fill.transform.rotation = Quaternion.Euler(15f, -40f, 0f);
                var camGo = new GameObject("C"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.22f, 0.24f, 0.27f);
                cam.fieldOfView = 32f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 50f;
                Directory.CreateDirectory("Temp/KhoaHon");
                { // khung đầu tiên của preview scene hay bị trống → render mồi một lần
                    var warm = RenderTexture.GetTemporary(64, 64, 24); cam.targetTexture = warm; cam.Render(); cam.targetTexture = null; RenderTexture.ReleaseTemporary(warm);
                }
                var shots = new (string name, Vector3 pos, Vector3 look)[]
                {
                    ("front", new Vector3(0.5f, 1.3f, 4.2f), new Vector3(0f, 1.05f, 0f)),
                    ("side", new Vector3(4.2f, 1.3f, 0.6f), new Vector3(0f, 1.05f, 0f)),
                    ("back", new Vector3(-0.8f, 1.5f, -4.2f), new Vector3(0f, 1.05f, 0f)),
                    ("face", new Vector3(0.12f, 1.98f, 0.95f), new Vector3(0f, 1.93f, 0f)),
                    ("upper", new Vector3(0.55f, 1.75f, 1.9f), new Vector3(0f, 1.62f, 0f)),
                };
                foreach (var s in shots)
                {
                    camGo.transform.position = s.pos; camGo.transform.LookAt(s.look);
                    var rt = RenderTexture.GetTemporary(900, 1200, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt; cam.Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var tex = new Texture2D(900, 1200, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 900, 1200), 0, 0); tex.Apply();
                    RenderTexture.active = prev; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                    File.WriteAllBytes("Temp/KhoaHon/hk_look_" + s.name + ".png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            return log;
        }

        // =====================================================================
        // 4) Gắn lưới Blender vào khung xương của bà
        /// <summary>Chép FBX trang phục từ Tools/Blender/out/characters vào project (rig Generic để giữ skin).</summary>
        static GameObject ImportGarments()
        {
            KhoaHonDataBuilder.EnsureFolder(MeshDir);
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/Blender/out/characters/" + GarmentFbxName));
            string dst = MeshDir + "/" + GarmentFbxName;
            if (File.Exists(src))
            {
                File.Copy(src, dst, true);
                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                var imp = (ModelImporter)AssetImporter.GetAtPath(dst);
                if (imp != null)
                {
                    imp.animationType = ModelImporterAnimationType.Generic;
                    imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None;
                    imp.isReadable = true; imp.importBlendShapes = false; imp.optimizeMeshVertices = false;
                    imp.importNormals = ModelImporterNormals.Calculate; imp.normalSmoothingAngle = 80f;
                    imp.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(dst);
        }

        /// <summary>
        /// Mặc đồ cho bà: gắn váy/khăn/tóc vào <paramref name="girl"/> (bản civilian_girl đã unpack, đang ở tư thế bind).
        /// Trả về mô tả kết quả để ghi log.
        /// </summary>
        public static string Dress(GameObject girl)
        {
            var fbx = ImportGarments();
            if (fbx == null) return "WARN: chưa có " + GarmentFbxName + " (chạy Tools/Blender/housekeeper_garments.py)";
            var clothTex = ClothTexture(out var clothN);
            var hairTex = HairTexture();
            var clothMat = ClipMat("M_HK_Cloth", clothTex, clothN, 0.5f, 0.12f);
            var hairMat = ClipMat("M_HK_HairLong", hairTex, null, 0.38f, 0.28f);

            // Xương gốc theo tên + ma trận world lúc bind (bà đang đứng ở tư thế bind, nhưng có scale/pos riêng → tính trong hệ của girl)
            var boneByName = new Dictionary<string, Transform>();
            foreach (var t in girl.GetComponentsInChildren<Transform>(true)) if (!boneByName.ContainsKey(t.name)) boneByName[t.name] = t;
            var girlInv = girl.transform.worldToLocalMatrix;

            var inst = (GameObject)Object.Instantiate(fbx);
            inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            inst.transform.localScale = Vector3.one;
            var report = new List<string>();
            try
            {
                foreach (var src in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var sm = src.sharedMesh; var sb = src.bones; var sbind = sm.bindposes;
                    if (sb.Length == 0) continue;
                    // Ma trận "không gian lưới → không gian gốc model" của FBX trang phục lúc bind (như nhau với mọi xương)
                    Matrix4x4 Ld = inst.transform.worldToLocalMatrix * sb[0].localToWorldMatrix * sbind[0];
                    var newBones = new Transform[sb.Length]; var newBind = new Matrix4x4[sb.Length];
                    int missing = 0;
                    for (int i = 0; i < sb.Length; i++)
                    {
                        Transform ob = null;
                        if (sb[i] != null) boneByName.TryGetValue(sb[i].name, out ob);
                        if (ob == null) { missing++; ob = boneByName.ContainsKey("pelvis") ? boneByName["pelvis"] : girl.transform; }
                        newBones[i] = ob;
                        // xương gốc trong hệ của girl (bỏ scale/vị trí của bà) → bindpose mới
                        newBind[i] = (girlInv * ob.localToWorldMatrix).inverse * Ld;
                    }
                    var mesh = Object.Instantiate(sm);
                    mesh.name = src.name;
                    mesh.bindposes = newBind;
                    mesh.RecalculateBounds();
                    string mp = MeshDir + "/" + src.name + ".asset";
                    AssetDatabase.DeleteAsset(mp);
                    AssetDatabase.CreateAsset(mesh, mp);

                    var old = girl.transform.Find(src.name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    var go = new GameObject(src.name);
                    go.transform.SetParent(girl.transform, false);
                    var smr = go.AddComponent<SkinnedMeshRenderer>();
                    smr.sharedMesh = mesh; smr.bones = newBones;
                    smr.rootBone = boneByName.ContainsKey("pelvis") ? boneByName["pelvis"] : girl.transform;
                    smr.sharedMaterial = src.name.Contains("Hair") ? hairMat : clothMat;
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    smr.updateWhenOffscreen = false;
                    smr.localBounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(2.4f, 2.4f, 2.6f)); // rộng rãi: không bị cull khi vung tay/bước
                    smr.quality = SkinQuality.Bone4;
                    report.Add($"{src.name}:{mesh.vertexCount}v/{sb.Length}b" + (missing > 0 ? $"(thiếu {missing} xương)" : ""));
                }
                // Kiểm tra khớp vị trí: xương đầu của FBX trang phục so với xương đầu gốc (cùng hệ model)
                var dHead = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "head");
                if (dHead != null && boneByName.TryGetValue("head", out var oHead))
                {
                    var a = inst.transform.InverseTransformPoint(dHead.position);
                    var b = girlInv.MultiplyPoint3x4(oHead.position);
                    report.Add($"lệch xương đầu {(a - b).magnitude * 100f:F1} cm");
                }
            }
            finally { Object.DestroyImmediate(inst); }
            AssetDatabase.SaveAssets();
            return "Dress: " + string.Join(", ", report);
        }
    }
}
