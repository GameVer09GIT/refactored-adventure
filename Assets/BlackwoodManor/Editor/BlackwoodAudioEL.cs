using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Blackwood.EditorTools
{
    /// <summary>
    /// Âm thanh tạo bằng ElevenLabs (hiệu ứng, nhạc, giọng nói). File gốc (.mp3) để ở Audio/EL/Raw — không được game tham chiếu
    /// nên không vào bản build. Công cụ này giải mã bằng Unity rồi ghi bản đã xử lý ra Audio/EL/&lt;tên&gt;.wav:
    ///  • âm một lần: cắt khoảng lặng/độ trễ bộ mã hóa ở đầu, tắt dần cuối;
    ///  • vòng lặp: đan chéo đầu–cuối (nhạc 2,5 s, hiệu ứng 0,15 s) nên lặp không giật;
    ///  • độ to khớp với file tổng hợp cùng tên cũ (giữ nguyên cân bằng âm lượng đã chỉnh trong game), trần −1 dBFS.
    /// <see cref="Load"/> ưu tiên bản ElevenLabs, chưa có thì dùng bản tổng hợp.
    /// </summary>
    public static partial class BlackwoodAudioSynth
    {
        public const string ElDir = "Assets/BlackwoodManor/Audio/EL";
        public const string ElRawDir = ElDir + "/Raw";

        /// <summary>Nạp clip theo tên: Audio/EL/&lt;tên&gt;.wav nếu có, không thì Audio/&lt;tên&gt;.wav (bản tổng hợp).</summary>
        public static AudioClip Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ElDir + "/" + name + ".wav")
                   ?? AssetDatabase.LoadAssetAtPath<AudioClip>(Dir + "/" + name + ".wav");
        }

        [MenuItem("Blackwood/Build/Audio (ElevenLabs import)")]
        public static void ImportElMenu() => Debug.Log(ImportElevenLabs());

        public static string ImportElevenLabs()
        {
            if (!Directory.Exists(ElRawDir)) return "EL: chưa có " + ElRawDir;
            BlackwoodDataBuilder.EnsureFolder(ElDir);
            var log = new List<string>();
            foreach (var path in Directory.GetFiles(ElRawDir, "*.mp3"))
            {
                string raw = path.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(raw);
                try { log.Add(ProcessOne(raw, name)); }
                catch (Exception e) { log.Add("EL " + name + ": LỖI " + e.Message); }
            }
            AssetDatabase.Refresh();
            // Thiết lập nhập cho bản đã xử lý (giống bộ tổng hợp: vòng lặp giải nén sẵn, còn lại nén trong bộ nhớ)
            foreach (var path in Directory.GetFiles(ElDir, "*.wav", SearchOption.AllDirectories))
            {
                string p = path.Replace('\\', '/');
                var imp = AssetImporter.GetAtPath(p) as AudioImporter;
                if (imp == null) continue;
                string n = Path.GetFileNameWithoutExtension(p);
                var s = imp.defaultSampleSettings;
                bool longClip = n.StartsWith("mus_") || n.StartsWith("amb_");
                s.loadType = longClip ? AudioClipLoadType.CompressedInMemory : n.Contains("loop") ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = longClip ? 0.6f : 0.75f;
                imp.defaultSampleSettings = s;
                imp.forceToMono = !(longClip || StereoNames.Contains(n));
                imp.SaveAndReimport();
            }
            return "EL import: " + log.Count + " file\n" + string.Join("\n", log);
        }

        static string ProcessOne(string rawPath, string name)
        {
            // Giải mã: PCM, giải nén khi nạp (GetData cần dữ liệu không nén)
            var imp = (AudioImporter)AssetImporter.GetAtPath(rawPath);
            var st = imp.defaultSampleSettings;
            if (st.loadType != AudioClipLoadType.DecompressOnLoad || st.compressionFormat != AudioCompressionFormat.PCM || imp.loadInBackground)
            {
                st.loadType = AudioClipLoadType.DecompressOnLoad; st.compressionFormat = AudioCompressionFormat.PCM; st.preloadAudioData = true;
                imp.defaultSampleSettings = st; imp.loadInBackground = false; imp.forceToMono = false;
                imp.SaveAndReimport();
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(rawPath);
            if (clip == null) return "EL " + name + ": không nạp được";
            clip.LoadAudioData();
            int ch = clip.channels, n = clip.samples;
            var inter = new float[n * ch];
            if (!clip.GetData(inter, 0)) return "EL " + name + ": GetData thất bại";
            var chans = new float[ch][];
            for (int c = 0; c < ch; c++) { chans[c] = new float[n]; for (int i = 0; i < n; i++) chans[c][i] = inter[i * ch + c]; }
            if (clip.frequency != SR) for (int c = 0; c < ch; c++) chans[c] = Resample(chans[c], clip.frequency, SR);

            // Tên đích: "hk_step_v2" → hk_step_02 (biến thể bước chân)
            string outName = name;
            bool loop = outName.Contains("loop");
            bool music = outName.StartsWith("mus_") || outName == "hk_hunt_v2_loop";

            if (!loop)
            {
                // Cắt khoảng lặng đầu (−48 dB so với đỉnh), giữ 4 ms trước tiếng đầu tiên; cắt đuôi lặng
                float pk = 1e-6f; foreach (var a in chans) foreach (var x in a) pk = Mathf.Max(pk, Mathf.Abs(x));
                float thr = pk * 0.004f;
                int first = 0, last = chans[0].Length - 1;
                while (first < last && Silent(chans, first, thr)) first++;
                while (last > first && Silent(chans, last, thr * 0.5f)) last--;
                first = Mathf.Max(0, first - (int)(0.004f * SR));
                last = Mathf.Min(chans[0].Length - 1, last + (int)(0.05f * SR));
                for (int c = 0; c < ch; c++) { var seg = new float[last - first + 1]; Array.Copy(chans[c], first, seg, 0, seg.Length); chans[c] = seg; }
            }
            else
            {
                float xf = music ? 2.5f : 0.15f;
                xf = Mathf.Min(xf, chans[0].Length / (float)SR * 0.2f);
                for (int c = 0; c < ch; c++) chans[c] = LoopFade(chans[c], xf);
            }
            // Thông cao 24 Hz + mép mềm (như bộ tổng hợp)
            for (int c = 0; c < ch; c++)
            {
                HighPass(chans[c], 24f, loop);
                if (!loop)
                {
                    var b = chans[c];
                    int a = Mathf.Min(b.Length / 4, (int)(0.001f * SR)), z = Mathf.Min(b.Length / 4, (int)(0.01f * SR));
                    for (int i = 0; i < a; i++) b[i] *= (float)i / a;
                    for (int i = 0; i < z; i++) b[b.Length - 1 - i] *= (float)i / z;
                }
            }
            // Độ to: khớp bản tổng hợp cũ cùng tên (nếu có) để cân bằng âm lượng trong game giữ nguyên
            string refName = System.Text.RegularExpressions.Regex.Replace(outName, "_0[1-4]$", "_01");
            var reference = ReadWav(Path.Combine(Application.dataPath, "../" + Dir + "/" + outName + ".wav")) ?? ReadWav(Path.Combine(Application.dataPath, "../" + Dir + "/" + refName + ".wav"));
            float target = reference != null ? ActiveLoudness(reference) : (music ? -18f : -16f);
            if (LoudCap.TryGetValue(outName, out float cap)) target = Mathf.Min(target, cap - 2f);
            float now = ActiveLoudness(MonoMix(chans));
            float gain = Mathf.Pow(10f, (target - now) / 20f);
            float peak = 1e-6f;
            foreach (var a in chans) foreach (var x in a) peak = Mathf.Max(peak, Mathf.Abs(x));
            // Âm có đỉnh nhọn (tiếng thét, xèo, leng keng) không lên đủ độ to nếu chỉ chặn đỉnh → cho vượt tối đa 9 dB rồi nén mềm (tanh)
            float headroom = 0.891f / peak;
            if (gain > headroom)
            {
                gain = Mathf.Min(gain, headroom * (LoudCap.ContainsKey(outName) ? 4.5f : 2.8f)); // âm hù dọa được nén mạnh hơn
                foreach (var a in chans) for (int i = 0; i < a.Length; i++) a[i] = 0.891f * (float)Math.Tanh(a[i] * gain / 0.891f);
            }
            else foreach (var a in chans) for (int i = 0; i < a.Length; i++) a[i] *= gain;
            // Đo lại sau khi nén (tanh làm đổi độ to) và bù tuyến tính, không vượt trần đỉnh
            for (int pass = 0; pass < 2; pass++)
            {
                float got = ActiveLoudness(MonoMix(chans));
                float fix = Mathf.Pow(10f, (target - got) / 20f);
                float pk2 = 1e-6f; foreach (var a in chans) foreach (var x in a) pk2 = Mathf.Max(pk2, Mathf.Abs(x));
                fix = Mathf.Min(fix, 0.891f / pk2);
                if (Mathf.Abs(fix - 1f) < 0.01f) break;
                foreach (var a in chans) for (int i = 0; i < a.Length; i++) a[i] *= fix;
            }

            WriteMulti(ElDir + "/" + outName + ".wav", chans);
            return $"EL {outName}: {ch}ch {chans[0].Length / (float)SR:F2}s, độ to {now:F1} → {target:F1} dB" + (reference == null ? " (không có bản cũ để khớp)" : "");
        }

        static bool Silent(float[][] chans, int i, float thr)
        {
            foreach (var a in chans) if (Mathf.Abs(a[i]) > thr) return false;
            return true;
        }

        static float[] MonoMix(float[][] chans)
        {
            var m = new float[chans[0].Length];
            foreach (var a in chans) for (int i = 0; i < m.Length; i++) m[i] += a[i] / chans.Length;
            return m;
        }

        static float[] Resample(float[] x, int from, int to)
        {
            int n = (int)((long)x.Length * to / from);
            var y = new float[n];
            for (int i = 0; i < n; i++)
            {
                double p = (double)i * from / to; int j = (int)p; float f = (float)(p - j);
                y[i] = j + 1 < x.Length ? x[j] * (1 - f) + x[j + 1] * f : x[Mathf.Min(j, x.Length - 1)];
            }
            return y;
        }

        /// <summary>Độ to (≈ LUFS) của phần có tiếng: trung bình năng lượng các cửa sổ 400 ms trong vòng 15 dB dưới cửa sổ to nhất.</summary>
        static float ActiveLoudness(float[] b)
        {
            // ITU-R BS.1770: lọc K hai tầng, khối 400 ms bước 100 ms, cổng tuyệt đối −70 LUFS + cổng tương đối −10 LU
            double[] B1 = { 1.53512485958697, -2.69169618940638, 1.19839281085285 }, A1 = { 1.0, -1.69065929318241, 0.73248077421585 };
            double[] B2 = { 1.0, -2.0, 1.0 }, A2 = { 1.0, -1.99004745483398, 0.99007225036621 };
            var k = new double[b.Length];
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0, u1 = 0, u2 = 0, v1 = 0, v2 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                double x = b[i], y = B1[0] * x + B1[1] * x1 + B1[2] * x2 - A1[1] * y1 - A1[2] * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                double v = B2[0] * y + B2[1] * u1 + B2[2] * u2 - A2[1] * v1 - A2[2] * v2;
                u2 = u1; u1 = y; v2 = v1; v1 = v;
                k[i] = v;
            }
            int win = (int)(0.4f * SR), hop = (int)(0.1f * SR);
            var ls = new List<double>();
            if (k.Length < win) { double s0 = 0; foreach (var v in k) s0 += v * v; return (float)(-0.691 + 10 * Math.Log10(Math.Max(1e-12, s0 / Math.Max(1, k.Length)))); }
            for (int s = 0; s + win <= k.Length; s += hop)
            {
                double sum = 0; for (int i = s; i < s + win; i++) sum += k[i] * k[i];
                ls.Add(-0.691 + 10 * Math.Log10(Math.Max(1e-12, sum / win)));
            }
            var g = ls.FindAll(l => l > -70);
            if (g.Count == 0) return -70f;
            double mean = 0; foreach (var l in g) mean += Math.Pow(10, l / 10); mean /= g.Count;
            double rel = 10 * Math.Log10(mean) - 10, acc = 0; int cnt = 0;
            foreach (var l in g) if (l > rel) { acc += Math.Pow(10, l / 10); cnt++; }
            return (float)(10 * Math.Log10(acc / Math.Max(1, cnt)));
        }

        /// <summary>Đọc WAV PCM 16-bit (mono hoặc stereo → trộn mono). Không có file thì trả null.</summary>
        static float[] ReadWav(string path)
        {
            if (!File.Exists(path)) return null;
            var d = File.ReadAllBytes(path);
            int ch = BitConverter.ToInt16(d, 22), bits = BitConverter.ToInt16(d, 34);
            if (bits != 16) return null;
            int p = 12;
            while (p + 8 <= d.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(d, p, 4); int len = BitConverter.ToInt32(d, p + 4);
                if (id == "data")
                {
                    int frames = len / (2 * ch); var m = new float[frames];
                    for (int i = 0; i < frames; i++)
                    {
                        float s = 0; for (int c = 0; c < ch; c++) s += BitConverter.ToInt16(d, p + 8 + (i * ch + c) * 2) / 32768f;
                        m[i] = s / ch;
                    }
                    return m;
                }
                p += 8 + len + (len & 1);
            }
            return null;
        }

        static void WriteMulti(string assetPath, float[][] chans)
        {
            int ch = chans.Length, n = chans[0].Length;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int dataLen = n * ch * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataLen); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)ch);
                w.Write(SR); w.Write(SR * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataLen);
                for (int i = 0; i < n; i++) for (int c = 0; c < ch; c++) w.Write((short)Mathf.Clamp(chans[c][i] * 32767f, -32768f, 32767f));
                w.Flush();
                BlackwoodDataBuilder.WriteBytesIfChanged(assetPath, ms.ToArray());
            }
        }
    }
}
