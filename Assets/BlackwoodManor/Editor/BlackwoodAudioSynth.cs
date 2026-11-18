using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Tổng hợp âm thanh gốc (không bản quyền) cho bà Quản Gia: chùm chìa khóa kéo lê, hát ru ngân "ầu ơ"
    /// (giai điệu ngũ cung tự viết), nhịp truy đuổi. Xuất WAV 44.1 kHz mono vào Assets/BlackwoodManor/Audio.
    /// </summary>
    public static partial class BlackwoodAudioSynth
    {
        const int SR = 44100;
        public const string Dir = "Assets/BlackwoodManor/Audio";

        [MenuItem("Blackwood/Build/Audio (synth)")]
        public static void BuildAll()
        {
            BlackwoodDataBuilder.EnsureFolder(Dir);
            Write("hk_keys_drag_loop", Keys(6f, 7));
            Write("hk_lullaby_hum", Lullaby());
            Write("hk_hunt_pulse_loop", HuntPulse(8f));
            Write("hk_whisper_run", Whisper(1.6f));
            var extra = BuildHorror();
            extra.AddRange(BuildPuzzle());
            extra.AddRange(BuildCombat());
            extra.AddRange(BuildAtmos());
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
                imp.defaultSampleSettings = s; imp.forceToMono = !StereoNames.Contains(n);
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
                float f = 1500f + (float)rng.NextDouble() * 2600f;
                float amp = 0.08f + (float)rng.NextDouble() * 0.12f;
                int s0 = (int)(start * SR);
                for (int i = 0; i < SR * 0.25f && s0 + i < n; i++)
                {
                    float t = (float)i / SR;
                    float e = Mathf.Exp(-t * (18f + f / 600f));
                    b[s0 + i] += amp * e * (Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t));
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

        /// <summary>Tầng master trước khi ghi file: khử lệch DC rồi giới hạn đỉnh ở −1 dBFS (không bao giờ cắt đỉnh 16-bit).</summary>
        internal static void Master(params float[][] channels)
        {
            const float ceiling = 0.891f; // −1 dBFS
            float peak = 1e-6f;
            foreach (var ch in channels)
            {
                if (ch == null || ch.Length == 0) continue;
                double mean = 0; foreach (var x in ch) mean += x; mean /= ch.Length;
                for (int i = 0; i < ch.Length; i++) { ch[i] -= (float)mean; peak = Mathf.Max(peak, Mathf.Abs(ch[i])); }
            }
            if (peak <= ceiling) return;
            float g = ceiling / peak;
            foreach (var ch in channels) if (ch != null) for (int i = 0; i < ch.Length; i++) ch[i] *= g;
        }

        /// <summary>Trần độ to tức thời (cửa sổ 400 ms, trọng số K xấp xỉ) cho các stinger quá gắt — dB LUFS.</summary>
        static readonly System.Collections.Generic.Dictionary<string, float> LoudCap = new System.Collections.Generic.Dictionary<string, float>
        {
            { "sfx_jumpscare", -7f }, { "hk_scream", -7.5f }, { "hk_blind_shriek", -8.5f }, { "sfx_brake_skid", -9f },
        };

        /// <summary>
        /// Hoàn thiện trước master (soát bằng phổ + đo độ to): lọc thông cao 24 Hz bỏ hạ âm vô ích (vòng lặp lọc vòng tròn để
        /// mối nối không giật); âm một lần thì vào 1 ms + tắt dần 8 ms (hết tiếng "tách" ở đầu/cuối); stinger gắt thì hạ về trần.
        /// </summary>
        internal static void Polish(string name, float[] b)
        {
            bool loop = name.Contains("loop");
            HighPass(b, 24f, loop);
            if (!loop)
            {
                int a = Mathf.Min(b.Length / 4, (int)(0.001f * SR)), z = Mathf.Min(b.Length / 4, (int)(0.008f * SR));
                for (int i = 0; i < a; i++) b[i] *= (float)i / a;
                for (int i = 0; i < z; i++) b[b.Length - 1 - i] *= (float)i / z;
            }
            if (LoudCap.TryGetValue(name, out float cap))
            {
                float m = MomentaryMax(b);
                if (m > cap) { float g = Mathf.Pow(10f, (cap - m) / 20f); for (int i = 0; i < b.Length; i++) b[i] *= g; }
            }
        }

        /// <summary>Lọc thông cao bậc 2 (Butterworth). Vòng lặp: chạy 2 vòng để trạng thái lọc khớp đầu-cuối.</summary>
        internal static void HighPass(float[] b, float fc, bool circular)
        {
            double w0 = 2 * System.Math.PI * fc / SR, cs = System.Math.Cos(w0), al = System.Math.Sin(w0) / (2 * 0.7071);
            double a0 = 1 + al, b0 = (1 + cs) / 2 / a0, b1 = -(1 + cs) / a0, b2 = b0, a1 = -2 * cs / a0, a2 = (1 - al) / a0;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            int passes = circular ? 2 : 1;
            for (int p = 0; p < passes; p++)
                for (int i = 0; i < b.Length; i++)
                {
                    double x = b[i], y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                    x2 = x1; x1 = x; y2 = y1; y1 = y;
                    if (p == passes - 1) b[i] = (float)y;
                }
        }

        /// <summary>Độ to tức thời lớn nhất (≈ LUFS, cửa sổ 400 ms, nhấn ~ +4 dB trên 2 kHz như trọng số K).</summary>
        static float MomentaryMax(float[] b)
        {
            var k = (float[])b.Clone();
            HighPass(k, 60f, false);
            // kệ cao: cộng thêm thành phần thông cao 2 kHz ≈ +4 dB
            var hi = (float[])k.Clone(); HighPass(hi, 2000f, false);
            for (int i = 0; i < k.Length; i++) k[i] += hi[i] * 0.58f;
            int win = (int)(0.4f * SR), hop = (int)(0.1f * SR);
            double best = 1e-12;
            for (int s = 0; s + win <= k.Length || s == 0; s += hop)
            {
                double sum = 0; int e = Mathf.Min(k.Length, s + win);
                for (int i = s; i < e; i++) sum += k[i] * k[i];
                best = System.Math.Max(best, sum / Mathf.Max(1, e - s));
                if (e >= k.Length) break;
            }
            return (float)(-0.691 + 10 * System.Math.Log10(best));
        }

        internal static void Write(string name, float[] samples)
        {
            Polish(name, samples);
            Master(samples);
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
