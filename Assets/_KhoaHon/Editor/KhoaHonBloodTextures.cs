using System.IO;
using UnityEditor;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Texture máu thủ tục kiểu ảnh chụp cho URP Decal. Mỗi hình dựng một trường "độ dày" h(u,v):
    /// lõi đặc gần đen, màng mỏng đỏ trong ở mép, viền khô sẫm, giọt bắn hình nòng nọc, sương máu li ti, vệt chảy có hạt ở đuôi.
    /// Mép luôn mềm và tắt hẳn trước biên ảnh nên decal không bao giờ lộ hình chữ nhật. Sinh kèm normal map từ độ dày.
    /// </summary>
    public static class KhoaHonBloodTextures
    {
        public const string Dir = "Assets/_KhoaHon/Textures/Gore";

        // ------------------------------------------------------------------ Khung vẽ
        class Canvas
        {
            public readonly int n;
            public readonly float[] h;
            public Canvas(int size) { n = size; h = new float[size * size]; }

            public void Fill(System.Func<float, float, float> f)
            {
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float v = f((x + 0.5f) / n, (y + 0.5f) / n);
                        int i = y * n + x;
                        if (v > h[i]) h[i] = v;
                    }
            }

            /// <summary>Giọt hình nòng nọc: đầu tròn, đuôi kéo dài về phía ngược hướng bay (ang = hướng bay).</summary>
            public void Drop(float cx, float cy, float rad, float ang, float tail, float amp)
            {
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                float ext = rad * (tail + 1.6f);
                int x0 = Mathf.Max(0, (int)((cx - ext) * n)), x1 = Mathf.Min(n - 1, (int)((cx + ext) * n) + 1);
                int y0 = Mathf.Max(0, (int)((cy - ext) * n)), y1 = Mathf.Min(n - 1, (int)((cy + ext) * n) + 1);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = (x + 0.5f) / n - cx, py = (y + 0.5f) / n - cy;
                        float along = px * ca + py * sa, across = -px * sa + py * ca;
                        float ax = along > 0f ? along : along / tail;
                        // đuôi thon dần
                        float w = along > 0f ? 1f : Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(-along / (rad * tail)));
                        float d = Mathf.Sqrt(ax * ax + (across / w) * (across / w)) / rad;
                        float val = Sm(1f, 0.55f, d) * amp;
                        int i = y * n + x;
                        if (val > h[i]) h[i] = val;
                    }
            }

            /// <summary>Đoạn thẳng có bán kính thay đổi (vệt chảy, ngón tay).</summary>
            public void Capsule(float ax, float ay, float bx, float by, float ra, float rb, float amp)
            {
                float ext = Mathf.Max(ra, rb) * 1.5f;
                int x0 = Mathf.Max(0, (int)((Mathf.Min(ax, bx) - ext) * n)), x1 = Mathf.Min(n - 1, (int)((Mathf.Max(ax, bx) + ext) * n) + 1);
                int y0 = Mathf.Max(0, (int)((Mathf.Min(ay, by) - ext) * n)), y1 = Mathf.Min(n - 1, (int)((Mathf.Max(ay, by) + ext) * n) + 1);
                float dx = bx - ax, dy = by - ay, len2 = dx * dx + dy * dy + 1e-9f;
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = (x + 0.5f) / n, py = (y + 0.5f) / n;
                        float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / len2);
                        float qx = ax + dx * t - px, qy = ay + dy * t - py;
                        float r = Mathf.Lerp(ra, rb, t);
                        float d = Mathf.Sqrt(qx * qx + qy * qy) / r;
                        float val = Sm(1f, 0.6f, d) * amp;
                        int i = y * n + x;
                        if (val > h[i]) h[i] = val;
                    }
            }

            public void Multiply(System.Func<float, float, float> f)
            {
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++) h[y * n + x] *= f((x + 0.5f) / n, (y + 0.5f) / n);
            }
        }

        // ------------------------------------------------------------------ Nhiễu
        static System.Random rnd;
        static float sx, sy;

        static void Init(int seed)
        {
            rnd = new System.Random(seed);
            sx = (float)rnd.NextDouble() * 500f; sy = (float)rnd.NextDouble() * 500f;
        }

        static float Rf(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        static float Sm(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float x, float y, int oct = 4)
        {
            float a = 0.5f, s = 0f, f = 1f, norm = 0f;
            for (int i = 0; i < oct; i++) { s += a * Mathf.PerlinNoise(x * f + sx, y * f + sy); norm += a; a *= 0.5f; f *= 2.03f; }
            return s / norm;
        }

        static float Edge(float u, float v)
        {
            // Tắt hẳn trước mép ảnh 3% — không lộ khung decal
            float m = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            return Sm(0.005f, 0.04f, m);
        }

        // ------------------------------------------------------------------ Hình
        /// <summary>Vết bắn do va đập: thân loang có tia, giọt nòng nọc văng ra theo tia, sương li ti.</summary>
        static Canvas Splat(int size, int seed, float spread)
        {
            Init(seed);
            var c = new Canvas(size);
            int spikes = 26;
            var sa = new float[spikes]; var sl = new float[spikes]; var sw = new float[spikes];
            for (int i = 0; i < spikes; i++) { sa[i] = Rf(-180f, 180f); sl[i] = Rf(0.03f, 0.2f) * spread; sw[i] = Rf(3f, 11f); }
            float baseR = Rf(0.12f, 0.16f);
            c.Fill((u, v) =>
            {
                float x = u - 0.5f + 0.05f * (Fbm(u * 5f, v * 5f) - 0.5f) * 2f;
                float y = v - 0.5f + 0.05f * (Fbm(u * 5f + 9.1f, v * 5f + 3.7f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(x * x + y * y), th = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                float R = baseR * (0.75f + 0.55f * Fbm(Mathf.Cos(th * Mathf.Deg2Rad) * 1.6f + 3f, Mathf.Sin(th * Mathf.Deg2Rad) * 1.6f + 3f));
                for (int i = 0; i < spikes; i++)
                {
                    float d = Mathf.DeltaAngle(th, sa[i]) / sw[i];
                    R += sl[i] * Mathf.Exp(-d * d) * Mathf.Clamp01(1.2f - r * 1.5f);
                }
                float body = Sm(R, R * 0.5f, r);
                return body * (0.7f + 0.45f * Fbm(u * 10f, v * 10f, 3));
            });
            // Giọt văng dọc theo tia (xa tâm thì nhỏ dần), đuôi chỉ về tâm
            for (int i = 0; i < 110; i++)
            {
                float a = rnd.NextDouble() < 0.75 ? sa[rnd.Next(spikes)] + Rf(-6f, 6f) : Rf(-180f, 180f);
                float ar = a * Mathf.Deg2Rad;
                float dist = Rf(baseR * 1.1f, 0.44f);
                float rad = Mathf.Lerp(0.022f, 0.004f, (dist - baseR) / (0.44f - baseR)) * Rf(0.5f, 1.2f);
                c.Drop(0.5f + Mathf.Cos(ar) * dist, 0.5f + Mathf.Sin(ar) * dist, rad, ar, Rf(1.5f, 4.5f), Rf(0.6f, 0.95f));
            }
            for (int i = 0; i < 500; i++)
            {
                float ar = Rf(0f, Mathf.PI * 2f), dist = Mathf.Sqrt(Rf(0.02f, 1f)) * 0.45f;
                c.Drop(0.5f + Mathf.Cos(ar) * dist, 0.5f + Mathf.Sin(ar) * dist, Rf(0.0015f, 0.0045f), ar, 1.6f, Rf(0.35f, 0.7f));
            }
            return c;
        }

        /// <summary>Vũng máu: các khối cầu mềm hòa vào nhau, mép gợn, lõi dày.</summary>
        static Canvas Pool(int size, int seed)
        {
            Init(seed);
            var c = new Canvas(size);
            int k = 12;
            var bx = new float[k]; var by = new float[k]; var br = new float[k];
            for (int i = 0; i < k; i++) { float a = Rf(0f, 6.283f), d = i == 0 ? 0f : Rf(0.08f, 0.28f); bx[i] = 0.5f + Mathf.Cos(a) * d; by[i] = 0.5f + Mathf.Sin(a) * d; br[i] = i == 0 ? 0.16f : Rf(0.05f, 0.13f); }
            c.Fill((u, v) =>
            {
                float wu = u + 0.06f * (Fbm(u * 5f, v * 5f) - 0.5f) * 2f, wv = v + 0.06f * (Fbm(u * 5f + 5f, v * 5f + 2f) - 0.5f) * 2f;
                float F = 0f;
                for (int i = 0; i < k; i++) { float dx = wu - bx[i], dy = wv - by[i]; F += Mathf.Exp(-(dx * dx + dy * dy) / (br[i] * br[i])); }
                float h = Sm(0.32f, 0.5f, F);
                return h * (0.7f + 0.4f * Fbm(u * 9f, v * 9f, 3));
            });
            for (int i = 0; i < 40; i++)
            {
                float ar = Rf(0f, 6.283f), dist = Rf(0.3f, 0.45f);
                c.Drop(0.5f + Mathf.Cos(ar) * dist, 0.5f + Mathf.Sin(ar) * dist, Rf(0.004f, 0.014f), ar, Rf(1.2f, 2.5f), Rf(0.6f, 0.9f));
            }
            return c;
        }

        /// <summary>Vệt kéo lê (thân người bị kéo): đầu đậm thành vũng, thân có sọc dọc, mép rách, đuôi đứt quãng.</summary>
        static Canvas Drag(int size, int seed)
        {
            Init(seed);
            var c = new Canvas(size);
            float ph = Rf(0f, 6f);
            c.Fill((u, v) =>
            {
                float t = Mathf.InverseLerp(0.06f, 0.94f, v);
                if (t <= 0f || t >= 1f) return 0f;
                float center = 0.5f + 0.06f * Mathf.Sin(v * 5f + ph) + 0.04f * (Fbm(v * 3f, 1.3f) - 0.5f) * 2f;
                float width = Mathf.Lerp(0.3f, 0.11f, t) * (0.75f + 0.5f * Fbm(v * 6f, 4.2f, 3));
                float edgeN = 0.05f * (Fbm(u * 18f, v * 18f, 3) - 0.5f) * 2f;
                float du = Mathf.Abs(u - center) + edgeN;
                float shape = Sm(width, width * 0.6f, du) * Sm(0f, 0.06f, t) * Sm(1f, 0.85f, t);
                // Sọc dọc do vải/da kéo lê: nhiễu dẹt theo hướng kéo
                float streak = Fbm((u - center) * 45f, v * 2.2f, 4);
                float breaks = Sm(0.25f + 0.35f * t, 0.6f + 0.2f * t, streak);
                float head = Sm(0.25f, 0f, t); // đầu vệt đặc
                return shape * Mathf.Lerp(breaks * 0.9f, 1f, head) * (0.75f + 0.3f * Fbm(u * 12f, v * 12f, 2));
            });
            // Vũng nhỏ ở đầu vệt + vài giọt rơi dọc đường
            for (int i = 0; i < 6; i++) c.Drop(0.5f + Rf(-0.1f, 0.1f), Rf(0.08f, 0.16f), Rf(0.04f, 0.07f), Rf(0f, 6.28f), 1.2f, 0.95f);
            for (int i = 0; i < 50; i++) c.Drop(0.5f + Rf(-0.22f, 0.22f), Rf(0.1f, 0.95f), Rf(0.003f, 0.01f), Mathf.PI * 0.5f, Rf(1.2f, 2.5f), Rf(0.5f, 0.85f));
            return c;
        }

        /// <summary>Dấu bàn tay: lòng bàn tay + năm ngón có nếp đốt, vân in loang lổ; smear = tay trượt xuống để lại vệt.</summary>
        static Canvas Hand(int size, int seed, bool child, bool smear)
        {
            Init(seed);
            var c = new Canvas(size);
            float s = child ? 0.78f : 1f;
            void Print(float oy, float amp)
            {
                float px = 0.5f, py = 0.42f + oy;
                c.Fill((u, v) =>
                {
                    float dx = (u - px) / (0.105f * s), dy = (v - py) / (0.125f * s);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) + 0.15f * (Fbm(u * 12f, v * 12f) - 0.5f);
                    return Sm(1f, 0.75f, d) * amp;
                });
                float[] ang = { 152f, 110f, 93f, 76f, 58f };
                float[] len = { 0.13f, 0.2f, 0.22f, 0.2f, 0.15f };
                float[] rad = { 0.021f, 0.018f, 0.019f, 0.018f, 0.015f };
                float[] baseOff = { 0.1f, 0.13f, 0.135f, 0.13f, 0.115f };
                for (int f = 0; f < 5; f++)
                {
                    float a = (ang[f] + Rf(-4f, 4f)) * Mathf.Deg2Rad;
                    float bx = px + Mathf.Cos(a) * baseOff[f] * s, by = py + Mathf.Sin(a) * baseOff[f] * s * 0.9f;
                    float L = len[f] * s, r = rad[f] * s;
                    // 3 đốt, khe nếp giữa các đốt
                    for (int seg = 0; seg < 3; seg++)
                    {
                        float t0 = seg / 3f + 0.03f, t1 = (seg + 1) / 3f - 0.03f;
                        c.Capsule(bx + Mathf.Cos(a) * L * t0, by + Mathf.Sin(a) * L * t0, bx + Mathf.Cos(a) * L * t1, by + Mathf.Sin(a) * L * t1, r, r * (seg == 2 ? 0.85f : 0.95f), amp * (seg == 2 ? 1f : 0.85f));
                    }
                }
            }
            if (smear)
            {
                // Tay áp vào rồi trượt xuống: nhiều bản in mờ dần
                for (int i = 6; i >= 1; i--) Print(-i * 0.03f, 0.22f + 0.06f * (6 - i));
                Print(0f, 1f);
            }
            else Print(0f, 1f);
            // Vân tay / da: in loang lổ
            c.Multiply((u, v) => 0.4f + 0.6f * Sm(0.32f, 0.62f, Fbm(u * 26f, v * 26f, 4)));
            // Máu đọng chảy xuống từ mép dưới lòng bàn tay
            int drips = smear ? 4 : rnd.Next(1, 4);
            for (int i = 0; i < drips; i++)
            {
                float x = 0.5f + Rf(-0.09f, 0.09f) * s, top = 0.32f, end = Rf(0.06f, 0.22f), w = Rf(0.006f, 0.011f);
                c.Capsule(x, top, x + Rf(-0.01f, 0.01f), end, w, w * 0.8f, 0.8f);
                c.Drop(x, end, w * 1.7f, -Mathf.PI * 0.5f, 1.3f, 0.9f);
            }
            return c;
        }

        /// <summary>Máu chảy trên tường: mảng bắn phía trên, nhiều dòng chảy dài ngắn có lắc nhẹ, đọng hạt ở đuôi.</summary>
        static Canvas Drips(int size, int seed)
        {
            Init(seed);
            var c = new Canvas(size);
            c.Fill((u, v) =>
            {
                float y = (v - 0.84f) / 0.09f, x = (u - 0.5f) / 0.42f;
                float d = Mathf.Sqrt(x * x + y * y) + 0.7f * (Fbm(u * 6f, v * 6f) - 0.5f) * 2f + 0.25f * (Fbm(u * 22f, v * 22f) - 0.5f) * 2f;
                return Sm(1f, 0.55f, d) * (0.65f + 0.45f * Fbm(u * 12f, v * 12f, 3));
            });
            int runs = 22;
            for (int i = 0; i < runs; i++)
            {
                float x = Rf(0.12f, 0.88f);
                float top = 0.82f - Mathf.Abs(x - 0.5f) * 0.12f;
                float end = Mathf.Clamp(top - Rf(0.1f, 0.75f) * (1f - Mathf.Abs(x - 0.5f)), 0.05f, top - 0.05f);
                float w = Rf(0.004f, 0.012f);
                float y = top, cx = x;
                while (y > end)
                {
                    float ny = Mathf.Max(end, y - 0.02f), nx = cx + Rf(-0.0025f, 0.0025f);
                    float k = Mathf.InverseLerp(top, end, ny);
                    c.Capsule(cx, y, nx, ny, w * Mathf.Lerp(1f, 0.7f, k), w * Mathf.Lerp(1f, 0.7f, k), 0.85f);
                    cx = nx; y = ny;
                }
                c.Drop(cx, end, w * 1.6f, -Mathf.PI * 0.5f, 1.4f, 0.95f);
            }
            for (int i = 0; i < 250; i++) c.Drop(Rf(0.08f, 0.92f), Rf(0.62f, 0.97f), Rf(0.0015f, 0.005f), Rf(0f, 6.28f), 1.5f, Rf(0.4f, 0.8f));
            return c;
        }

        /// <summary>Vết cháy sém: muội đen loang, mép vàng nâu.</summary>
        static Canvas Scorch(int size, int seed)
        {
            Init(seed);
            var c = new Canvas(size);
            c.Fill((u, v) =>
            {
                float x = u - 0.5f, y = v - 0.5f;
                float d = Mathf.Sqrt(x * x + y * y) / 0.42f + 0.4f * (Fbm(u * 5f, v * 5f) - 0.5f);
                return Sm(1f, 0.2f, d) * (0.6f + 0.5f * Fbm(u * 16f, v * 16f));
            });
            return c;
        }

        // ------------------------------------------------------------------ Xuất
        public struct Pair { public Texture2D baseMap, normalMap; }

        public enum Kind { Splat, SplatWide, Pool, Drag, Hand, HandSmear, HandChild, Drips, Scorch }

        public static Pair Build(Kind kind, int seed)
        {
            Canvas c;
            switch (kind)
            {
                case Kind.Splat: c = Splat(1024, seed, 1f); break;
                case Kind.SplatWide: c = Splat(1024, seed, 1.6f); break;
                case Kind.Pool: c = Pool(1024, seed); break;
                case Kind.Drag: c = Drag(1024, seed); break;
                case Kind.Hand: c = Hand(512, seed, false, false); break;
                case Kind.HandSmear: c = Hand(512, seed, false, true); break;
                case Kind.HandChild: c = Hand(512, seed, true, false); break;
                case Kind.Drips: c = Drips(1024, seed); break;
                default: c = Scorch(512, seed); break;
            }
            string name = "TD_" + kind + "_" + seed;
            return Write(name, c, kind == Kind.Scorch);
        }

        static Pair Write(string name, Canvas c, bool soot)
        {
            int n = c.n;
            // Làm mượt 1 lần cho normal
            var hb = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float s = 0f; int k = 0;
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int xx = Mathf.Clamp(x + ox, 0, n - 1), yy = Mathf.Clamp(y + oy, 0, n - 1);
                            s += c.h[yy * n + xx]; k++;
                        }
                    hb[y * n + x] = s / k;
                }
            var col = new Color32[n * n];
            var nrm = new Color32[n * n];
            float strength = n / 256f * 1.6f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = y * n + x;
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                    float h = Mathf.Clamp01(c.h[i]) * Edge(u, v);
                    Color cc; float a;
                    if (soot)
                    {
                        a = Sm(0.02f, 0.5f, h);
                        cc = Color.Lerp(new Color(0.16f, 0.1f, 0.06f), new Color(0.02f, 0.018f, 0.016f), Sm(0.1f, 0.6f, h));
                    }
                    else
                    {
                        float thick = Sm(0.18f, 0.9f, h);
                        float rim = Sm(0.03f, 0.1f, h) * (1f - Sm(0.1f, 0.26f, h));
                        float nz = Fbm(u * 50f, v * 50f, 3);
                        cc = Color.Lerp(new Color(0.62f, 0.05f, 0.035f), new Color(0.3f, 0.012f, 0.01f), thick);
                        cc = Color.Lerp(cc, new Color(0.24f, 0.045f, 0.03f), rim * 0.6f);
                        float k = 0.82f + 0.32f * nz;
                        cc = new Color(cc.r * k, cc.g * k, cc.b * k);
                        a = Sm(0.025f, 0.14f, h) * Mathf.Lerp(0.72f, 1f, thick);
                    }
                    col[i] = new Color(cc.r, cc.g, cc.b, a);
                    int xl = Mathf.Max(0, x - 1), xr = Mathf.Min(n - 1, x + 1), yd = Mathf.Max(0, y - 1), yu = Mathf.Min(n - 1, y + 1);
                    float dx = (hb[y * n + xr] - hb[y * n + xl]) * strength, dy = (hb[yu * n + x] - hb[yd * n + x]) * strength;
                    var nv = new Vector3(-dx, -dy, 1f).normalized;
                    nrm[i] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);
                }
            return new Pair { baseMap = Save(name, n, col, false), normalMap = Save(name + "_N", n, nrm, true) };
        }

        static Texture2D Save(string name, int n, Color32[] px, bool normal)
        {
            KhoaHonDataBuilder.EnsureFolder(Dir);
            string path = Dir + "/" + name + ".png";
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, normal);
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !normal;
            ti.alphaIsTransparency = !normal;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
