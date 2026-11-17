using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Lớp không khí âm thanh STEREO (đeo tai nghe mới thấy rõ): mưa trên mái, gió rít qua khe cửa, sấm, cụm đàn dây nghịch tai
    /// tăng căng thẳng, drone nền, bước chân nặng của Mrs. Peake trên sàn gỗ, tiếng rè điện khi đèn chập chờn.
    /// Tất cả tổng hợp bằng code — không dùng mẫu có bản quyền.
    /// </summary>
    public static partial class BlackwoodAudioSynth
    {
        /// <summary>Tên các clip 2 kênh (BuildAll không ép mono).</summary>
        public static readonly HashSet<string> StereoNames = new HashSet<string>();

        static List<string> BuildAtmos()
        {
            var made = new List<string>();
            void S(string n, float[] l, float[] r) { WriteStereo(n, l, r); made.Add(n); StereoNames.Add(n); }
            void W(string n, float[] d) { Write(n, d); made.Add(n); }

            Rain(18f, out var rl, out var rr); S("amb_rain_roof_loop", rl, rr);
            Wind(16f, out var wl, out var wr); S("amb_wind_howl_loop", wl, wr);
            for (int i = 0; i < 3; i++) { Thunder(6.5f + i, 40 + i * 13, i == 0, out var tl, out var tr); S("sfx_thunder_0" + (i + 1), tl, tr); }
            Cluster(16f, out var cl, out var cr); S("mus_tension_cluster_loop", cl, cr);
            Dread(20f, out var dl, out var dr); S("mus_dread_drone_loop", dl, dr);
            for (int i = 0; i < 4; i++) W("hk_step_0" + (i + 1), HeavyStep(0.55f, 900 + i * 37, i % 2 == 1));
            W("sfx_light_buzz", LightBuzz(0.35f));
            return made;
        }

        // ---------------------------------------------------------------- Mưa: nền rào rào + giọt gõ mái tôn/ngói + máng nước nhỏ giọt
        static void Rain(float sec, out float[] L, out float[] R)
        {
            int n = (int)(SR * sec); L = new float[n]; R = new float[n];
            for (int ch = 0; ch < 2; ch++)
            {
                var b = ch == 0 ? L : R; var r = new System.Random(500 + ch * 91);
                float hp = 0, prev = 0, lp = 0, lp2 = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR;
                    float w = Noise(r);
                    hp = 0.86f * (hp + w - prev); prev = w;             // xì xào cao
                    lp += (hp - lp) * 0.42f;
                    lp2 += (w - lp2) * 0.035f;                            // ù ù của mái
                    float swell = 0.75f + 0.25f * Mathf.Sin(TAU * t / sec * 2f + ch * 1.3f) + 0.12f * Mathf.Sin(TAU * t / sec * 5f + ch);
                    b[i] = lp * 0.22f * swell + lp2 * 0.9f * swell;
                }
                // Giọt lớn gõ lên mái / bậu cửa (click có cộng hưởng ngắn)
                int drops = (int)(sec * 55);
                for (int k = 0; k < drops; k++)
                {
                    int s0 = r.Next(n - 2000); float f = 900f + (float)r.NextDouble() * 2600f, amp = 0.05f + 0.12f * (float)(r.NextDouble() * r.NextDouble());
                    for (int i = 0; i < 1400; i++) { float t = (float)i / SR; b[s0 + i] += Mathf.Sin(TAU * f * t) * Mathf.Exp(-t * 160f) * amp; }
                }
                // Máng nước nhỏ giọt đều (một bên tai mỗi cái)
                float period = ch == 0 ? 0.83f : 1.27f;
                for (float t0 = 0.3f; t0 < sec - 0.5f; t0 += period * (0.92f + 0.16f * (float)r.NextDouble()))
                {
                    int s0 = (int)(t0 * SR); float f0 = ch == 0 ? 1450f : 1120f;
                    for (int i = 0; i < 2600 && s0 + i < n; i++) { float t = (float)i / SR; b[s0 + i] += Mathf.Sin(TAU * (f0 + 900f * t * 8f) * t) * Mathf.Exp(-t * 70f) * 0.07f; }
                }
            }
            L = LoopFade(Normalize(L, 0.6f), 1.5f); R = LoopFade(Normalize(R, 0.6f), 1.5f);
        }

        // ---------------------------------------------------------------- Gió rít: nhiễu qua bộ cộng hưởng quét chậm (khe cửa, ống khói)
        static void Wind(float sec, out float[] L, out float[] R)
        {
            int n = (int)(SR * sec); L = new float[n]; R = new float[n];
            for (int ch = 0; ch < 2; ch++)
            {
                var b = ch == 0 ? L : R; var r = new System.Random(800 + ch * 17);
                float y1 = 0, v1 = 0, y2 = 0, v2 = 0, lp = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR, ph = TAU * t / sec;
                    float w = Noise(r); lp += (w - lp) * 0.2f;
                    float gust = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(ph * 2f + ch * 0.9f + 0.6f * Mathf.Sin(ph * 5f)), 2.2f);
                    float f1 = 260f + 240f * gust + 60f * Mathf.Sin(ph * 7f + ch);
                    float f2 = 610f + 520f * gust + 90f * Mathf.Sin(ph * 3f + ch * 2f);
                    Resonate(lp, f1, 0.9965f, ref y1, ref v1);
                    Resonate(lp, f2, 0.995f, ref y2, ref v2);
                    b[i] = (y1 * 0.7f + y2 * 0.4f) * (0.15f + 0.85f * gust) + lp * 0.03f;
                }
            }
            L = LoopFade(Normalize(L, 0.55f), 1.5f); R = LoopFade(Normalize(R, 0.55f), 1.5f);
        }

        // ---------------------------------------------------------------- Sấm: tiếng nổ xé + ầm ì lăn dài, dội giữa hai tai
        static void Thunder(float sec, int seed, bool close, out float[] L, out float[] R)
        {
            int n = (int)(SR * sec); L = new float[n]; R = new float[n];
            for (int ch = 0; ch < 2; ch++)
            {
                var b = ch == 0 ? L : R; var r = new System.Random(seed + ch * 7);
                float lp1 = 0, lp2 = 0, lp3 = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR;
                    float w = Noise(r);
                    lp1 += (w - lp1) * 0.5f;      // xé
                    lp2 += (w - lp2) * 0.03f;     // ầm
                    lp3 += (lp2 - lp3) * 0.02f;   // rền rất trầm
                    float crack = close ? Mathf.Exp(-t * 9f) * (0.6f + 0.4f * Mathf.Sin(TAU * 37f * t)) : Mathf.Exp(-t * 3f) * 0.25f * Mathf.Clamp01(t / 0.2f);
                    // các đợt ầm nối nhau
                    float roll = 0f;
                    for (int k = 0; k < 5; k++)
                    {
                        float tk = 0.25f + k * (0.55f + 0.2f * Mathf.Sin(seed + k)) + ch * 0.07f * k;
                        float d = t - tk; if (d > 0f) roll += Mathf.Exp(-d * (1.1f + 0.25f * k)) * (1f - Mathf.Exp(-d * 25f)) * (1f - 0.13f * k);
                    }
                    float tail = Mathf.Clamp01((sec - t) / 2.5f);
                    b[i] = (lp1 * crack * 0.9f + lp2 * roll * 7f + lp3 * roll * 26f) * tail;
                }
            }
            float pk = close ? 0.95f : 0.7f;
            L = Normalize(L, pk); R = Normalize(R, pk);
        }

        // ---------------------------------------------------------------- Cụm dây nghịch tai (căng thẳng): 2 bè cách nhau nửa cung, tremolo, lọc quét
        static void Cluster(float sec, out float[] L, out float[] R)
        {
            int n = (int)(SR * sec); L = new float[n]; R = new float[n];
            float[] semis = { 0f, 1f, 2f, 6f, 7f, 13f };
            for (int ch = 0; ch < 2; ch++)
            {
                var b = ch == 0 ? L : R; var r = new System.Random(300 + ch * 5);
                var ph = new float[semis.Length * 2];
                float lp = 0, lp2 = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR, cyc = TAU * t / sec;
                    float v = 0f;
                    for (int k = 0; k < semis.Length; k++)
                    {
                        float f = 196f * Mathf.Pow(2f, semis[k] / 12f) * (1f + 0.004f * Mathf.Sin(cyc * (3 + k) + ch * 1.7f + k)); // vibrato chậm, lệch pha 2 tai
                        for (int d = 0; d < 2; d++)
                        {
                            int j = k * 2 + d;
                            ph[j] += f * (d == 0 ? 1f : 1.006f) / SR;
                            float saw = Mathf.Repeat(ph[j], 1f) * 2f - 1f;
                            v += saw * (k < 3 ? 0.16f : 0.1f);
                        }
                    }
                    float trem = 0.72f + 0.28f * Mathf.Sin(TAU * (9.5f + 1.5f * Mathf.Sin(cyc * 2f)) * t + ch); // tremolo run tay
                    float bow = Noise(r) * 0.05f;                                                              // tiếng mã vĩ cọ dây
                    float cut = 0.035f + 0.05f * (0.5f + 0.5f * Mathf.Sin(cyc + ch * 0.6f));
                    lp += ((v * trem + bow) - lp) * cut; lp2 += (lp - lp2) * cut;
                    float swell = 0.45f + 0.55f * Mathf.Pow(0.5f - 0.5f * Mathf.Cos(cyc * 2f), 1.5f);
                    b[i] = lp2 * swell;
                }
            }
            L = LoopFade(Normalize(L, 0.6f), 1.2f); R = LoopFade(Normalize(R, 0.6f), 1.2f);
        }

        // ---------------------------------------------------------------- Drone nền: sub phách chậm + hồi âm kim loại xa
        static void Dread(float sec, out float[] L, out float[] R)
        {
            int n = (int)(SR * sec); L = new float[n]; R = new float[n];
            for (int ch = 0; ch < 2; ch++)
            {
                var b = ch == 0 ? L : R; var r = new System.Random(120 + ch * 3);
                float lp = 0, y = 0, vv = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR, cyc = TAU * t / sec;
                    float sub = 0.5f * Mathf.Sin(TAU * 41.2f * t) + 0.35f * Mathf.Sin(TAU * (41.2f * 1.498f + (ch == 0 ? 0.31f : -0.27f)) * t)
                              + 0.2f * Mathf.Sin(TAU * 82.9f * t + 2f * Mathf.Sin(cyc * 3f));
                    float w = Noise(r); lp += (w - lp) * 0.004f;
                    Resonate(w * 0.02f, 1870f + 240f * Mathf.Sin(cyc * 2f + ch * 2f), 0.9994f, ref y, ref vv); // ngân kim loại
                    float breathe = 0.6f + 0.4f * Mathf.Sin(cyc * 3f + ch * 0.8f);
                    b[i] = sub * 0.5f * breathe + lp * 9f + y * 0.05f * (0.5f + 0.5f * Mathf.Sin(cyc * 5f + ch));
                }
            }
            L = LoopFade(Normalize(L, 0.6f), 1.5f); R = LoopFade(Normalize(R, 0.6f), 1.5f);
        }

        // ---------------------------------------------------------------- Bước chân nặng trên sàn gỗ cũ (gót nện + ván rung + đôi khi cót két)
        static float[] HeavyStep(float sec, int seed, bool creak)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(seed);
            float f0 = 62f + (float)r.NextDouble() * 22f, lp = 0, y = 0, v = 0;
            float cf = 380f + (float)r.NextDouble() * 260f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float thud = Mathf.Sin(TAU * (f0 + 60f * Mathf.Exp(-t * 60f)) * t) * Mathf.Exp(-t * 16f);
                float w = Noise(r); lp += (w - lp) * 0.3f;
                float slap = lp * Mathf.Exp(-t * 90f) * 0.7f;
                Resonate(w * Mathf.Exp(-t * 50f), 210f, 0.992f, ref y, ref v); // ván sàn rung
                float ck = 0f;
                if (creak && t > 0.08f)
                {
                    float tt = t - 0.08f, e = Mathf.Sin(Mathf.PI * Mathf.Clamp01(tt / 0.4f));
                    ck = (Mathf.Repeat(cf * (1f + 0.5f * tt) * tt, 1f) - 0.5f) * (0.5f + 0.5f * Mathf.Sin(TAU * 43f * tt)) * e * 0.16f;
                }
                b[i] = thud * 0.9f + slap + y * 0.08f + ck;
            }
            return Normalize(b, 0.85f);
        }

        // ---------------------------------------------------------------- Rè điện ngắn khi đèn chập chờn
        static float[] LightBuzz(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(66);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float hum = Mathf.Sign(Mathf.Sin(TAU * 100f * t)) * 0.25f + Mathf.Sin(TAU * 2400f * t) * 0.12f;
                float gate = Mathf.PerlinNoise(t * 60f, 0.5f) > 0.45f ? 1f : 0.1f;
                float tick = r.NextDouble() < 0.004 ? Noise(r) * 2f : 0f;
                b[i] = (hum * gate + tick) * Mathf.Clamp01(t / 0.01f) * Mathf.Clamp01((sec - t) / 0.08f);
            }
            return Normalize(b, 0.5f);
        }

        internal static void WriteStereo(string name, float[] l, float[] r)
        {
            HighPass(l, 24f, name.Contains("loop")); HighPass(r, 24f, name.Contains("loop"));
            Master(l, r);
            int n = Mathf.Min(l.Length, r.Length);
            string path = Path.Combine(Application.dataPath, "../" + Dir + "/" + name + ".wav");
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int dataLen = n * 4;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataLen); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)2);
                w.Write(SR); w.Write(SR * 4); w.Write((short)4); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataLen);
                for (int i = 0; i < n; i++)
                {
                    w.Write((short)Mathf.Clamp(l[i] * 32767f, -32768f, 32767f));
                    w.Write((short)Mathf.Clamp(r[i] * 32767f, -32768f, 32767f));
                }
            }
        }
    }
}
