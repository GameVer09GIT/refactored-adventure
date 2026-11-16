using System.Collections.Generic;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Âm thanh cho cơ chế rọi đèn pin chống lại Mrs. Peake: tiếng rít xèo xèo khi bị rọi (loop),
    /// tiếng thét đau khi chói mắt đến choáng, tiếng "ù" báo đèn có tác dụng.
    /// </summary>
    public static partial class BlackwoodAudioSynth
    {
        static List<string> BuildCombat()
        {
            var made = new List<string>();
            void W(string n, float[] d) { Write(n, d); made.Add(n); }
            W("hk_burn_hiss_loop", BurnHiss(3.6f));
            W("hk_blind_shriek", BlindShriek(2.3f));
            return made;
        }

        // Rít + xèo: nhiễu thông cao bị điều biên gấp (da cháy xèo) chồng tiếng gầm gừ cổ họng (răng cưa trầm qua 2 formant)
        static float[] BurnHiss(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(311);
            float hp = 0, prev = 0, lp = 0, f1 = 0, f1v = 0, f2 = 0, f2v = 0, ph = 0, crackle = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float w = Noise(r);
                // thông cao bậc 1
                hp = 0.92f * (hp + w - prev); prev = w;
                float flutter = 0.55f + 0.45f * Mathf.PerlinNoise(t * 17f, 0.3f);
                float hiss = hp * 0.5f * flutter;
                // lách tách ngẫu nhiên
                if (r.NextDouble() < 0.0016) crackle = 1f;
                crackle *= 0.985f;
                lp += (w - lp) * 0.6f;
                float crack = lp * crackle * 0.5f;
                // gầm gừ: răng cưa ~62 Hz rung giọng, qua 2 bộ cộng hưởng (650 Hz, 1150 Hz)
                float f0 = 62f * (1f + 0.07f * Mathf.Sin(TAU * 5.3f * t) + 0.05f * (Mathf.PerlinNoise(t * 3f, 7f) - 0.5f));
                ph += f0 / SR; float saw = Mathf.Repeat(ph, 1f) * 2f - 1f;
                float src = saw * (0.6f + 0.4f * Mathf.PerlinNoise(t * 11f, 2f)) + w * 0.12f;
                Resonate(src, 650f, 0.985f, ref f1, ref f1v);
                Resonate(src, 1150f, 0.98f, ref f2, ref f2v);
                float growl = (f1 * 0.05f + f2 * 0.03f);
                b[i] = hiss + crack + growl;
            }
            return LoopFade(Normalize(b, 0.7f), 0.5f);
        }

        // Thét đau: cao độ vọt lên rồi gãy xuống, giọng vỡ (điều biên thô) + hơi rít, đuôi nghẹn
        static float[] BlindShriek(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(977);
            float[] det = { 1f, 1.011f, 0.991f, 2.003f, 0.502f };
            float[] ph = new float[det.Length];
            float f1 = 0, f1v = 0, f2 = 0, f2v = 0, lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float f = t < 0.12f ? Mathf.Lerp(380f, 1500f, t / 0.12f)
                        : t < 0.9f ? 1500f - 250f * (t - 0.12f) + 90f * Mathf.Sin(TAU * 9f * t)
                        : Mathf.Lerp(1305f, 420f, Mathf.Clamp01((t - 0.9f) / 1.2f)) + 60f * Mathf.Sin(TAU * 13f * t);
                float v = 0;
                for (int k = 0; k < det.Length; k++) { ph[k] += f * det[k] / SR; v += (Mathf.Repeat(ph[k], 1f) * 2 - 1) * (k < 3 ? 0.3f : 0.14f); }
                // giọng vỡ: điều biên thô, càng về cuối càng đứt quãng
                float rough = 1f + 0.7f * Mathf.Sin(TAU * (64f + 30f * t) * t);
                float choke = t > 1.2f ? 0.55f + 0.45f * Mathf.Sign(Mathf.Sin(TAU * 17f * t)) * Mathf.PerlinNoise(t * 20f, 1f) : 1f;
                v *= rough * choke;
                float w = Noise(r); lp += (w - lp) * 0.55f;
                v += lp * (0.3f + 0.3f * Mathf.Clamp01(t - 0.8f));
                Resonate(v, 1100f, 0.97f, ref f1, ref f1v);
                Resonate(v, 2900f, 0.96f, ref f2, ref f2v);
                float outv = v * 0.6f + f1 * 0.02f + f2 * 0.012f;
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Pow(Mathf.Clamp01((sec - t) / 1.1f), 1.4f);
                b[i] = (float)System.Math.Tanh(outv * 2.4f) * env;
            }
            return Normalize(b, 0.95f);
        }

        /// <summary>Bộ cộng hưởng 2 cực (formant). y, v là trạng thái.</summary>
        static void Resonate(float x, float freq, float damp, ref float y, ref float v)
        {
            float k = 2f * Mathf.Sin(Mathf.PI * freq / SR);
            v = v * damp + (x - y) * k;
            y += v * k;
        }
    }
}
