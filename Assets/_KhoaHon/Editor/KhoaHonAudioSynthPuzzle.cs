using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Âm thanh tổng hợp cho lớp giải đố Blackwood: băng cassette, két sắt, bệ nến, xích hầm, rung nhà, nạy cửa sổ.
    /// </summary>
    public static partial class KhoaHonAudioSynth
    {
        static List<string> BuildPuzzle()
        {
            var made = new List<string>();
            void W(string n, float[] d) { Write(n, d); made.Add(n); }
            W("sfx_tape_hiss_loop", TapeHiss(6f));
            W("sfx_tape_click", MechClick(0.22f, 1400f, 3));
            W("sfx_safe_tick", MechClick(0.06f, 3200f, 11));
            W("sfx_safe_open", SafeOpen());
            W("sfx_safe_wrong", WrongThud());
            W("sfx_rumble", Rumble(4.5f));
            W("sfx_chains_break", ChainsBreak());
            W("sfx_candle_ignite", CandleIgnite());
            W("sfx_candle_out", CandleOut());
            W("sfx_window_pry", WoodCrack());
            W("sfx_prop_clatter", MechClick(0.35f, 2100f, 29));
            W("sfx_prop_thud", WrongThud());
            return made;
        }

        // Tiếng rè băng: nhiễu hồng lọc, rung tốc độ (wow/flutter), lốp bốp
        static float[] TapeHiss(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(303);
            float lp = 0, hp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float w = Noise(r); lp += (w - lp) * 0.18f; hp = lp - hp * 0.995f;
                float flutter = 0.8f + 0.2f * Mathf.Sin(TAU * 0.7f * t) + 0.05f * Mathf.Sin(TAU * 7.3f * t);
                float hum = 0.04f * Mathf.Sin(TAU * 50f * t) + 0.02f * Mathf.Sin(TAU * 100f * t);
                float pop = r.NextDouble() < 0.0004 ? Noise(r) * 2.5f : 0f;
                b[i] = lp * 0.35f * flutter + hum + pop;
            }
            return Normalize(LoopFade(b, 0.4f), 0.45f);
        }

        static float[] MechClick(float sec, float freq, int seed)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(seed);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 60f);
                float env2 = t > sec * 0.45f ? Mathf.Exp(-(t - sec * 0.45f) * 80f) * 0.5f : 0f;
                b[i] = (Mathf.Sin(TAU * freq * t) * 0.6f + Noise(r) * 0.4f) * (env + env2);
            }
            return Normalize(b, 0.8f);
        }

        // Két mở: tiếng chốt thép nặng + bản lề rít
        static float[] SafeOpen()
        {
            float sec = 2.2f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(77);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float clunk = t < 0.4f ? (Mathf.Sin(TAU * 90f * t) * 0.8f + Noise(r) * 0.5f) * Mathf.Exp(-t * 14f) : 0f;
                float clunk2 = t > 0.18f && t < 0.6f ? Mathf.Sin(TAU * 140f * (t - 0.18f)) * Mathf.Exp(-(t - 0.18f) * 18f) * 0.6f : 0f;
                float creak = 0f;
                if (t > 0.55f)
                {
                    float u = t - 0.55f;
                    float f = 380f + 120f * Mathf.Sin(TAU * 1.3f * u) + 40f * Noise(r);
                    lp += (Noise(r) - lp) * 0.3f;
                    creak = Mathf.Sin(TAU * f * u) * (0.25f + 0.2f * lp) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u / 1.6f));
                }
                b[i] = clunk + clunk2 + creak;
            }
            return Normalize(b, 0.85f);
        }

        static float[] WrongThud()
        {
            float sec = 0.5f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(12);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                b[i] = (Mathf.Sin(TAU * 70f * t) + 0.5f * Mathf.Sin(TAU * 73f * t) + Noise(r) * 0.2f) * Mathf.Exp(-t * 7f);
            }
            return Normalize(b, 0.75f);
        }

        // Rung nhà: tiếng ầm trầm + gỗ kẽo kẹt + bụi rơi
        static float[] Rumble(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(808);
            float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / sec)) * (0.6f + 0.4f * Mathf.PerlinNoise(t * 3f, 0.5f));
                float w = Noise(r); lp += (w - lp) * 0.01f; lp2 += (lp - lp2) * 0.02f;
                float sub = Mathf.Sin(TAU * 32f * t + Mathf.Sin(t * 5f)) * 0.5f;
                float debris = r.NextDouble() < 0.002 ? Noise(r) * 0.6f : 0f;
                b[i] = (lp2 * 30f + sub) * env + debris * env;
            }
            return Normalize(b, 0.9f);
        }

        // Xích đứt: 5 tiếng "băng" kim loại + leng keng
        static float[] ChainsBreak()
        {
            float sec = 3.2f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(55);
            for (int k = 0; k < 5; k++)
            {
                int s0 = (int)((0.15f + k * 0.38f + (float)r.NextDouble() * 0.1f) * SR);
                float f0 = 900f + (float)r.NextDouble() * 900f;
                for (int i = 0; i < SR * 1.2f && s0 + i < n; i++)
                {
                    float t = (float)i / SR;
                    float snap = Noise(r) * Mathf.Exp(-t * 90f) * 1.2f;
                    float ring = (Mathf.Sin(TAU * f0 * t) + 0.6f * Mathf.Sin(TAU * f0 * 2.71f * t) + 0.4f * Mathf.Sin(TAU * f0 * 4.13f * t)) * Mathf.Exp(-t * 6f) * 0.35f;
                    float rattle = (r.NextDouble() < 0.01 ? Noise(r) : 0f) * Mathf.Exp(-t * 3f);
                    b[s0 + i] += snap + ring + rattle;
                }
            }
            return Normalize(b, 0.9f);
        }

        static float[] CandleIgnite()
        {
            float sec = 1.4f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(66);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                lp += (Noise(r) - lp) * 0.08f;
                float whoosh = lp * 3f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.6f)) * (t < 0.6f ? 1f : 0f);
                float crackle = (r.NextDouble() < 0.003 ? Noise(r) : 0f) * Mathf.Exp(-t * 1.5f);
                float tone = Mathf.Sin(TAU * 220f * t) * 0.08f * Mathf.Exp(-t * 2f);
                b[i] = whoosh + crackle + tone;
            }
            return Normalize(b, 0.7f);
        }

        static float[] CandleOut()
        {
            float sec = 1.8f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(67);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                lp += (Noise(r) - lp) * 0.05f;
                float puff = lp * 4f * Mathf.Exp(-t * 9f);
                float moan = Mathf.Sin(TAU * (160f - 40f * t) * t) * 0.18f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 0.2f) / 1.5f)) * (t > 0.2f ? 1f : 0f);
                b[i] = puff + moan;
            }
            return Normalize(b, 0.7f);
        }

        // Nạy cửa sổ: gỗ nứt + kính rung
        static float[] WoodCrack()
        {
            float sec = 1.6f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(91);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float strain = Mathf.Sin(TAU * (200f + 300f * t) * t) * 0.2f * (t < 0.9f ? 1f : 0f);
                float crack = t > 0.9f ? Noise(r) * Mathf.Exp(-(t - 0.9f) * 25f) * 1.4f : 0f;
                float glass = t > 0.92f ? Mathf.Sin(TAU * 2600f * t) * Mathf.Exp(-(t - 0.92f) * 8f) * 0.2f : 0f;
                b[i] = strain + crack + glass;
            }
            return Normalize(b, 0.85f);
        }
    }
}
