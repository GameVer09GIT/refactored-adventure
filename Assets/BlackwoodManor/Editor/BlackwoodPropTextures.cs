using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Chất liệu PBR cho đạo cụ dựng bằng Blender (trước đây chỉ là màu phẳng):
    ///  • mỗi "loại bề mặt" (sắt, đồng, gỗ, vải, xương…) có một bộ normal + mask (kim loại / AO / độ nhẵn) lặp liền mạch;
    ///  • mỗi vật liệu có albedo riêng: màu gốc + vết bẩn/gỉ/ố của loại đó.
    /// UV của model do Blender chiếu hộp theo kích thước thật (props_common.box_uv) nên tiling vật liệu để (1,1).
    /// Gọi từ converter sau khi nhập FBX; chạy lại nhiều lần vẫn ra cùng kết quả (màu gốc lưu trong userData của .mat).
    /// </summary>
    public static class BlackwoodPropTextures
    {
        public const string TexDir = "Assets/BlackwoodManor/Textures/Props";
        const string MatDir = "Assets/BlackwoodManor/Models/Props/Materials";
        const int S = 512;

        // ------------------------------------------------------------------ nhiễu lặp liền mạch
        static int Mod(int a, int n) { int r = a % n; return r < 0 ? r + n : r; }
        static float Sm(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        static float H(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + s * 974711);
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        static float VN(float x, float y, int px, int py, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi; fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            int x0 = Mod(xi, px), x1 = Mod(xi + 1, px), y0 = Mod(yi, py), y1 = Mod(yi + 1, py);
            return Mathf.Lerp(Mathf.Lerp(H(x0, y0, s), H(x1, y0, s), fx), Mathf.Lerp(H(x0, y1, s), H(x1, y1, s), fx), fy);
        }

        /// <summary>fBm lặp theo chu kỳ (fx, fy) ô trên một tile; fx ≠ fy cho vân kéo dài.</summary>
        static float Fbm(float u, float v, int fx, int fy, int oct, int s, float gain = 0.5f)
        {
            float a = 0.5f, sum = 0f, norm = 0f;
            for (int o = 0; o < oct; o++)
            {
                sum += a * VN(u * fx, v * fy, fx, fy, s + o * 17); norm += a;
                fx *= 2; fy *= 2; a *= gain;
            }
            return sum / norm;
        }

        /// <summary>Worley lặp: f1 = khoảng cách tới điểm gần nhất, f2 = gần nhì (đơn vị ô).</summary>
        static void Worley(float u, float v, int n, int s, out float f1, out float f2)
        {
            float x = u * n, y = v * n; int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            f1 = 9f; f2 = 9f;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = xi + dx, cy = yi + dy, mx = Mod(cx, n), my = Mod(cy, n);
                    float px = cx + H(mx, my, s), py = cy + H(mx, my, s + 7);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                }
        }

        /// <summary>Vết xước mảnh ngẫu nhiên (có quấn mép), giá trị 0..1.</summary>
        static float[] Scratches(int count, float minLen, float maxLen, float angleSpread, int seed)
        {
            var b = new float[S * S];
            var rnd = new System.Random(seed);
            for (int k = 0; k < count; k++)
            {
                float x = (float)rnd.NextDouble() * S, y = (float)rnd.NextDouble() * S;
                float ang = ((float)rnd.NextDouble() - 0.5f) * angleSpread + (rnd.Next(2) == 0 ? 0f : Mathf.PI * 0.5f * (angleSpread > 3f ? 1f : 0f));
                float len = Mathf.Lerp(minLen, maxLen, (float)rnd.NextDouble()) * S;
                float str = 0.35f + 0.65f * (float)rnd.NextDouble();
                float dx = Mathf.Cos(ang), dy = Mathf.Sin(ang), bend = ((float)rnd.NextDouble() - 0.5f) * 0.004f;
                for (float t = 0; t < len; t += 0.7f)
                {
                    float fade = Mathf.Sin(Mathf.PI * t / len);
                    int ix = Mod(Mathf.RoundToInt(x), S), iy = Mod(Mathf.RoundToInt(y), S);
                    b[iy * S + ix] = Mathf.Max(b[iy * S + ix], str * fade);
                    x += dx * 0.7f; y += dy * 0.7f;
                    float c = Mathf.Cos(bend), sn = Mathf.Sin(bend); float ndx = dx * c - dy * sn; dy = dx * sn + dy * c; dx = ndx;
                }
            }
            return b;
        }

        // ------------------------------------------------------------------ loại bề mặt
        class Surf
        {
            public float[] h = new float[S * S], tone = new float[S * S], stain = new float[S * S];
            public float[] metal = new float[S * S], smooth = new float[S * S], ao = new float[S * S];
        }

        delegate void Px(Surf s, int i, float u, float v);

        static Surf Make(Px f)
        {
            var s = new Surf();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int i = y * S + x;
                    s.ao[i] = 1f; s.tone[i] = 1f;
                    f(s, i, (x + 0.5f) / S, (y + 0.5f) / S);
                }
            return s;
        }

        static readonly Dictionary<string, Surf> cache = new Dictionary<string, Surf>();

        static Surf Class(string cls)
        {
            if (cache.TryGetValue(cls, out var c)) return c;
            Surf s;
            switch (cls)
            {
                case "iron":
                    s = Make((o, i, u, v) =>
                    {
                        float n = Fbm(u, v, 6, 6, 5, 11), fine = Fbm(u, v, 48, 48, 3, 12);
                        Worley(u, v, 44, 13, out float f1, out _);
                        float pit = (1f - Sm(0.05f, 0.3f, f1)) * Sm(0.45f, 0.6f, Fbm(u, v, 10, 10, 3, 14));
                        float rust = Sm(0.6f, 0.8f, Fbm(u, v, 6, 6, 5, 15) + 0.18f * (fine - 0.5f) + 0.25f * pit);
                        o.h[i] = 0.5f + 0.3f * (n - 0.5f) + 0.12f * (fine - 0.5f) - 0.4f * pit + 0.1f * rust * fine;
                        o.tone[i] = 0.82f + 0.36f * n - 0.3f * pit;
                        o.stain[i] = rust;
                        o.metal[i] = 1f - 0.85f * rust;
                        o.smooth[i] = Mathf.Clamp01(0.46f + 0.18f * (n - 0.5f) - 0.2f * rust - 0.2f * pit);
                        o.ao[i] = 1f - 0.55f * pit;
                    });
                    break;
                case "rust":
                    s = Make((o, i, u, v) =>
                    {
                        float n = Fbm(u, v, 5, 5, 6, 21, 0.6f), fine = Fbm(u, v, 64, 64, 3, 22);
                        Worley(u, v, 26, 23, out float f1, out float f2);
                        float flake = Sm(0.02f, 0.14f, f2 - f1);
                        o.h[i] = 0.5f + 0.45f * (n - 0.5f) + 0.25f * (fine - 0.5f) - 0.3f * (1f - flake);
                        o.tone[i] = 0.6f + 0.7f * n - 0.25f * (1f - flake);
                        o.stain[i] = Sm(0.4f, 0.75f, Fbm(u, v, 9, 9, 4, 24) + 0.3f * (fine - 0.5f));
                        o.metal[i] = 0.25f * Sm(0.6f, 0.8f, n);
                        o.smooth[i] = 0.08f + 0.14f * n;
                        o.ao[i] = 0.6f + 0.4f * flake;
                    });
                    break;
                case "brass":
                    {
                        var sc = Scratches(260, 0.04f, 0.3f, 6.3f, 31);
                        s = Make((o, i, u, v) =>
                        {
                            float n = Fbm(u, v, 5, 5, 5, 32), fine = Fbm(u, v, 40, 40, 3, 33);
                            Worley(u, v, 30, 34, out float f1, out _);
                            float pit = (1f - Sm(0.04f, 0.22f, f1)) * Sm(0.5f, 0.7f, Fbm(u, v, 8, 8, 3, 35));
                            float pat = Sm(0.57f, 0.8f, Fbm(u, v, 7, 7, 5, 36) + 0.2f * (fine - 0.5f) + 0.3f * pit);
                            o.h[i] = 0.5f + 0.16f * (n - 0.5f) + 0.06f * (fine - 0.5f) - 0.3f * pit - 0.12f * sc[i] + 0.08f * pat * fine;
                            o.tone[i] = 0.86f + 0.3f * n - 0.2f * pit - 0.1f * sc[i];
                            o.stain[i] = pat;
                            o.metal[i] = 1f - 0.5f * pat;
                            o.smooth[i] = Mathf.Clamp01(0.66f + 0.14f * (n - 0.5f) - 0.22f * pat - 0.2f * sc[i] - 0.15f * pit);
                            o.ao[i] = 1f - 0.4f * pit;
                        });
                        break;
                    }
                case "chrome":
                    {
                        var sc = Scratches(420, 0.03f, 0.25f, 6.3f, 41);
                        s = Make((o, i, u, v) =>
                        {
                            float smudge = Sm(0.4f, 0.9f, Fbm(u, v, 9, 9, 4, 42)), fine = Fbm(u, v, 64, 64, 2, 43);
                            o.h[i] = 0.5f - 0.3f * sc[i] + 0.03f * (fine - 0.5f);
                            o.tone[i] = 0.95f + 0.1f * (fine - 0.5f) - 0.12f * sc[i];
                            o.stain[i] = smudge * 0.7f;
                            o.metal[i] = 1f - 0.15f * smudge;
                            o.smooth[i] = Mathf.Clamp01(0.9f - 0.14f * smudge - 0.3f * sc[i]);
                        });
                        break;
                    }
                case "wood":
                    s = Make((o, i, u, v) =>
                    {
                        float warp = Fbm(u, v, 3, 4, 3, 51) * 0.06f;
                        float g = Fbm(u, v + warp, 3, 44, 4, 52), g2 = Fbm(u, v + warp, 6, 120, 2, 53);
                        float ring = 1f - Mathf.Abs(2f * g - 1f);
                        float pore = Sm(0.62f, 0.8f, Fbm(u, v, 28, 300, 2, 54));
                        float dirt = Sm(0.5f, 0.8f, Fbm(u, v, 4, 4, 4, 55));
                        Worley(u, v, 5, 56, out float f1, out float f2);
                        float crack = (1f - Sm(0.0f, 0.02f, f2 - f1)) * Sm(0.55f, 0.7f, Fbm(u, v, 3, 3, 2, 57));
                        o.h[i] = 0.5f + 0.26f * (ring - 0.5f) + 0.12f * (g2 - 0.5f) - 0.3f * pore - 0.5f * crack;
                        o.tone[i] = 0.7f + 0.45f * ring + 0.2f * (g2 - 0.5f) - 0.35f * pore - 0.5f * crack;
                        o.stain[i] = dirt;
                        o.smooth[i] = Mathf.Clamp01(0.36f + 0.2f * (ring - 0.5f) - 0.2f * pore - 0.18f * dirt);
                        o.ao[i] = 1f - 0.4f * pore - 0.6f * crack;
                    });
                    break;
                case "lacquer":
                    s = Make((o, i, u, v) =>
                    {
                        Worley(u, v, 16, 61, out float f1, out float f2);
                        float crack = 1f - Sm(0.0f, 0.035f, f2 - f1);
                        float dust = Sm(0.45f, 0.8f, Fbm(u, v, 5, 5, 5, 62)), n = Fbm(u, v, 8, 8, 4, 63);
                        float chip = Sm(0.72f, 0.8f, Fbm(u, v, 7, 7, 4, 64));
                        o.h[i] = 0.55f + 0.06f * (n - 0.5f) - 0.45f * crack - 0.3f * chip;
                        o.tone[i] = 0.9f + 0.2f * (n - 0.5f) - 0.5f * crack;
                        o.stain[i] = Mathf.Max(dust * 0.7f, chip);
                        o.smooth[i] = Mathf.Clamp01(0.82f - 0.45f * dust - 0.5f * crack - 0.6f * chip);
                        o.ao[i] = 1f - 0.5f * crack;
                    });
                    break;
                case "cloth":
                    s = Make((o, i, u, v) =>
                    {
                        const int T = 64;
                        float wob = (Fbm(u, v, 8, 8, 2, 71) - 0.5f) * 0.006f;
                        float tu = (u + wob) * T, tv = (v - wob) * T;
                        float hw = Mathf.Abs(Mathf.Sin(Mathf.PI * tu)), hf = Mathf.Abs(Mathf.Sin(Mathf.PI * tv));
                        bool over = ((Mathf.FloorToInt(tu) + Mathf.FloorToInt(tv)) & 1) == 0;
                        float weave = over ? 0.35f + 0.65f * hf * (0.5f + 0.5f * hw) : 0.35f + 0.65f * hw * (0.5f + 0.5f * hf);
                        float fuzz = Fbm(u, v, 96, 96, 2, 72), wr = Fbm(u, v, 4, 4, 4, 73);
                        float thread = 0.85f + 0.3f * H(Mathf.FloorToInt(tu), over ? 0 : Mathf.FloorToInt(tv), 74);
                        o.h[i] = 0.25f + 0.5f * weave + 0.12f * (fuzz - 0.5f) + 0.2f * (wr - 0.5f);
                        o.tone[i] = (0.68f + 0.38f * weave + 0.14f * (fuzz - 0.5f)) * Mathf.Lerp(1f, thread, 0.35f);
                        o.stain[i] = Sm(0.48f, 0.78f, Fbm(u, v, 3, 3, 5, 75));
                        o.smooth[i] = 0.06f + 0.1f * weave;
                        o.ao[i] = 0.55f + 0.45f * weave;
                    });
                    break;
                case "leather":
                    s = Make((o, i, u, v) =>
                    {
                        Worley(u, v, 34, 81, out float f1, out float f2);
                        float crease = 1f - Sm(0.0f, 0.16f, f2 - f1);
                        float n = Fbm(u, v, 6, 6, 5, 82), wear = Sm(0.55f, 0.8f, Fbm(u, v, 4, 4, 4, 83));
                        o.h[i] = 0.6f - 0.45f * crease + 0.2f * (n - 0.5f);
                        o.tone[i] = 0.92f - 0.35f * crease + 0.25f * (n - 0.5f);
                        o.stain[i] = wear;
                        o.smooth[i] = Mathf.Clamp01(0.42f - 0.25f * crease + 0.12f * (n - 0.5f) - 0.2f * wear);
                        o.ao[i] = 1f - 0.45f * crease;
                    });
                    break;
                case "paper":
                    {
                        var fib = Scratches(900, 0.01f, 0.05f, 6.3f, 91);
                        s = Make((o, i, u, v) =>
                        {
                            float n = Fbm(u, v, 48, 48, 3, 92), big = Fbm(u, v, 4, 4, 4, 93);
                            Worley(u, v, 14, 94, out float f1, out _);
                            float spot = (1f - Sm(0.05f, 0.3f, f1)) * Sm(0.5f, 0.65f, Fbm(u, v, 6, 6, 3, 95));
                            o.h[i] = 0.5f + 0.2f * (n - 0.5f) + 0.2f * fib[i] + 0.15f * (big - 0.5f);
                            o.tone[i] = 0.9f + 0.14f * (n - 0.5f) + 0.12f * (big - 0.5f) - 0.08f * fib[i];
                            o.stain[i] = Mathf.Max(spot, Sm(0.55f, 0.85f, big));
                            o.smooth[i] = 0.1f + 0.06f * n;
                        });
                        break;
                    }
                case "plastic":
                    {
                        var sc = Scratches(160, 0.03f, 0.2f, 6.3f, 101);
                        s = Make((o, i, u, v) =>
                        {
                            Worley(u, v, 110, 102, out float f1, out _);
                            float grain = Sm(0.1f, 0.6f, f1), smudge = Sm(0.45f, 0.75f, Fbm(u, v, 5, 5, 4, 103));
                            float dust = Sm(0.55f, 0.85f, Fbm(u, v, 7, 7, 5, 104));
                            o.h[i] = 0.5f + 0.22f * (grain - 0.5f) - 0.3f * sc[i];
                            o.tone[i] = 0.94f + 0.08f * (grain - 0.5f) + 0.25f * sc[i];
                            o.stain[i] = dust;
                            o.smooth[i] = Mathf.Clamp01(0.52f + 0.2f * smudge - 0.28f * sc[i] - 0.3f * dust - 0.08f * grain);
                            o.ao[i] = 0.85f + 0.15f * grain;
                        });
                        break;
                    }
                case "paint":
                    {
                        var sc = Scratches(200, 0.04f, 0.35f, 6.3f, 111);
                        s = Make((o, i, u, v) =>
                        {
                            float n = Fbm(u, v, 5, 5, 5, 112), peel = Fbm(u, v, 9, 9, 5, 113, 0.6f);
                            float chip = Sm(0.71f, 0.75f, peel), dust = Sm(0.45f, 0.8f, Fbm(u, v, 4, 4, 4, 114));
                            float orange = Fbm(u, v, 70, 70, 2, 115);
                            o.h[i] = 0.6f + 0.05f * (orange - 0.5f) - 0.35f * chip - 0.2f * sc[i] * (1f - chip);
                            o.tone[i] = 0.92f + 0.14f * (n - 0.5f) + 0.2f * sc[i] * (1f - chip);
                            o.stain[i] = Mathf.Max(chip, 0.45f * dust);
                            o.metal[i] = 0.35f * chip;
                            o.smooth[i] = Mathf.Clamp01(0.7f - 0.5f * chip - 0.35f * dust - 0.25f * sc[i]);
                            o.ao[i] = 1f - 0.3f * Sm(0.65f, 0.73f, peel) * (1f - chip);
                        });
                        break;
                    }
                case "wax":
                    s = Make((o, i, u, v) =>
                    {
                        float drip = Fbm(u, v, 9, 2, 4, 121), n = Fbm(u, v, 10, 10, 4, 122);
                        float run = Sm(0.55f, 0.75f, drip);
                        o.h[i] = 0.45f + 0.35f * run + 0.1f * (n - 0.5f);
                        o.tone[i] = 0.9f + 0.14f * (n - 0.5f) + 0.06f * run;
                        o.stain[i] = Sm(0.5f, 0.85f, Fbm(u, v, 4, 5, 4, 123)) * (1f - 0.5f * run);
                        o.smooth[i] = Mathf.Clamp01(0.5f + 0.2f * run - 0.15f * n);
                        o.ao[i] = 0.8f + 0.2f * run;
                    });
                    break;
                case "stone":
                    s = Make((o, i, u, v) =>
                    {
                        float n = Fbm(u, v, 4, 4, 7, 131, 0.58f), fine = Fbm(u, v, 80, 80, 2, 132);
                        Worley(u, v, 22, 133, out float f1, out float f2);
                        float pit = (1f - Sm(0.05f, 0.3f, f1)) * Sm(0.4f, 0.6f, Fbm(u, v, 7, 7, 3, 134));
                        Worley(u, v, 4, 135, out float g1, out float g2);
                        float crack = (1f - Sm(0f, 0.018f, g2 - g1)) * Sm(0.45f, 0.6f, Fbm(u, v, 3, 3, 2, 136));
                        o.h[i] = 0.5f + 0.4f * (n - 0.5f) + 0.1f * (fine - 0.5f) - 0.35f * pit - 0.5f * crack;
                        o.tone[i] = 0.78f + 0.4f * n + 0.12f * (fine - 0.5f) - 0.3f * pit - 0.4f * crack;
                        o.stain[i] = Sm(0.5f, 0.8f, Fbm(u, v, 3, 3, 5, 137) + 0.2f * pit);
                        o.smooth[i] = 0.1f + 0.14f * n;
                        o.ao[i] = 1f - 0.5f * pit - 0.6f * crack;
                    });
                    break;
                case "bone":
                    s = Make((o, i, u, v) =>
                    {
                        float fib = Fbm(u, v, 4, 60, 3, 141), n = Fbm(u, v, 5, 5, 5, 142);
                        Worley(u, v, 70, 143, out float f1, out _);
                        float pore = (1f - Sm(0.08f, 0.3f, f1)) * Sm(0.4f, 0.65f, Fbm(u, v, 6, 6, 3, 144));
                        Worley(u, v, 6, 145, out float g1, out float g2);
                        float crack = (1f - Sm(0f, 0.022f, g2 - g1)) * Sm(0.5f, 0.65f, Fbm(u, v, 3, 3, 2, 146));
                        o.h[i] = 0.55f + 0.12f * (fib - 0.5f) + 0.15f * (n - 0.5f) - 0.4f * pore - 0.5f * crack;
                        o.tone[i] = 0.84f + 0.24f * n + 0.1f * (fib - 0.5f) - 0.35f * pore - 0.5f * crack;
                        o.stain[i] = Sm(0.42f, 0.78f, Fbm(u, v, 4, 4, 5, 147) + 0.3f * pore + 0.4f * crack);
                        o.smooth[i] = Mathf.Clamp01(0.3f + 0.14f * (n - 0.5f) - 0.2f * pore);
                        o.ao[i] = 1f - 0.5f * pore - 0.6f * crack;
                    });
                    break;
                case "skin":
                    s = Make((o, i, u, v) =>
                    {
                        float n = Fbm(u, v, 4, 4, 5, 151), fine = Fbm(u, v, 60, 60, 2, 152);
                        Worley(u, v, 90, 153, out float f1, out float f2);
                        float crease = 1f - Sm(0f, 0.2f, f2 - f1);
                        o.h[i] = 0.55f - 0.3f * crease + 0.1f * (fine - 0.5f) + 0.1f * (n - 0.5f);
                        o.tone[i] = 0.86f + 0.28f * n - 0.12f * crease;
                        o.stain[i] = Sm(0.45f, 0.75f, Fbm(u, v, 3, 3, 5, 154));
                        o.smooth[i] = Mathf.Clamp01(0.28f + 0.12f * (n - 0.5f) - 0.1f * crease);
                        o.ao[i] = 1f - 0.3f * crease;
                    });
                    break;
                case "knurl":
                    {
                        // Thân đèn pin (UV hình trụ của Unity: U quanh thân, V dọc thân): đoạn giữa khía nhám kim cương, hai đầu trơn có rãnh vòng.
                        var sc = Scratches(140, 0.03f, 0.22f, 6.3f, 171);
                        s = Make((o, i, u, v) =>
                        {
                            float band = Sm(0.16f, 0.2f, v) * (1f - Sm(0.6f, 0.64f, v));
                            float a = u * 40f + v * 54f, b2 = u * 40f - v * 54f;
                            float da = Mathf.Abs(a - Mathf.Floor(a) - 0.5f), db = Mathf.Abs(b2 - Mathf.Floor(b2) - 0.5f);
                            float pyr = 1f - 2f * Mathf.Max(da, db);
                            float groove = 0f;
                            foreach (float gv in new[] { 0.07f, 0.11f, 0.7f, 0.75f, 0.8f, 0.93f })
                                groove = Mathf.Max(groove, 1f - Sm(0.004f, 0.012f, Mathf.Abs(v - gv)));
                            float n = Fbm(u, v, 8, 8, 4, 172), fine = Fbm(u, v, 80, 80, 2, 173);
                            float wear = Mathf.Max(band * Sm(0.55f, 0.9f, pyr) * Sm(0.35f, 0.7f, n), sc[i] * (1f - band) * 0.8f);
                            o.h[i] = 0.45f + band * 0.45f * pyr - 0.4f * groove * (1f - band) + 0.04f * (fine - 0.5f) - 0.12f * sc[i] * (1f - band);
                            o.tone[i] = 0.9f + 0.16f * (n - 0.5f) - 0.25f * band * (1f - pyr) - 0.3f * groove * (1f - band);
                            o.stain[i] = wear;
                            o.metal[i] = 0.8f + 0.2f * wear;
                            o.smooth[i] = Mathf.Clamp01(0.5f + 0.1f * (n - 0.5f) + 0.22f * wear - 0.2f * band * (1f - pyr) - 0.2f * groove);
                            o.ao[i] = 1f - 0.45f * band * (1f - pyr) - 0.5f * groove * (1f - band);
                        });
                        break;
                    }
                default: // "blooddry"
                    s = Make((o, i, u, v) =>
                    {
                        Worley(u, v, 20, 161, out float f1, out float f2);
                        float crack = 1f - Sm(0f, 0.06f, f2 - f1);
                        float n = Fbm(u, v, 5, 5, 5, 162), wet = Sm(0.55f, 0.75f, Fbm(u, v, 4, 4, 4, 163));
                        o.h[i] = 0.55f + 0.2f * (n - 0.5f) - 0.4f * crack * (1f - wet) + 0.12f * (1f - f1);
                        o.tone[i] = 0.7f + 0.6f * n - 0.4f * crack * (1f - wet);
                        o.stain[i] = Sm(0.4f, 0.7f, Fbm(u, v, 7, 7, 4, 164)) * (1f - wet);
                        o.smooth[i] = Mathf.Clamp01(0.2f + 0.62f * wet - 0.15f * crack);
                        o.ao[i] = 1f - 0.4f * crack * (1f - wet);
                    });
                    break;
            }
            cache[cls] = s;
            return s;
        }

        // ------------------------------------------------------------------ bảng vật liệu
        struct Spec
        {
            public string cls; public Color stain; public float amount, smooth, bump, metal;
            public Spec(string cls, float r, float g, float b, float amount, float smooth = 1f, float bump = 1f, float metal = -1f)
            { this.cls = cls; stain = new Color(r, g, b); this.amount = amount; this.smooth = smooth; this.bump = bump; this.metal = metal; }
        }

        static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            { "M_Iron", new Spec("iron", 0.30f, 0.13f, 0.05f, 0.75f) },
            { "M_SteelSafe", new Spec("iron", 0.2f, 0.09f, 0.04f, 0.4f, 1f, 0.8f) },
            { "M_BW_CrowbarSteel", new Spec("iron", 0.28f, 0.12f, 0.05f, 0.55f, 1.1f, 0.8f) },
            { "M_Rust", new Spec("rust", 0.46f, 0.21f, 0.07f, 0.7f, 1f, 1.2f) },
            { "M_Bronze", new Spec("brass", 0.07f, 0.15f, 0.12f, 0.6f, 1f, 0.7f) },
            { "M_BronzeDark", new Spec("brass", 0.04f, 0.09f, 0.07f, 0.7f, 0.8f, 0.9f) },
            { "M_BW_Brass", new Spec("brass", 0.10f, 0.14f, 0.09f, 0.5f, 1f, 0.6f) },
            { "M_BW_BrassDull", new Spec("brass", 0.05f, 0.09f, 0.065f, 0.7f, 0.75f, 0.9f) },
            { "M_Gilt", new Spec("brass", 0.13f, 0.08f, 0.03f, 0.45f, 1f, 0.5f) },
            { "M_Chrome", new Spec("chrome", 0.22f, 0.19f, 0.15f, 0.55f, 1f, 0.5f) },
            { "M_WoodDark", new Spec("wood", 0.025f, 0.017f, 0.012f, 0.55f) },
            { "M_BW_Oak", new Spec("wood", 0.04f, 0.025f, 0.015f, 0.45f) },
            { "M_LacquerRed", new Spec("lacquer", 0.20f, 0.15f, 0.11f, 0.5f, 1f, 0.8f) },
            { "M_StoneAltar", new Spec("stone", 0.06f, 0.075f, 0.05f, 0.5f) },
            { "M_ClothBeige", new Spec("cloth", 0.22f, 0.16f, 0.10f, 0.55f) },
            { "M_ClothRed", new Spec("cloth", 0.10f, 0.015f, 0.015f, 0.55f) },
            { "M_RagBlood", new Spec("cloth", 0.09f, 0.008f, 0.008f, 0.65f) },
            { "M_Shroud", new Spec("cloth", 0.36f, 0.29f, 0.17f, 0.55f) },
            { "M_ShroudBlood", new Spec("cloth", 0.07f, 0.008f, 0.008f, 0.55f) },
            { "M_ShroudDirt", new Spec("cloth", 0.09f, 0.07f, 0.045f, 0.6f) },
            { "M_BW_Canvas", new Spec("cloth", 0.05f, 0.045f, 0.03f, 0.45f) },
            { "M_BW_CanvasDark", new Spec("cloth", 0.025f, 0.022f, 0.016f, 0.45f) },
            { "M_AoBaBa", new Spec("cloth", 0.03f, 0.028f, 0.025f, 0.35f) },
            { "M_Rope", new Spec("cloth", 0.10f, 0.07f, 0.04f, 0.6f, 1f, 1.3f) },
            { "M_BW_Leather", new Spec("leather", 0.30f, 0.17f, 0.09f, 0.45f) },
            { "M_PaperOld", new Spec("paper", 0.42f, 0.26f, 0.11f, 0.65f) },
            { "M_PlasticBlack", new Spec("plastic", 0.17f, 0.16f, 0.14f, 0.4f) },
            { "M_PlasticRed", new Spec("plastic", 0.20f, 0.12f, 0.10f, 0.35f) },
            { "M_BW_Keycap", new Spec("plastic", 0.30f, 0.24f, 0.14f, 0.5f) },
            { "M_BW_Tyre", new Spec("plastic", 0.22f, 0.18f, 0.13f, 0.6f, 0.45f, 1.2f) },
            { "M_BW_Typewriter", new Spec("paint", 0.22f, 0.11f, 0.05f, 0.7f, 1f, 0.7f) },
            { "M_BW_SignWhite", new Spec("paint", 0.30f, 0.14f, 0.06f, 0.7f, 0.7f) },
            { "M_BW_SignGreen", new Spec("paint", 0.30f, 0.14f, 0.06f, 0.7f, 0.7f) },
            { "M_BW_CarPaint", new Spec("paint", 0.17f, 0.14f, 0.10f, 0.6f, 1.1f, 0.6f) },
            { "M_Wax", new Spec("wax", 0.28f, 0.19f, 0.09f, 0.5f) },
            { "M_BoneOld", new Spec("bone", 0.30f, 0.19f, 0.09f, 0.65f) },
            { "M_Horn", new Spec("bone", 0.04f, 0.025f, 0.015f, 0.5f, 1.5f, 0.7f) },
            { "M_DollSkin", new Spec("skin", 0.24f, 0.17f, 0.11f, 0.55f) },
            { "M_CorpseHand", new Spec("skin", 0.26f, 0.13f, 0.21f, 0.6f) },
            { "M_BloodDried", new Spec("blooddry", 0.22f, 0.05f, 0.03f, 0.5f) },
            // Đèn pin (Slot 1, khối primitive của Unity): chỉ thêm chất liệu thân, không đụng transform / IK / đèn. Radar (Slot 2) giữ nguyên.
            { "M_FlashlightBody", new Spec("knurl", 0.5f, 0.5f, 0.52f, 0.7f, 1f, 1.1f) },
        };

        // ------------------------------------------------------------------ ghi texture
        static Texture2D Save(string name, Color[] px, bool srgb, bool normal)
        {
            BlackwoodDataBuilder.EnsureFolder(TexDir);
            string path = TexDir + "/" + name + ".png";
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            BlackwoodDataBuilder.WriteBytesIfChanged(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = srgb; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Repeat;
            ti.anisoLevel = 4; ti.filterMode = FilterMode.Trilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Color[] Normal(float[] h, float strength)
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

        static Texture2D ClassTex(string cls, bool normal, bool force)
        {
            string name = "T_Prop_" + cls + (normal ? "_N" : "_M");
            var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + name + ".png");
            if (ex != null && !force) return ex;
            var s = Class(cls);
            if (normal) return Save(name, Normal(s.h, 5f), false, true);
            var px = new Color[S * S];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(s.metal[i], s.ao[i], 0f, s.smooth[i]);
            return Save(name, px, false, false);
        }

        /// <summary>Độ cao (0..1, lặp liền mạch, cỡ <see cref="Size"/>) của một loại bề mặt — dùng để trộn vào texture khác.</summary>
        public static float[] ClassHeight(string cls) => Class(cls).h;
        public const int Size = S;

        /// <summary>Ghi normal map chi tiết của một loại bề mặt ra đường dẫn bất kỳ (da, vải cho nhân vật).</summary>
        public static Texture2D DetailNormal(string cls, string path, float strength)
        {
            var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (ex != null) return ex;
            var px = Normal(Class(cls).h, strength);
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            BlackwoodDataBuilder.WriteBytesIfChanged(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.NormalMap; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Repeat;
            ti.anisoLevel = 4; ti.filterMode = FilterMode.Trilinear;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Màu gốc của vật liệu (trước khi đổ vào texture) lưu ở userData của .mat để chạy lại không bị nhân đôi.</summary>
        static Color BaseColor(Material m, string path)
        {
            var imp = AssetImporter.GetAtPath(path);
            const string key = "bwbase=";
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (imp != null && imp.userData != null && imp.userData.StartsWith(key))
            {
                var p = imp.userData.Substring(key.Length).Split(';');
                if (p.Length == 3) return new Color(float.Parse(p[0], inv), float.Parse(p[1], inv), float.Parse(p[2], inv));
            }
            var c = m.GetColor("_BaseColor");
            if (imp != null)
            {
                imp.userData = key + c.r.ToString("R", inv) + ";" + c.g.ToString("R", inv) + ";" + c.b.ToString("R", inv);
                imp.SaveAndReimport();
            }
            return c;
        }

        [MenuItem("Blackwood/Build/Prop Textures (regenerate)")]
        public static void Menu() => Debug.Log(Apply(true));

        public static string Apply(bool force = false)
        {
            if (EditorApplication.isPlaying) return "Đang Play.";
            cache.Clear();
            int n = 0;
            var paths = new List<string>();
            if (AssetDatabase.IsValidFolder(MatDir)) paths.AddRange(Directory.GetFiles(MatDir, "*.mat").Select(p => p.Replace('\\', '/')));
            paths.Add("Assets/BlackwoodManor/Materials/M_BW_CrowbarSteel.mat");
            paths.Add("Assets/BlackwoodManor/Prefabs/Items/Materials/M_FlashlightBody.mat");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var path in paths)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader != lit || !Specs.TryGetValue(m.name, out var sp)) continue;
                var cur = m.GetTexture("_BaseMap");
                if (cur != null && !AssetDatabase.GetAssetPath(cur).StartsWith(TexDir)) continue; // đã có texture riêng → không đụng
                var surf = Class(sp.cls);
                Color bc = BaseColor(m, path);
                string an = "T_Prop_" + m.name.Substring(2) + "_A";
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + an + ".png");
                if (albedo == null || force)
                {
                    var px = new Color[S * S];
                    for (int i = 0; i < px.Length; i++)
                    {
                        float t = surf.tone[i], k = Mathf.Clamp01(surf.stain[i] * sp.amount);
                        var c = new Color(bc.r * t, bc.g * t, bc.b * t);
                        float st = 0.75f + 0.5f * (t - 0.75f);
                        c = Color.Lerp(c, new Color(sp.stain.r * st, sp.stain.g * st, sp.stain.b * st), k);
                        px[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                    }
                    albedo = Save(an, px, true, false);
                }
                var nrm = ClassTex(sp.cls, true, force && !doneClass.Contains(sp.cls + "N"));
                var msk = ClassTex(sp.cls, false, force && !doneClass.Contains(sp.cls + "M"));
                doneClass.Add(sp.cls + "N"); doneClass.Add(sp.cls + "M");
                m.SetTexture("_BaseMap", albedo); m.SetTexture("_MainTex", albedo); m.SetColor("_BaseColor", Color.white); m.SetColor("_Color", Color.white);
                m.SetTextureScale("_BaseMap", Vector2.one);
                m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", sp.bump); m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap", msk); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", Mathf.Clamp01(sp.smooth)); m.SetFloat("_SmoothnessTextureChannel", 0f);
                m.SetTexture("_OcclusionMap", msk); m.SetFloat("_OcclusionStrength", 1f); m.EnableKeyword("_OCCLUSIONMAP");
                BlackwoodMaterialUtil.Validate(m);
                n++;
            }
            doneClass.Clear();
            cache.Clear();
            AssetDatabase.SaveAssets();
            return "Prop textures: " + n + " vật liệu";
        }

        static readonly HashSet<string> doneClass = new HashSet<string>();

        // ------------------------------------------------------------------ màn hình thiết bị
        static void Box(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++)
                    px[y * w + x] = Color.Lerp(px[y * w + x], c, c.a);
        }

        static void Disc(Color[] px, int w, int h, float cx, float cy, float r, Color c)
        {
            for (int y = Mathf.Max(0, (int)(cy - r - 1)); y < Mathf.Min(h, (int)(cy + r + 2)); y++)
                for (int x = Mathf.Max(0, (int)(cx - r - 1)); x < Mathf.Min(w, (int)(cx + r + 2)); x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float k = Mathf.Clamp01(r - d + 0.5f) * c.a;
                    px[y * w + x] = Color.Lerp(px[y * w + x], c, k);
                }
        }

        /// <summary>
        /// Vật liệu màn hình phát sáng cho điện thoại (giao diện livestream) hoặc màn lật máy quay (khung ngắm nhìn đêm).
        /// Model Blender đã trải UV màn hình khít 0..1 (UV_FIT trong props_common.py). Chữ vẽ bằng thanh màu vì màn hình quá nhỏ để đọc.
        /// </summary>
        public static Material ScreenMaterial(bool phone)
        {
            string name = phone ? "PhoneScreen" : "CamScreen";
            int w = phone ? 256 : 384, h = phone ? 512 : 256;
            var px = new Color[w * h];
            var rnd = new System.Random(phone ? 5 : 9);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float n = Fbm(u, v, 6, 6, 4, 201), g = H(x, y, 202);
                    float vig = 1f - Mathf.Clamp01(((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2.6f);
                    if (phone)
                    {
                        // khung hình camera: hành lang tối, khung cửa nhạt ở giữa
                        float door = (1f - Sm(0.1f, 0.16f, Mathf.Abs(u - 0.5f))) * Sm(0.34f, 0.4f, v) * (1f - Sm(0.7f, 0.76f, v));
                        float b = (0.035f + 0.07f * n + 0.1f * door) * vig + 0.02f * g;
                        px[y * w + x] = new Color(b * 0.8f, b * 0.9f, b * 1.25f, 1f);
                    }
                    else
                    {
                        float shape = (1f - Sm(0.1f, 0.5f, Mathf.Abs(u - 0.5f) * 2f)) * Sm(0.15f, 0.5f, v);
                        float b = (0.1f + 0.3f * n * shape + 0.12f * g) * (0.35f + 0.65f * vig);
                        float scan = 0.85f + 0.15f * ((y & 1) == 0 ? 1f : 0f);
                        px[y * w + x] = new Color(b * 0.25f * scan, b * 1.15f * scan, b * 0.3f * scan, 1f);
                    }
                }
            if (phone)
            {
                var red = new Color(0.9f, 0.08f, 0.06f, 1f); var white = new Color(0.92f, 0.92f, 0.92f, 1f);
                Box(px, w, h, 14, h - 46, 74, h - 22, red);                         // nhãn LIVE
                Box(px, w, h, 22, h - 38, 66, h - 30, white);
                Disc(px, w, h, 90, h - 34, 5, white);                               // số người xem
                Box(px, w, h, 100, h - 38, 150, h - 30, new Color(0.9f, 0.9f, 0.9f, 0.85f));
                Box(px, w, h, w - 40, h - 44, w - 16, h - 40, white); Box(px, w, h, w - 40, h - 36, w - 16, h - 32, white); Box(px, w, h, w - 40, h - 28, w - 16, h - 24, white);
                Box(px, w, h, 0, 0, w, 216, new Color(0f, 0f, 0f, 0.45f));           // nền khung chat
                for (int k = 0; k < 7; k++)
                {
                    int y0 = 58 + k * 22; int nw = 26 + rnd.Next(34), tw = 50 + rnd.Next(100);
                    var hue = Color.HSVToRGB((float)rnd.NextDouble(), 0.45f, 0.95f); hue.a = 1f;
                    Box(px, w, h, 14, y0, 14 + nw, y0 + 8, hue);
                    Box(px, w, h, 20 + nw, y0, Mathf.Min(w - 40, 20 + nw + tw), y0 + 8, new Color(0.8f, 0.8f, 0.8f, 0.8f));
                }
                Box(px, w, h, 12, 16, w - 54, 44, new Color(1f, 1f, 1f, 0.16f));   // ô nhập
                Disc(px, w, h, w - 30, 30, 13, new Color(0.95f, 0.25f, 0.4f, 1f));  // nút tim
                for (int k = 0; k < 4; k++) Disc(px, w, h, w - 26 - (k % 2) * 8, 240 + k * 34, 7 - k, new Color(0.95f, 0.3f, 0.45f, 0.9f - k * 0.18f));
            }
            else
            {
                var gr = new Color(0.55f, 1f, 0.6f, 0.95f);
                int m = 22, L = 34;
                foreach (int sx in new[] { 0, 1 })
                    foreach (int sy in new[] { 0, 1 })
                    {
                        int x = sx == 0 ? m : w - m, y = sy == 0 ? m : h - m;
                        Box(px, w, h, sx == 0 ? x : x - L, y - 1, sx == 0 ? x + L : x, y + 2, gr);
                        Box(px, w, h, x - 1, sy == 0 ? y : y - L, x + 2, sy == 0 ? y + L : y, gr);
                    }
                Box(px, w, h, w / 2 - 14, h / 2 - 1, w / 2 + 14, h / 2 + 1, gr); Box(px, w, h, w / 2 - 1, h / 2 - 14, w / 2 + 1, h / 2 + 14, gr);
                Disc(px, w, h, 44, h - 44, 7, new Color(1f, 0.1f, 0.08f, 1f));       // REC
                Box(px, w, h, 58, h - 49, 104, h - 40, gr);
                Box(px, w, h, w - 86, h - 52, w - 44, h - 36, gr); Box(px, w, h, w - 83, h - 49, w - 47, h - 39, new Color(0.02f, 0.12f, 0.03f, 1f));
                Box(px, w, h, w - 81, h - 47, w - 62, h - 41, gr); Box(px, w, h, w - 44, h - 47, w - 40, h - 41, gr);  // pin
                for (int k = 0; k < 4; k++) Box(px, w, h, 44 + k * 30, 36, 66 + k * 30, 45, gr);                       // mã thời gian
            }
            BlackwoodDataBuilder.EnsureFolder(TexDir);
            string tp = TexDir + "/T_Prop_" + name + ".png";
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            BlackwoodDataBuilder.WriteBytesIfChanged(tp, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(tp, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None; ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);

            string mp = MatDir + "/M_BW_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, mp); }
            mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); mat.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f));
            mat.SetFloat("_Smoothness", 0.92f); mat.SetFloat("_Metallic", 0f);
            mat.SetTexture("_EmissionMap", tex); mat.SetColor("_EmissionColor", Color.white * (phone ? 1.6f : 1.3f));
            BlackwoodMaterialUtil.Emissive(mat, Color.white * (phone ? 1.6f : 1.3f), tex);
            BlackwoodMaterialUtil.Validate(mat);
            return mat;
        }

        // ------------------------------------------------------------------ xem trước
        /// <summary>Chụp toàn bộ model đạo cụ thành các tấm ảnh lưới (Temp/Blackwood/props_N.png), không đụng scene đang mở.</summary>
        [MenuItem("Blackwood/Build/Preview Props")]
        public static void PreviewMenu() => Debug.Log(Preview());

        public static string Preview(string only = null, float spotIntensity = 0f)
        {
            var files = Directory.GetFiles("Assets/BlackwoodManor/Models/Props", "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToList();
            if (!string.IsNullOrEmpty(only)) files = files.Where(f => only.Split(',').Any(o => f.Contains(o))).ToList();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            int sheets = 0;
            try
            {
                GameObject New(string name) { var g = new GameObject(name); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scene); return g; }
                var key = New("L").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = spotIntensity > 0f ? 0.15f : 1.5f; key.color = new Color(1f, 0.96f, 0.9f);
                key.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
                var fill = New("F").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = spotIntensity > 0f ? 0.05f : 0.45f; fill.color = new Color(0.6f, 0.7f, 0.9f);
                fill.transform.rotation = Quaternion.Euler(20f, -60f, 0f);
                var camGo = New("C"); var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
                cam.fieldOfView = 24f; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
                Light spot = null;
                if (spotIntensity > 0f)
                {
                    spot = camGo.AddComponent<Light>(); spot.type = LightType.Spot; spot.spotAngle = 50f; spot.innerSpotAngle = 20f;
                    spot.intensity = spotIntensity; spot.range = 20f; spot.color = new Color(1f, 0.95f, 0.85f);
                }
                Directory.CreateDirectory("Temp/Blackwood");
                { var warm = RenderTexture.GetTemporary(64, 64, 24); cam.targetTexture = warm; cam.Render(); cam.targetTexture = null; RenderTexture.ReleaseTemporary(warm); }
                const int cols = 4, rows = 3, cw = 480, ch = 400;
                for (int start = 0; start < files.Count; start += cols * rows)
                {
                    var sheet = new Texture2D(cols * cw, rows * ch, TextureFormat.RGB24, false);
                    for (int k = 0; k < cols * rows && start + k < files.Count; k++)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(files[start + k]);
                        var go = Object.Instantiate(prefab);
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                        var rs = go.GetComponentsInChildren<Renderer>();
                        if (rs.Length > 0)
                        {
                            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                            float rad = b.extents.magnitude;
                            float dist = rad / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                            var dir = new Vector3(0.55f, 0.45f, 0.75f).normalized;
                            camGo.transform.position = b.center + dir * dist; camGo.transform.LookAt(b.center);
                            if (spot != null) spot.range = dist * 3f;
                            var rt = RenderTexture.GetTemporary(cw, ch, 24, RenderTextureFormat.ARGB32);
                            cam.aspect = (float)cw / ch; cam.targetTexture = rt; cam.Render();
                            var prev = RenderTexture.active; RenderTexture.active = rt;
                            sheet.ReadPixels(new Rect(0, 0, cw, ch), (k % cols) * cw, (rows - 1 - k / cols) * ch);
                            RenderTexture.active = prev; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
                        }
                        Object.DestroyImmediate(go);
                    }
                    sheet.Apply();
                    File.WriteAllBytes("Temp/Blackwood/props_" + sheets + ".png", sheet.EncodeToPNG());
                    Object.DestroyImmediate(sheet);
                    sheets++;
                }
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
            return "Preview props: " + files.Count + " model, " + sheets + " ảnh";
        }
    }
}
