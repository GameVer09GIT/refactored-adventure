using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KhoaHon.EditorTools
{
    /// <summary>
    /// Tổng hợp âm thanh gốc (không bản quyền) cho bà Quản Gia: chùm chìa khóa kéo lê, hát ru ngân "ầu ơ"
    /// (giai điệu ngũ cung tự viết), nhịp truy đuổi. Xuất WAV 44.1 kHz mono vào Assets/_KhoaHon/Audio.
    /// </summary>
    public static partial class KhoaHonAudioSynth
    {
        const int SR = 44100;
        public const string Dir = "Assets/_KhoaHon/Audio";

        [MenuItem("KhoaHon/Build/Audio (synth)")]
        public static void BuildAll()
        {
            KhoaHonDataBuilder.EnsureFolder(Dir);
            Write("hk_keys_drag_loop", Keys(6f, 7));
            Write("hk_lullaby_hum", Lullaby());
            Write("hk_hunt_pulse_loop", HuntPulse(8f));
            Write("hk_whisper_run", Whisper(1.6f));
            var extra = BuildHorror();
            extra.AddRange(BuildPuzzle());
            AssetDatabase.Refresh();
            var names = new System.Collections.Generic.List<string> { "hk_keys_drag_loop", "hk_lullaby_hum", "hk_hunt_pulse_loop", "hk_whisper_run" };
            names.AddRange(extra);
            foreach (var n in names)
            {
                var imp = AssetImporter.GetAtPath(Dir + "/" + n + ".wav") as AudioImporter;
                if (imp == null) continue;
                var s = imp.defaultSampleSettings;
                s.loadType = n.Contains("loop") ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.7f;
                imp.defaultSampleSettings = s; imp.forceToMono = true;
                imp.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- âm thanh
        static float[] Keys(float seconds, int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(SR * seconds);
            var b = new float[n];
            // Tiếng kim loại cọ sàn (nhiễu lọc thông dải, nhịp theo bước chân khập khiễng: nặng-nhẹ)
            float lp = 0, hp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float step = (t % 1.4f);
                float env = step < 0.55f ? Mathf.Exp(-step * 5f) * 0.9f : Mathf.Exp(-(step - 0.75f) * 7f) * (step > 0.75f ? 0.45f : 0f);
                float w = (float)(rng.NextDouble() * 2 - 1);
                lp += (w - lp) * 0.25f; hp = lp - hp * 0.995f;
                b[i] += hp * 0.09f * env;
            }
            // Leng keng: nhiều chìa khóa va nhau (sine giảm dần, tần số cao, hơi lệch)
            int hits = (int)(seconds * 11);
            for (int k = 0; k < hits; k++)
            {
                float start = (float)(rng.NextDouble() * seconds);
                float step = start % 1.4f;
                if (rng.NextDouble() > (step < 0.5f || (step > 0.75f && step < 1.0f) ? 0.95 : 0.25)) continue;
                float f = 2200f + (float)rng.NextDouble() * 4200f;
                float amp = 0.08f + (float)rng.NextDouble() * 0.12f;
                int s0 = (int)(start * SR);
                for (int i = 0; i < SR * 0.25f && s0 + i < n; i++)
                {
                    float t = (float)i / SR;
                    float e = Mathf.Exp(-t * (18f + f / 600f));
                    b[s0 + i] += amp * e * (Mathf.Sin(2 * Mathf.PI * f * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t));
                }
            }
            return LoopFade(Normalize(b, 0.7f), 0.05f);
        }

        static float[] Lullaby()
        {
            // Giai điệu ngũ cung tự viết (nốt MIDI, độ dài phách) — ngân "ầu ơ", chậm, run rẩy
            int[] notes = { 64, 67, 69, 67, 64, 62, 64, 0, 62, 64, 67, 64, 62, 60, 62, 0, 57, 60, 62, 60, 57, 55, 57 };
            float[] beats = { 1, 1, 2, 1, 1, 1, 3, 1, 1, 1, 2, 1, 1, 1, 3, 1, 1, 1, 2, 1, 1, 1, 4 };
            float beat = 0.62f;
            float total = 0; foreach (var x in beats) total += x * beat;
            int n = (int)(SR * (total + 1.5f));
            var b = new float[n];
            float tcur = 0.2f;
            var rng = new System.Random(3);
            for (int k = 0; k < notes.Length; k++)
            {
                float dur = beats[k] * beat;
                if (notes[k] > 0)
                {
                    float f0 = 440f * Mathf.Pow(2f, (notes[k] - 69) / 12f);
                    int s0 = (int)(tcur * SR), len = (int)((dur + 0.25f) * SR);
                    for (int i = 0; i < len && s0 + i < n; i++)
                    {
                        float t = (float)i / SR;
                        float vib = 1f + 0.012f * Mathf.Sin(2 * Mathf.PI * 5.2f * t + k);
                        float f = f0 * vib;
                        float att = Mathf.Clamp01(t / 0.12f), rel = Mathf.Clamp01((dur + 0.25f - t) / 0.3f);
                        float env = att * rel * (0.85f + 0.15f * Mathf.Sin(2 * Mathf.PI * 0.7f * t));
                        // Giọng ngân: hài bậc thấp + "formant" tối (hum)
                        float v = Mathf.Sin(2 * Mathf.PI * f * t) + 0.45f * Mathf.Sin(2 * Mathf.PI * 2 * f * t) + 0.18f * Mathf.Sin(2 * Mathf.PI * 3 * f * t) + 0.06f * Mathf.Sin(2 * Mathf.PI * 4 * f * t);
                        b[s0 + i] += v * env * 0.18f;
                    }
                }
                tcur += dur;
            }
            // Hơi thở/nhiễu phòng nhẹ + vang giả (delay)
            for (int i = 0; i < n; i++) b[i] += (float)(rng.NextDouble() * 2 - 1) * 0.004f;
            var outB = new float[n];
            int[] taps = { (int)(0.083f * SR), (int)(0.161f * SR), (int)(0.247f * SR), (int)(0.389f * SR) };
            float[] g = { 0.35f, 0.25f, 0.18f, 0.12f };
            for (int i = 0; i < n; i++) { float s = b[i]; for (int k = 0; k < taps.Length; k++) if (i - taps[k] >= 0) s += outB[i - taps[k]] * g[k] * 0.6f; outB[i] = s; }
            return Normalize(outB, 0.6f);
        }

        static float[] HuntPulse(float seconds)
        {
            int n = (int)(SR * seconds);
            var b = new float[n];
            var rng = new System.Random(11);
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                // Trống trầm dồn dập (tim đập nhanh) 132 bpm, mỗi phách 2 nhịp
                float bt = t % (60f / 132f);
                float kick = Mathf.Exp(-bt * 22f) * Mathf.Sin(2 * Mathf.PI * (48f + 60f * Mathf.Exp(-bt * 30f)) * bt);
                float bt2 = (t + 0.13f) % (60f / 132f);
                float kick2 = Mathf.Exp(-bt2 * 28f) * Mathf.Sin(2 * Mathf.PI * 42f * bt2) * 0.6f;
                // Drone nghịch tai (tritone) + tiếng rít
                float drone = 0.22f * Mathf.Sin(2 * Mathf.PI * 55f * t) + 0.16f * Mathf.Sin(2 * Mathf.PI * 77.8f * t + Mathf.Sin(t * 0.7f));
                float w = (float)(rng.NextDouble() * 2 - 1); lp += (w - lp) * 0.02f;
                float scrape = lp * 0.25f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.25f * t));
                b[i] = kick * 0.9f + kick2 + drone + scrape;
            }
            return LoopFade(Normalize(b, 0.75f), 0.08f);
        }

        static float[] Whisper(float seconds)
        {
            // Thì thầm (nhiễu lọc theo đường bao 2 âm tiết "chạy… đi…")
            int n = (int)(SR * seconds);
            var b = new float[n];
            var rng = new System.Random(21);
            float lp = 0, bp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float env = Bump(t, 0.15f, 0.55f) + 0.85f * Bump(t, 0.8f, 1.45f);
                float w = (float)(rng.NextDouble() * 2 - 1);
                lp += (w - lp) * 0.35f; bp += (lp - bp) * 0.08f;
                b[i] = (lp - bp) * env;
            }
            return Normalize(b, 0.55f);
        }

        static float Bump(float t, float a, float b) => t < a || t > b ? 0f : Mathf.Sin(Mathf.PI * (t - a) / (b - a));

        // ---------------------------------------------------------------- WAV
        internal static float[] Normalize(float[] b, float peak)
        {
            float m = 1e-6f; foreach (var x in b) m = Mathf.Max(m, Mathf.Abs(x));
            for (int i = 0; i < b.Length; i++) b[i] = b[i] / m * peak;
            return b;
        }

        internal static float[] LoopFade(float[] b, float sec)
        {
            int f = (int)(sec * SR);
            for (int i = 0; i < f; i++) { float a = (float)i / f; b[i] = b[i] * a + b[b.Length - f + i] * (1 - a); }
            Array.Resize(ref b, b.Length - f);
            return b;
        }

        internal static void Write(string name, float[] samples)
        {
            string path = Path.Combine(Application.dataPath, "../" + Dir + "/" + name + ".wav");
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int dataLen = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataLen); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataLen);
                foreach (var s in samples) w.Write((short)Mathf.Clamp(s * 32767f, -32768f, 32767f));
            }
        }
    }
}
