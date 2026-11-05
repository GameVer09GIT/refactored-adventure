using System.Collections.Generic;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Bộ âm thanh kinh dị tự tổng hợp (không bản quyền): nền nhà, hơi thở khò khè, tiếng thét, stinger phát hiện,
    /// gõ cửa, cót két, thì thầm, UI, nhạc truy đuổi v2, jumpscare, nhạc menu hộp nhạc ma.
    /// </summary>
    public static partial class KhoaHonAudioSynth
    {
        const float TAU = Mathf.PI * 2f;

        static List<string> BuildHorror()
        {
            var made = new List<string>();
            void W(string n, float[] d) { Write(n, d); made.Add(n); }
            W("amb_house_loop", HouseAmbience(24f));
            W("hk_breath_loop", RaspBreath(4.2f));
            W("hk_scream", Scream(1.9f, 5));
            W("sfx_stinger_detect", Stinger());
            W("sfx_scare_knock", Knocks());
            W("sfx_scare_creak", Creak(1.8f));
            W("sfx_scare_whispers", Whispers(3.2f));
            W("sfx_scare_bang", Bang());
            W("ui_hover", UiTick(0.07f, 1900f, 0.35f));
            W("ui_click", UiTick(0.18f, 420f, 0.8f));
            W("ui_back", UiTick(0.16f, 260f, 0.7f));
            W("hk_hunt_v2_loop", HuntV2(9.6f));
            W("sfx_jumpscare", Jumpscare());
            W("mus_menu_musicbox_loop", MusicBox());
            W("sfx_heartbeat_fast", Heartbeat(4f, 150f));
            return made;
        }

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2 - 1);

        // Nền nhà: drone trầm phách, gió luồn, gỗ cót két xa, kim loại rên
        static float[] HouseAmbience(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(101);
            float lp = 0, lp2 = 0, bp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float drone = 0.32f * Mathf.Sin(TAU * 38f * t) + 0.22f * Mathf.Sin(TAU * 38.7f * t) + 0.12f * Mathf.Sin(TAU * 57.3f * t + Mathf.Sin(t * 0.3f) * 2f);
                float w = Noise(r); lp += (w - lp) * 0.012f; lp2 += (lp - lp2) * 0.05f; bp = lp - lp2;
                float gust = 0.35f + 0.65f * Mathf.Pow(Mathf.Sin(TAU * t / sec * 3f + Mathf.Sin(t * 0.7f)) * 0.5f + 0.5f, 3f);
                b[i] = drone * 0.5f + bp * 6f * gust;
            }
            // Cót két / rên kim loại rải rác
            for (int k = 0; k < 7; k++)
            {
                float st = (float)(r.NextDouble() * (sec - 2f)); int s0 = (int)(st * SR);
                bool metal = k % 3 == 0;
                float f0 = metal ? 110f + (float)r.NextDouble() * 80f : 520f + (float)r.NextDouble() * 500f;
                float dur = metal ? 1.8f : 0.9f;
                for (int i = 0; i < SR * dur && s0 + i < n; i++)
                {
                    float t = (float)i / SR, e = Mathf.Sin(Mathf.PI * t / dur);
                    float f = f0 * (1f + 0.25f * Mathf.Sin(TAU * 0.6f * t)) * (metal ? 1f : (1f + 0.4f * t));
                    float v = metal ? Mathf.Sin(TAU * f * t) * 0.18f + Mathf.Sin(TAU * f * 2.41f * t) * 0.08f
                                    : (Mathf.Repeat(f * t, 1f) - 0.5f) * 0.12f * (0.5f + 0.5f * Mathf.Sin(TAU * 31f * t));
                    b[s0 + i] += v * e * 0.6f;
                }
            }
            return LoopFade(Normalize(b, 0.55f), 1.0f);
        }

        // Hơi thở khò khè: hít vào (cao) – thở ra (trầm, ọc ọc)
        static float[] RaspBreath(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(7);
            float l1 = 0, l2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, ph = t % 2.1f;
                bool inhale = ph < 0.8f;
                float env = inhale ? Mathf.Sin(Mathf.PI * ph / 0.8f) * 0.6f : (ph > 0.95f && ph < 2.0f ? Mathf.Sin(Mathf.PI * (ph - 0.95f) / 1.05f) : 0f);
                float w = Noise(r);
                float a = inhale ? 0.3f : 0.12f;
                l1 += (w - l1) * a; l2 += (l1 - l2) * (inhale ? 0.12f : 0.04f);
                float rasp = (l1 - l2);
                float gurgle = inhale ? 1f : (0.55f + 0.45f * Mathf.Sin(TAU * 27f * t) * Mathf.Sin(TAU * 3.1f * t));
                float voice = inhale ? 0f : 0.25f * Mathf.Sin(TAU * (92f + 8f * Mathf.Sin(t * 5f)) * t);
                b[i] = (rasp * 2.2f * gurgle + voice * 0.4f) * env;
            }
            return LoopFade(Normalize(b, 0.7f), 0.08f);
        }

        // Thét: nhiều sóng răng cưa lệch tông quét lên rồi gãy, điều chế vòng + méo cứng
        static float[] Scream(float sec, int seed)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(seed);
            float[] det = { 1f, 1.013f, 0.987f, 1.5f, 0.749f };
            float[] ph = new float[det.Length];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float sweep = t < 0.25f ? Mathf.Lerp(500f, 1250f, t / 0.25f) : 1250f + 180f * Mathf.Sin(TAU * 6.5f * t) - 260f * Mathf.Max(0, t - 1.1f);
                float v = 0;
                for (int k = 0; k < det.Length; k++) { ph[k] += sweep * det[k] / SR; v += (Mathf.Repeat(ph[k], 1f) * 2 - 1) * (k < 3 ? 0.3f : 0.15f); }
                v *= 1f + 0.6f * Mathf.Sin(TAU * 71f * t);
                float w = Noise(r); lp += (w - lp) * 0.5f;
                v += lp * 0.35f;
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((sec - t) / 0.5f);
                b[i] = (float)System.Math.Tanh(v * 2.6f) * env;
            }
            return Normalize(b, 0.95f);
        }

        // Stinger: tiếng rít hút vào 0.45 s → đòn dàn dây nghịch tai + trống trầm
        static float[] Stinger()
        {
            float sec = 2.8f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(13);
            float[] fr = { 110f, 116.5f, 155.6f, 220f, 233f, 311f, 466f };
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float v = 0;
                if (t < 0.45f) { float w = Noise(r); lp += (w - lp) * (0.02f + t * 0.8f); v += lp * Mathf.Pow(t / 0.45f, 3f) * 1.4f; }
                float th = t - 0.45f;
                if (th >= 0)
                {
                    float env = Mathf.Exp(-th * 1.3f);
                    foreach (var f in fr) v += (Mathf.Repeat(f * th, 1f) * 2 - 1) * 0.12f * env;
                    v += Mathf.Sin(TAU * (38f + 70f * Mathf.Exp(-th * 9f)) * th) * Mathf.Exp(-th * 3.5f) * 1.2f;
                    v += Noise(r) * Mathf.Exp(-th * 14f) * 0.5f;
                }
                b[i] = (float)System.Math.Tanh(v * 1.6f);
            }
            return Normalize(b, 0.9f);
        }

        static float[] Knocks()
        {
            float sec = 1.6f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(3);
            foreach (var st in new[] { 0.05f, 0.42f, 0.71f })
            {
                int s0 = (int)(st * SR);
                for (int i = 0; i < SR * 0.4f && s0 + i < n; i++)
                {
                    float t = (float)i / SR;
                    b[s0 + i] += (Mathf.Sin(TAU * 105f * t) * 0.8f + Mathf.Sin(TAU * 172f * t) * 0.4f) * Mathf.Exp(-t * 16f) + Noise(r) * Mathf.Exp(-t * 90f) * 0.6f;
                }
            }
            return Normalize(b, 0.85f);
        }

        static float[] Creak(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; float ph = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float rate = 18f + 60f * Mathf.Sin(Mathf.PI * t / sec) + 15f * Mathf.Sin(TAU * 2.3f * t);
                ph += rate / SR; float pulse = Mathf.Repeat(ph, 1f) < 0.08f ? 1f : 0f;
                // Lọc cộng hưởng ~760 Hz
                float f = 760f + 120f * Mathf.Sin(TAU * 0.9f * t), q = 0.985f, w0 = TAU * f / SR;
                float y = pulse + 2f * q * Mathf.Cos(w0) * y1 - q * q * y2; y2 = y1; y1 = y;
                b[i] = y * Mathf.Sin(Mathf.PI * t / sec);
            }
            return Normalize(b, 0.7f);
        }

        static float[] Whispers(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(17);
            for (int v = 0; v < 4; v++)
            {
                float l = 0, l2 = 0, off = v * 0.37f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR + off;
                    float env = Mathf.Max(0, Mathf.Sin(TAU * (1.7f + v * 0.4f) * t)) * Mathf.Max(0, Mathf.Sin(TAU * 0.31f * t + v));
                    float w = Noise(r); l += (w - l) * (0.25f + v * 0.05f); l2 += (l - l2) * 0.06f;
                    b[i] += (l - l2) * env * 0.6f;
                }
            }
            return Normalize(b, 0.6f);
        }

        static float[] Bang()
        {
            float sec = 1.4f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(23); float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR; float w = Noise(r); lp += (w - lp) * 0.08f;
                b[i] = Mathf.Sin(TAU * (55f + 40f * Mathf.Exp(-t * 20f)) * t) * Mathf.Exp(-t * 4f) + lp * 3f * Mathf.Exp(-t * 7f) + w * Mathf.Exp(-t * 60f) * 0.5f;
            }
            return Normalize(b, 0.95f);
        }

        static float[] UiTick(float sec, float f, float body)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(5);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                b[i] = Mathf.Sin(TAU * f * t) * Mathf.Exp(-t * (40f / sec / 10f)) * body + Noise(r) * Mathf.Exp(-t * 400f) * 0.3f + Mathf.Sin(TAU * f * 0.5f * t) * Mathf.Exp(-t * 30f) * 0.3f;
            }
            return Normalize(b, 0.5f);
        }

        // Truy đuổi v2: 152 bpm, trống nặng, dây vĩ tremolo rít cao, kèn nghịch tai
        static float[] HuntV2(float sec)
        {
            int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(31);
            float beat = 60f / 152f; float[] st = { 1760f, 1864.7f, 2093f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, bt = t % beat, bt2 = (t + beat * 0.5f) % beat;
                float kick = Mathf.Sin(TAU * (45f + 90f * Mathf.Exp(-bt * 35f)) * bt) * Mathf.Exp(-bt * 9f) * 1.1f;
                float off = Mathf.Sin(TAU * 60f * bt2) * Mathf.Exp(-bt2 * 22f) * 0.45f + Noise(r) * Mathf.Exp(-bt2 * 50f) * 0.25f;
                float trem = 0.5f + 0.5f * Mathf.Sin(TAU * 13f * t);
                float strings = 0; foreach (var f in st) strings += (Mathf.Repeat(f * t, 1f) * 2 - 1) * 0.05f;
                float bar = (t % (beat * 4f)) / (beat * 4f);
                float brass = ((Mathf.Repeat(73.4f * t, 1f) * 2 - 1) + (Mathf.Repeat(77.8f * t, 1f) * 2 - 1)) * 0.12f * (bar < 0.5f ? 1f : 0.4f);
                float drone = Mathf.Sin(TAU * 36.7f * t) * 0.35f;
                b[i] = (float)System.Math.Tanh((kick + off + strings * trem + brass + drone) * 1.3f);
            }
            return LoopFade(Normalize(b, 0.85f), 0.05f);
        }

        static float[] Jumpscare()
        {
            var s = Scream(1.6f, 41); var st = Stinger(); int n = Mathf.Max(s.Length, (int)(SR * 1.6f));
            var b = new float[n]; var r = new System.Random(43);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float v = (i < s.Length ? s[i] : 0) * 0.9f;
                int j = i + (int)(0.45f * SR); if (j < st.Length) v += st[j] * 0.7f;
                v += Noise(r) * Mathf.Exp(-t * 6f) * 0.5f;
                b[i] = (float)System.Math.Tanh(v * 1.8f);
            }
            return Normalize(b, 1.0f);
        }

        // Nhạc menu: hộp nhạc lệch tông chơi giai điệu ru tự viết, trên nền drone + hơi thở
        static float[] MusicBox()
        {
            int[] notes = { 76, 79, 81, 79, 76, 74, 76, 0, 74, 76, 79, 76, 74, 72, 74, 0, 69, 72, 74, 72, 69, 67, 69, 0 };
            float beat = 0.72f; int len = notes.Length;
            float sec = len * beat + 2f; int n = (int)(SR * sec); var b = new float[n]; var r = new System.Random(77);
            for (int k = 0; k < len; k++)
            {
                if (notes[k] == 0) continue;
                float f0 = 440f * Mathf.Pow(2f, (notes[k] - 69) / 12f) * (1f + ((k * 7) % 5 - 2) * 0.004f);
                int s0 = (int)(k * beat * SR);
                for (int i = 0; i < SR * 2.2f && s0 + i < n; i++)
                {
                    float t = (float)i / SR;
                    float tine = Mathf.Sin(TAU * f0 * t) + 0.35f * Mathf.Sin(TAU * f0 * 2.98f * t) * Mathf.Exp(-t * 6f) + 0.15f * Mathf.Sin(TAU * f0 * 5.4f * t) * Mathf.Exp(-t * 12f);
                    b[s0 + i] += tine * Mathf.Exp(-t * 2.2f) * 0.22f;
                }
            }
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR; float w = Noise(r); lp += (w - lp) * 0.01f;
                b[i] += 0.13f * Mathf.Sin(TAU * 55f * t) * (0.6f + 0.4f * Mathf.Sin(TAU * 0.07f * t)) + lp * 0.6f;
            }
            // Vang giả
            var o = new float[n]; int[] taps = { (int)(0.11f * SR), (int)(0.23f * SR), (int)(0.37f * SR) };
            for (int i = 0; i < n; i++) { float v = b[i]; foreach (var tp in taps) if (i - tp >= 0) v += o[i - tp] * 0.28f; o[i] = v; }
            return LoopFade(Normalize(o, 0.55f), 1.2f);
        }

        static float[] Heartbeat(float sec, float bpm)
        {
            int n = (int)(SR * sec); var b = new float[n]; float per = 60f / bpm;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, p = t % per;
                float lub = Mathf.Sin(TAU * 52f * p) * Mathf.Exp(-p * 28f);
                float p2 = p - 0.16f; float dub = p2 > 0 ? Mathf.Sin(TAU * 64f * p2) * Mathf.Exp(-p2 * 34f) * 0.7f : 0f;
                b[i] = lub + dub;
            }
            return LoopFade(Normalize(b, 0.9f), 0.02f);
        }
    }
}
