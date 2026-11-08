using System.IO;
using UnityEditor;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Biến texture gốc của civilian_girl thành "xác chết hiện hồn" kiểu Banshee mà không sửa file của asset pack:
    /// đọc texture qua GPU (Blit), xử lý trên CPU rồi ghi bản mới vào Assets/_KhoaHon/Textures/Housekeeper.
    /// Mặt: da xám tái có gân xanh, hốc mắt thâm đen, nước mắt máu, môi tím tái, máu trào từ miệng xuống cổ, tóc bạc.
    /// Mắt: tròng trắng đục. Thân: da tử thi, quần áo thành vải trắng ngà bẩn thấm máu loang.
    /// Toạ độ UV của mặt/mắt/răng đo trực tiếp trên head_Albedo (4096²).
    /// </summary>
    public static class KhoaHonBansheeTextures
    {
        public const string Dir = "Assets/_KhoaHon/Textures/Housekeeper";
        const int Size = 2048;

        static float sx, sy;

        static float Sm(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        static float Fbm(float x, float y, int oct = 4)
        {
            float a = 0.5f, s = 0f, f = 1f, n = 0f;
            for (int i = 0; i < oct; i++) { s += a * Mathf.PerlinNoise(x * f + sx, y * f + sy); n += a; a *= 0.5f; f *= 2.03f; }
            return s / n;
        }

        static float Ridge(float x, float y) => 1f - Mathf.Abs(2f * Fbm(x, y, 4) - 1f);

        static Color[] Read(Texture src, int size)
        {
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, size, size), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var px = t.GetPixels(); Object.DestroyImmediate(t);
            return px;
        }

        static Texture2D Save(string name, Color[] px, int size, bool alpha)
        {
            KhoaHonDataBuilder.EnsureFolder(Dir);
            string path = Dir + "/" + name + ".png";
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + path), t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.sRGBTexture = true; ti.alphaIsTransparency = alpha; ti.mipmapEnabled = true; ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static readonly Color BloodWet = new Color(0.30f, 0.012f, 0.01f);
        static readonly Color BloodDry = new Color(0.16f, 0.035f, 0.025f);

        /// <summary>Da tử thi: bỏ màu hồng, xám xanh tái, vết bầm tím, gân xanh đen.</summary>
        static Color Corpse(Color c, float u, float v, float veinScale)
        {
            float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            float b = Mathf.Clamp01(lum * 0.98f + 0.03f);
            var o = new Color(b * 0.80f, b * 0.86f, b * 0.86f);
            float bruise = Sm(0.58f, 0.75f, Fbm(u * 6f + 11f, v * 6f));
            o = Color.Lerp(o, new Color(0.30f, 0.24f, 0.30f), bruise * 0.45f);
            float vein = Sm(0.945f, 0.985f, Ridge(u * veinScale, v * veinScale)) * Sm(0.52f, 0.68f, Fbm(u * 3f, v * 3f + 7f));
            o = Color.Lerp(o, new Color(0.2f, 0.24f, 0.32f), vein * 0.4f);
            float mottle = 0.88f + 0.2f * Fbm(u * 40f, v * 40f, 3);
            return new Color(o.r * mottle, o.g * mottle, o.b * mottle, c.a);
        }

        static Color Blood(Color c, float amount, float u, float v)
        {
            if (amount <= 0f) return c;
            float wet = Fbm(u * 30f + 3f, v * 30f, 3);
            var bc = Color.Lerp(BloodDry, BloodWet, Sm(0.35f, 0.7f, wet) * Sm(0.3f, 0.9f, amount));
            var o = Color.Lerp(c, bc, Sm(0f, 0.35f, amount));
            o.a = c.a;
            return o;
        }

        // ------------------------------------------------------------------ Đầu
        public static Texture2D Head(Texture src)
        {
            sx = 31.7f; sy = 4.2f;
            var px = Read(src, Size);
            var rnd = new System.Random(5);
            // Vệt máu chảy: nước mắt máu (2 mắt), khóe miệng, cằm xuống cổ
            var streaks = new System.Collections.Generic.List<Vector4>(); // x, yTop, yBottom, width
            foreach (float ex in new[] { 0.395f, 0.605f })
                for (int i = 0; i < 3; i++) streaks.Add(new Vector4(ex + (float)(rnd.NextDouble() - 0.5) * 0.03f, 0.715f, 0.6f - (float)rnd.NextDouble() * 0.08f, 0.004f + (float)rnd.NextDouble() * 0.004f));
            for (int i = 0; i < 6; i++) streaks.Add(new Vector4(0.455f + (float)rnd.NextDouble() * 0.09f, 0.565f, 0.36f - (float)rnd.NextDouble() * 0.12f, 0.003f + (float)rnd.NextDouble() * 0.004f));
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                    var c = px[i];
                    float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    // Tròng mắt (vùng texture mắt dưới trái): trắng đục, mạch máu đỏ quanh rìa
                    float de = Vector2.Distance(new Vector2(u, v), new Vector2(0.225f, 0.1f));
                    if (u < 0.36f && v < 0.21f && de < 0.075f)
                    {
                        var milky = new Color(0.86f, 0.88f, 0.9f) * (0.92f + 0.1f * Fbm(u * 80f, v * 80f, 2));
                        float iris = Sm(0.05f, 0.03f, de);
                        var o = Color.Lerp(c, milky, Sm(0.07f, 0.05f, de));
                        o = Color.Lerp(o, new Color(0.7f, 0.74f, 0.78f), iris * 0.6f);
                        float vessel = Sm(0.92f, 0.98f, Ridge(u * 60f, v * 60f)) * Sm(0.035f, 0.065f, de);
                        o = Color.Lerp(o, new Color(0.55f, 0.06f, 0.05f), vessel * 0.8f);
                        o.a = c.a; px[i] = o; continue;
                    }
                    // Khoang miệng (hàng dưới của atlas: lợi, lưỡi, vòm họng): tối sẫm như máu đông; răng vàng ố
                    if (v < 0.2f && (u > 0.36f || u < 0.1f))
                    {
                        bool tooth = lum > 0.55f && u > 0.72f;
                        var o = tooth ? new Color(0.58f * c.r, 0.5f * c.g, 0.32f * c.b, c.a)
                                      : new Color(0.16f + c.r * 0.12f, 0.02f + c.g * 0.03f, 0.02f + c.b * 0.03f, c.a);
                        if (tooth) o = Blood(o, Sm(0.5f, 0.68f, Fbm(u * 25f, v * 25f)) * 0.7f, u, v);
                        px[i] = o; continue;
                    }
                    // Tóc / lông mày: bạc trắng bết bẩn
                    bool hair = lum < 0.24f && v > 0.42f && (v > 0.8f || u < 0.27f || u > 0.73f || (v > 0.74f && v < 0.8f && Mathf.Abs(u - 0.5f) < 0.17f));
                    if (hair)
                    {
                        float g = 0.3f + lum * 1.2f + 0.12f * (Fbm(u * 50f, v * 8f, 3) - 0.5f);
                        px[i] = new Color(g * 0.92f, g * 0.9f, g * 0.84f, c.a); continue;
                    }
                    var col = Corpse(c, u, v, 22f);
                    // Hốc mắt thâm đen trũng sâu
                    foreach (float ex in new[] { 0.4f, 0.6f })
                    {
                        float dx = (u - ex) / 0.08f, dy = (v - 0.725f) / 0.055f;
                        float e = Mathf.Sqrt(dx * dx + dy * dy) + 0.25f * (Fbm(u * 18f, v * 18f) - 0.5f);
                        col = Color.Lerp(col, new Color(0.05f, 0.035f, 0.045f), Sm(1.05f, 0.35f, e) * 0.9f);
                    }
                    // Môi tím đen nứt nẻ
                    {
                        float dx = (u - 0.5f) / 0.08f, dy = (v - 0.583f) / 0.032f;
                        float e = Mathf.Sqrt(dx * dx + dy * dy);
                        col = Color.Lerp(col, new Color(0.12f, 0.06f, 0.08f), Sm(1.1f, 0.6f, e) * 0.9f);
                    }
                    // Máu: quanh miệng, cằm, nước mắt máu, cổ
                    float blood = 0f;
                    {
                        float dx = (u - 0.5f) / 0.085f, dy = (v - 0.552f) / 0.05f;
                        float e = Mathf.Sqrt(dx * dx + dy * dy) + 0.6f * (Fbm(u * 10f, v * 10f) - 0.5f);
                        blood = Mathf.Max(blood, Sm(1f, 0.45f, e) * Sm(0.38f, 0.62f, Fbm(u * 16f + 2f, v * 16f, 3)));
                    }
                    foreach (var s in streaks)
                    {
                        if (v > s.y || v < s.z) continue;
                        float t = Mathf.InverseLerp(s.y, s.z, v);
                        float w = s.w * Mathf.Lerp(1f, 0.6f, t);
                        float wob = 0.004f * (Fbm(v * 30f, s.x * 10f) - 0.5f) * 2f;
                        float d = Mathf.Abs(u - s.x - wob);
                        blood = Mathf.Max(blood, Sm(w, w * 0.4f, d));
                        float bead = Vector2.Distance(new Vector2(u, v), new Vector2(s.x + wob, s.z));
                        blood = Mathf.Max(blood, Sm(w * 1.8f, w * 0.8f, bead));
                    }
                    // Vết bắn li ti trên mặt
                    blood = Mathf.Max(blood, Sm(0.74f, 0.8f, Fbm(u * 45f, v * 45f, 3)) * Sm(0.3f, 0.6f, v));
                    px[i] = Blood(col, blood, u, v);
                }
            return Save("T_Banshee_Head", px, Size, false);
        }

        // ------------------------------------------------------------------ Thân
        public static Texture2D Body(Texture src)
        {
            sx = 77.1f; sy = 12.9f;
            var px = Read(src, Size);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                    var c = px[i];
                    float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    bool skin = c.r > c.g && c.g > c.b && (c.r - c.b) > 0.08f && lum > 0.33f && (c.r - c.g) < 0.25f;
                    bool boots = u < 0.27f && v > 0.72f;
                    Color o;
                    float blood;
                    if (skin)
                    {
                        o = Corpse(c, u, v, 14f);
                        // Vết cào: các đường mảnh đỏ sẫm
                        float scratch = Sm(0.965f, 0.99f, Ridge(u * 9f + v * 2f, v * 36f));
                        o = Color.Lerp(o, new Color(0.3f, 0.03f, 0.03f), scratch * 0.7f);
                        blood = Sm(0.6f, 0.72f, Fbm(u * 7f, v * 7f)) * 0.85f;
                    }
                    else if (boots)
                    {
                        o = new Color(c.r * 0.6f, c.g * 0.55f, c.b * 0.5f, c.a);
                        blood = Sm(0.6f, 0.75f, Fbm(u * 9f, v * 9f)) * 0.6f;
                    }
                    else
                    {
                        // Vải trắng ngà cũ: giữ nếp nhăn theo độ sáng gốc, ố vàng nâu, bụi bẩn
                        float w = Mathf.Clamp(0.6f + (lum - 0.2f) * 1.6f, 0.32f, 0.82f);
                        o = new Color(w * 0.96f, w * 0.93f, w * 0.84f, c.a);
                        float grime = Fbm(u * 6f + 2f, v * 6f, 4);
                        o = Color.Lerp(o, new Color(0.36f, 0.31f, 0.24f), Sm(0.45f, 0.75f, grime) * 0.55f);
                        float k = 0.85f + 0.25f * Fbm(u * 60f, v * 60f, 2);
                        o = new Color(o.r * k, o.g * k, o.b * k, c.a);
                        // Máu thấm loang lớn + vệt chảy dọc
                        float big = Sm(0.56f, 0.64f, Fbm(u * 4.5f, v * 4.5f) * 0.75f + Fbm(u * 18f, v * 18f) * 0.25f);
                        float drip = Sm(0.88f, 0.96f, Ridge(u * 40f, v * 2.2f)) * Sm(0.45f, 0.6f, Fbm(u * 5f, v * 3f + 4f));
                        blood = Mathf.Max(big, drip * 0.9f);
                        // Viền khô sẫm quanh vết thấm
                        float rim = Sm(0.5f, 0.56f, Fbm(u * 4.5f, v * 4.5f) * 0.75f + Fbm(u * 18f, v * 18f) * 0.25f) * (1f - big);
                        o = Color.Lerp(o, new Color(0.42f, 0.24f, 0.2f), rim * 0.6f);
                    }
                    px[i] = Blood(o, blood, u, v);
                }
            return Save("T_Banshee_Body", px, Size, false);
        }

        // ------------------------------------------------------------------ Tóc
        public static Texture2D Hair(Texture src)
        {
            sx = 3.3f; sy = 51.4f;
            var px = Read(src, 1024);
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float u = (i % 1024 + 0.5f) / 1024f, v = (i / 1024 + 0.5f) / 1024f;
                float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                float g = 0.26f + lum * 1.1f + 0.14f * (Fbm(u * 6f, v * 40f, 3) - 0.5f);
                var o = new Color(g * 0.9f, g * 0.88f, g * 0.82f, c.a);
                o = Color.Lerp(o, new Color(0.22f, 0.19f, 0.15f), Sm(0.45f, 0.75f, Fbm(u * 4f, v * 4f)) * 0.6f); // bết bẩn
                o = Blood(o, Sm(0.66f, 0.76f, Fbm(u * 8f + 5f, v * 8f)) * 0.7f, u, v);
                px[i] = o;
            }
            return Save("T_Banshee_Hair", px, 1024, true);
        }
    }
}
