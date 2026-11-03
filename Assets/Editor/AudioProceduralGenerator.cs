using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HorrorAudio.Editor
{
    public struct ClipMetric
    {
        public string FileName;
        public string RelativePath;
        public float Duration;
        public float Peak;
        public float Rms;
        public string Source;
    }

    /// <summary>
    /// Editor tool tự động tổng hợp âm thanh thủ tục (Procedural DSP) xuất ra file WAV chuẩn 16-bit PCM Mono 44.1kHz.
    /// Đảm bảo seed cố định, fade-in/out 5ms chống click, peak chuẩn hóa ~0.70 và cấu hình AudioImporter tối ưu.
    /// </summary>
    public static class AudioProceduralGenerator
    {
        public const int SampleRate = 44100;

        [MenuItem("Tools/Audio/Generate Sounds", false, 1)]
        public static void GenerateSoundsMenu()
        {
            List<ClipMetric> metrics = GenerateAllSounds(true);
            PrintMetricsReport(metrics);
            EditorUtility.DisplayDialog("Audio Generator", $"Đã tạo thành công {metrics.Count} file âm thanh thủ tục trong thư mục Assets/Audio/!\nChi tiết số liệu đã được in ra Unity Console.", "OK");
        }

        /// <summary>
        /// Tạo toàn bộ file âm thanh thủ tục nếu chưa có hoặc khi forceRegenerate = true
        /// </summary>
        public static List<ClipMetric> GenerateAllSounds(bool forceRegenerate = false)
        {
            var metrics = new List<ClipMetric>();

            string baseDir = Path.Combine(Application.dataPath, "Audio");
            string dirtDir = Path.Combine(baseDir, "Footsteps/Dirt");
            string woodDir = Path.Combine(baseDir, "Footsteps/Wood");
            string breathDir = Path.Combine(baseDir, "Breathing");
            string heartDir = Path.Combine(baseDir, "Heartbeat");

            Directory.CreateDirectory(dirtDir);
            Directory.CreateDirectory(woodDir);
            Directory.CreateDirectory(breathDir);
            Directory.CreateDirectory(heartDir);

            // 1. Bước chân ĐẤT đi bộ (4 biến thể, ~0.20s)
            float[] dirtWalkDurs = { 0.20f, 0.21f, 0.19f, 0.21f };
            float[] dirtWalkCutoffs = { 1100f, 1250f, 980f, 1350f };
            float[] dirtWalkThuds = { 80f, 92f, 72f, 102f };
            for (int i = 0; i < 4; i++)
            {
                string rel = $"Assets/Audio/Footsteps/Dirt/dirt_walk_{i + 1:D2}.wav";
                string full = Path.Combine(dirtDir, $"dirt_walk_{i + 1:D2}.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeDirtStep(dirtWalkDurs[i], dirtWalkCutoffs[i], dirtWalkThuds[i], 0.45f, 24f, 101 + i);
                    metrics.Add(WriteWavAndImport(rel, full, samples, false));
                }
            }

            // 2. Bước chân ĐẤT chạy (4 biến thể, ~0.14s)
            float[] dirtRunDurs = { 0.14f, 0.13f, 0.15f, 0.14f };
            float[] dirtRunCutoffs = { 1450f, 1600f, 1380f, 1550f };
            float[] dirtRunThuds = { 88f, 105f, 82f, 110f };
            for (int i = 0; i < 4; i++)
            {
                string rel = $"Assets/Audio/Footsteps/Dirt/dirt_run_{i + 1:D2}.wav";
                string full = Path.Combine(dirtDir, $"dirt_run_{i + 1:D2}.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeDirtStep(dirtRunDurs[i], dirtRunCutoffs[i], dirtRunThuds[i], 0.60f, 35f, 201 + i);
                    metrics.Add(WriteWavAndImport(rel, full, samples, false));
                }
            }

            // Kiểm tra xem đã có âm thanh sàn gỗ thật amthanhsango hay chưa
            bool hasRealWoodAudio = Directory.Exists(Path.Combine(woodDir, "amthanhsango")) && Directory.GetFiles(Path.Combine(woodDir, "amthanhsango"), "*.wav").Length > 0;

            if (!hasRealWoodAudio)
            {
                // 3. Bước chân GỖ đi bộ (4 biến thể chân thực, ~0.26 - 0.32s)
                float[] woodWalkDurs = { 0.28f, 0.30f, 0.27f, 0.31f };
                float[] woodWalkPlankFreqs = { 125f, 110f, 138f, 118f };
                float[] woodWalkImpacts = { 1.0f, 1.05f, 0.95f, 1.15f };
                float[] woodWalkScuffs = { 2200f, 1950f, 2450f, 2100f };
                float[] woodWalkCreaks = { 0.45f, 0.65f, 0.35f, 0.55f };
                for (int i = 0; i < 4; i++)
                {
                    string rel = $"Assets/Audio/Footsteps/Wood/wood_walk_{i + 1:D2}.wav";
                    string full = Path.Combine(woodDir, $"wood_walk_{i + 1:D2}.wav");
                    if (forceRegenerate || !File.Exists(full))
                    {
                        float[] samples = SynthesizeRealisticWoodStep(woodWalkDurs[i], woodWalkPlankFreqs[i], woodWalkImpacts[i], woodWalkScuffs[i], woodWalkCreaks[i], 301 + i);
                        metrics.Add(WriteWavAndImport(rel, full, samples, false));
                    }
                }

                // 4. Bước chân GỖ chạy (4 biến thể chạy rầm rập chân thực, ~0.18 - 0.22s)
                float[] woodRunDurs = { 0.20f, 0.19f, 0.21f, 0.20f };
                float[] woodRunPlankFreqs = { 135f, 120f, 145f, 125f };
                float[] woodRunImpacts = { 1.55f, 1.60f, 1.50f, 1.70f };
                float[] woodRunScuffs = { 2600f, 2300f, 2800f, 2400f };
                float[] woodRunCreaks = { 0.70f, 0.85f, 0.60f, 0.80f };
                for (int i = 0; i < 4; i++)
                {
                    string rel = $"Assets/Audio/Footsteps/Wood/wood_run_{i + 1:D2}.wav";
                    string full = Path.Combine(woodDir, $"wood_run_{i + 1:D2}.wav");
                    if (forceRegenerate || !File.Exists(full))
                    {
                        float[] samples = SynthesizeRealisticWoodStep(woodRunDurs[i], woodRunPlankFreqs[i], woodRunImpacts[i], woodRunScuffs[i], woodRunCreaks[i], 401 + i);
                        metrics.Add(WriteWavAndImport(rel, full, samples, false));
                    }
                }
            }

            // 5. Tiếng gỗ kêu cọt kẹt (3 biến thể cọt kẹt rợn tóc gáy, 0.6 - 0.95s)
            float[] creakDurs = { 0.75f, 0.95f, 0.60f };
            float[] creakStarts = { 320f, 260f, 450f };
            float[] creakEnds = { 580f, 480f, 720f };
            float[] creakIntensities = { 1.1f, 1.25f, 1.0f };
            for (int i = 0; i < 3; i++)
            {
                string rel = $"Assets/Audio/Footsteps/Wood/wood_creak_{i + 1:D2}.wav";
                string full = Path.Combine(woodDir, $"wood_creak_{i + 1:D2}.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeRealisticWoodCreak(creakDurs[i], creakStarts[i], creakEnds[i], creakIntensities[i], 501 + i);
                    metrics.Add(WriteWavAndImport(rel, full, samples, false));
                }
            }

            // (Đã bỏ phần tạo âm thanh thở tổng hợp 6 & 7 theo yêu cầu: Sử dụng file thở thật tiengtho.mp3)

            // 8. Tim đập "thình-thịch" (1 clip, ĐỘ DÀI TỐI ĐA 0.30s: chọn 0.28s)
            {
                string rel = "Assets/Audio/Heartbeat/heartbeat.wav";
                string full = Path.Combine(heartDir, "heartbeat.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeHeartbeat(0.28f, 801);
                    metrics.Add(WriteWavAndImport(rel, full, samples, false));
                }
            }

            // === ÂM THANH MA GHÊ RỢN ===
            string ghostDir = Path.Combine(baseDir, "Ghost");
            Directory.CreateDirectory(ghostDir);

            // 9. Tiếng rên rỉ liên tục của ma (Loop 8.0s, tần số thấp rùng rợn)
            {
                string rel = "Assets/Audio/Ghost/ghost_moan_loop.wav";
                string full = Path.Combine(ghostDir, "ghost_moan_loop.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeGhostMoan(8.0f, 901);
                    metrics.Add(WriteWavAndImport(rel, full, samples, true));
                }
            }

            // 10. Tiếng thở lạnh lẽo của ma (Loop 6.0s, khí lạnh phả vào gáy)
            {
                string rel = "Assets/Audio/Ghost/ghost_breath_loop.wav";
                string full = Path.Combine(ghostDir, "ghost_breath_loop.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeGhostBreath(6.0f, 1001);
                    metrics.Add(WriteWavAndImport(rel, full, samples, true));
                }
            }

            // 11. Tiếng la/rít đột ngột của ma (3 biến thể, 0.8-1.5s)
            float[] scareDurs = { 1.2f, 0.85f, 1.5f };
            float[] scareFreqs = { 380f, 520f, 290f };
            for (int i = 0; i < 3; i++)
            {
                string rel = $"Assets/Audio/Ghost/ghost_scare_{i + 1:D2}.wav";
                string full = Path.Combine(ghostDir, $"ghost_scare_{i + 1:D2}.wav");
                if (forceRegenerate || !File.Exists(full))
                {
                    float[] samples = SynthesizeGhostScare(scareDurs[i], scareFreqs[i], 1101 + i);
                    metrics.Add(WriteWavAndImport(rel, full, samples, false));
                }
            }

            AssetDatabase.Refresh();
            return metrics;
        }

        #region DSP Synthesis Methods

        /// <summary>
        /// 1 & 2: Bước chân Đất (Noise qua Lowpass + Low Thud)
        /// </summary>
        private static float[] SynthesizeDirtStep(float duration, float cutoff, float thudFreq, float thudGain, float decayRate, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var filter = new BiquadFilter();
            filter.SetLowpass(cutoff, SampleRate, 0.707f);

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;

                // White noise
                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float filtered = filter.Process(white);

                // Noise envelope: Fast attack (0.005s), exponential decay
                float attack = Mathf.Clamp01(t / 0.005f);
                float noiseEnv = attack * Mathf.Exp(-decayRate * t);

                // Low thud
                float thud = Mathf.Sin(2f * Mathf.PI * thudFreq * t) * Mathf.Exp(-45f * t);

                buffer[i] = 0.75f * filtered * noiseEnv + thudGain * thud;
            }

            PostProcessBuffer(buffer, 0.70f);
            return buffer;
        }

        /// <summary>
        /// Mô hình vật lý âm học sàn gỗ nhà sàn/nhà gỗ kinh dị:
        /// 1. Heel Impact Thump: Pitch dive 145Hz -> 65Hz với độ nén phi tuyến tính (lực nện gót giày lên mặt sàn gỗ).
        /// 2. Hollow Cavity & Plank Resonance: Bộ lọc 3 mode cộng hưởng ván gỗ (115Hz, 260Hz, 520Hz) kích hoạt bởi xung lực và nhiễu thô.
        /// 3. Sole Scuff: Tiếng ma sát đế giày da/cao su miết trên mặt ván gỗ khô ráp (Bandpass 1600 - 2800Hz).
        /// 4. Micro-Creak: Tiếng thớ gỗ và đinh gỗ oằn nhẹ dưới sức nặng cơ thể (Stick-slip ~450 - 750Hz).
        /// 5. Room Early Reflections: Phản xạ âm trong phòng kín nhà gỗ (12ms, 25ms, 40ms) tạo cảm giác không gian trong nhà chân thực.
        /// </summary>
        private static float[] SynthesizeRealisticWoodStep(float duration, float plankFreq, float impactForce, float scuffTone, float creakAmount, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var plankMode1 = new BiquadFilter();
            var plankMode2 = new BiquadFilter();
            var plankMode3 = new BiquadFilter();
            var scuffFilter = new BiquadFilter();
            var lowThumpFilter = new BiquadFilter();
            var roomReflectFilter = new BiquadFilter();

            plankMode1.SetBandpass(plankFreq, SampleRate, 4.0f);
            plankMode2.SetBandpass(plankFreq * 2.2f, SampleRate, 5.0f);
            plankMode3.SetBandpass(plankFreq * 4.6f, SampleRate, 3.5f);
            scuffFilter.SetBandpass(scuffTone, SampleRate, 2.0f);
            lowThumpFilter.SetLowpass(160f, SampleRate, 0.9f);
            roomReflectFilter.SetLowpass(2200f, SampleRate, 0.707f);

            float creakPhase = 0f;
            float creakFreq = 520f + (float)(rnd.NextDouble() * 120.0 - 60.0);

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;

                // 1. Cú nện gót giày (Heel strike impact thump):
                float thumpFreq = 65f + 80f * Mathf.Exp(-65f * t);
                float thumpEnv = Mathf.Exp(-32f * t);
                float thumpPhase = thumpFreq * t;
                float rawThump = Mathf.Sin(2f * Mathf.PI * thumpPhase) + 0.35f * Mathf.Sin(4f * Mathf.PI * thumpPhase);
                float thump = lowThumpFilter.Process(rawThump) * thumpEnv * impactForce * 1.3f;

                // 2. Kích thích khoang rỗng và thân ván gỗ (Plank & Sub-floor resonance):
                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float excEnv = Mathf.Exp(-45f * t);
                float excitation = white * excEnv;

                float m1 = plankMode1.Process(excitation) * Mathf.Exp(-22f * t) * 1.5f;
                float m2 = plankMode2.Process(excitation) * Mathf.Exp(-30f * t) * 0.9f;
                float m3 = plankMode3.Process(excitation) * Mathf.Exp(-42f * t) * 0.6f;
                float plankBody = m1 + m2 + m3;

                // 3. Tiếng ma sát đế giày (Sole scuff & crunch):
                float scuffAttack = Mathf.Clamp01(t / 0.003f);
                float scuffEnv = scuffAttack * Mathf.Exp(-75f * t);
                float scuff = scuffFilter.Process(white) * scuffEnv * 0.75f;

                // 4. Tiếng ván gỗ oằn nhẹ (Micro-creak / wood strain):
                float microCreak = 0f;
                if (t >= 0.015f && t < 0.12f)
                {
                    float localT = (t - 0.015f) / 0.105f;
                    float creakEnv = Mathf.Sin(Mathf.PI * localT);
                    creakPhase += creakFreq / SampleRate;
                    if (creakPhase >= 1f) creakPhase -= 1f;
                    float pulse = creakPhase < 0.2f ? 1f : -0.25f;
                    microCreak = (pulse * 0.7f + white * 0.3f) * creakEnv * creakAmount * 0.65f;
                }

                buffer[i] = thump + plankBody + scuff + microCreak;
            }

            // 5. Early Reflections trong phòng kín nhà gỗ:
            int tap1 = (int)(SampleRate * 0.012f);
            int tap2 = (int)(SampleRate * 0.024f);
            int tap3 = (int)(SampleRate * 0.038f);
            float[] reflected = new float[numSamples];
            System.Array.Copy(buffer, reflected, numSamples);

            for (int i = 0; i < numSamples; i++)
            {
                float echo = 0f;
                if (i >= tap1) echo += buffer[i - tap1] * 0.22f;
                if (i >= tap2) echo += buffer[i - tap2] * 0.14f;
                if (i >= tap3) echo += buffer[i - tap3] * 0.08f;
                reflected[i] += roomReflectFilter.Process(echo);
            }

            PostProcessBuffer(reflected, 0.92f);
            return reflected;
        }

        /// <summary>
        /// Tiếng ván gỗ cọt kẹt rợn người trong nhà kinh dị (Haunted Wooden Floorboard Creak)
        /// </summary>
        private static float[] SynthesizeRealisticWoodCreak(float duration, float startFreq, float endFreq, float intensity, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var formant1 = new BiquadFilter();
            var formant2 = new BiquadFilter();
            var cavityFilter = new BiquadFilter();
            var highCreakFilter = new BiquadFilter();

            cavityFilter.SetBandpass(160f, SampleRate, 3.0f);
            highCreakFilter.SetBandpass(1850f, SampleRate, 4.0f);

            float phase = 0f;
            float slipRate = 42f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                float env = Mathf.Sin(Mathf.PI * progress);
                env = Mathf.Pow(env, 1.25f);

                float wobble = Mathf.Sin(2f * Mathf.PI * 4.5f * t) * 25f;
                float curFreq = startFreq + (endFreq - startFreq) * progress + wobble;

                formant1.SetBandpass(curFreq, SampleRate, 5.5f);
                formant2.SetBandpass(curFreq * 1.85f, SampleRate, 6.0f);

                phase += slipRate / SampleRate;
                if (phase >= 1f) phase -= 1f;
                float slipImpulse = phase < 0.18f ? 1.0f : -0.2f;

                float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float excitation = slipImpulse * 0.70f + white * 0.30f;

                float f1 = formant1.Process(excitation) * 1.4f;
                float f2 = formant2.Process(excitation) * 0.8f;
                float sub = cavityFilter.Process(excitation) * 0.5f;
                float high = highCreakFilter.Process(white) * 0.25f;

                buffer[i] = (f1 + f2 + sub + high) * env * intensity;
            }

            PostProcessBuffer(buffer, 0.90f);
            return buffer;
        }

        /// <summary>
        /// 6: Thở hổn hển (Loop liền mạch 6.0s, 4 chu kỳ hít/thở 1.5s, bandpass noise 500-2500Hz)
        /// </summary>
        private static float[] SynthesizeBreathingLoop(float duration, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var filter = new BiquadFilter();

            float cycleDuration = 1.50f; // 4 chu kỳ trong 6.0s
            int cycles = Mathf.RoundToInt(duration / cycleDuration);

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float cycleT = t % cycleDuration;

                float sample = 0f;

                // White noise generator
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);

                if (cycleT < 0.65f)
                {
                    // INHALE (0.0s -> 0.65s): Nhịp hít vào
                    float normT = cycleT / 0.65f;
                    float env = Mathf.Pow(Mathf.Sin(Mathf.PI * normT), 1.6f);

                    // Swept bandpass: 900Hz -> 1350Hz
                    float centerFreq = Mathf.Lerp(900f, 1350f, normT);
                    if (i % 4 == 0) filter.SetBandpass(centerFreq, SampleRate, 1.8f);

                    sample = filter.Process(noise) * env * 0.85f;
                }
                else if (cycleT < 0.75f)
                {
                    // PAUSE 1 (0.65s -> 0.75s): Nghỉ ngắn giữa hít và thở
                    sample = 0f;
                }
                else if (cycleT < 1.40f)
                {
                    // EXHALE (0.75s -> 1.40s): Nhịp thở ra
                    float normT = (cycleT - 0.75f) / 0.65f;
                    float env = Mathf.Pow(Mathf.Sin(Mathf.PI * normT), 1.4f);

                    // Swept bandpass: 1200Hz -> 720Hz (ấm hơn, trầm dần)
                    float centerFreq = Mathf.Lerp(1200f, 720f, normT);
                    if (i % 4 == 0) filter.SetBandpass(centerFreq, SampleRate, 1.6f);

                    sample = filter.Process(noise) * env * 1.0f;
                }
                else
                {
                    // PAUSE 2 (1.40s -> 1.50s): Nghỉ trước chu kỳ kế
                    sample = 0f;
                }

                buffer[i] = sample;
            }

            // Đảm bảo Seamless Loop: biên độ ở đầu (t=0) và cuối (t=6.0) đều bằng 0
            PostProcessBuffer(buffer, 0.70f);
            return buffer;
        }

        /// <summary>
        /// 7: Hớp hơi khi cạn sức (Inhale gấp gáp 0.5s, dải 700-2400Hz, tăng nhanh rồi tắt)
        /// </summary>
        private static float[] SynthesizeGasp(float duration, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var filter = new BiquadFilter();

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                // Gasp envelope: rises sharply in 0.12s, fades to 0 by duration
                float env;
                if (t < 0.12f)
                {
                    env = Mathf.Pow(t / 0.12f, 1.2f);
                }
                else
                {
                    env = Mathf.Pow(1f - (t - 0.12f) / (duration - 0.12f), 2.2f);
                }

                // Swept bandpass rising with sharp inhale: 850Hz -> 1600Hz
                float centerFreq = Mathf.Lerp(850f, 1600f, Mathf.Clamp01(t / 0.20f));
                if (i % 4 == 0) filter.SetBandpass(centerFreq, SampleRate, 2.0f);

                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                buffer[i] = filter.Process(noise) * env;
            }

            PostProcessBuffer(buffer, 0.70f);
            return buffer;
        }

        /// <summary>
        /// 8: Tim đập thình-thịch (Độ dài 0.28s <= 0.30s, hai nhịp sine trầm 50-70Hz, nhịp 2 ở 0.135s)
        /// </summary>
        private static float[] SynthesizeHeartbeat(float duration, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float sample = 0f;

                // Nhịp 1 ("Thình" / Lub): bắt đầu tại t = 0.0s
                if (t >= 0f && t < 0.12f)
                {
                    float t1 = t;
                    // Pitch giảm nhẹ từ 68Hz xuống 50Hz
                    float f1 = Mathf.Lerp(68f, 50f, t1 / 0.10f);
                    float env1 = Mathf.Clamp01(t1 / 0.006f) * Mathf.Exp(-38f * t1);
                    float tone1 = Mathf.Sin(2f * Mathf.PI * f1 * t1);
                    float sub1 = 0.35f * Mathf.Sin(2f * Mathf.PI * (f1 * 0.5f) * t1);
                    sample += (tone1 + sub1) * env1 * 1.0f;
                }

                // Nhịp 2 ("Thịch" / Dub): bắt đầu tại t = 0.135s với biên độ ~70%
                if (t >= 0.135f && t < 0.25f)
                {
                    float t2 = t - 0.135f;
                    // Pitch giảm nhẹ từ 60Hz xuống 45Hz
                    float f2 = Mathf.Lerp(60f, 45f, t2 / 0.08f);
                    float env2 = Mathf.Clamp01(t2 / 0.005f) * Mathf.Exp(-42f * t2);
                    float tone2 = Mathf.Sin(2f * Mathf.PI * f2 * t2);
                    float sub2 = 0.30f * Mathf.Sin(2f * Mathf.PI * (f2 * 0.5f) * t2);
                    sample += (tone2 + sub2) * env2 * 0.70f;
                }

                // Từ 0.25s -> 0.28s là khoảng lặng hoàn toàn tắt trước 0.30s
                buffer[i] = sample;
            }

            PostProcessBuffer(buffer, 0.70f);
            return buffer;
        }

        /// <summary>
        /// 9: Tiếng rên rỉ ma (Loop 8.0s): Nhiều lớp drone tần số thấp + noise qua bandpass quét chậm
        /// Tạo cảm giác oan hồn rên rỉ lờ mờ trong bóng tối
        /// </summary>
        private static float[] SynthesizeGhostMoan(float duration, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var filter1 = new BiquadFilter();
            var filter2 = new BiquadFilter();

            float phaseMain = 0f;
            float phaseSub = 0f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                // 1. Drone tần số thấp rùng rợn: 2 sine trầm chồng lên nhau
                float mainFreq = 85f + 15f * Mathf.Sin(2f * Mathf.PI * 0.12f * t); // Rung nhẹ 0.12Hz
                phaseMain += mainFreq / SampleRate;
                if (phaseMain >= 1f) phaseMain -= 1f;
                float drone1 = Mathf.Sin(2f * Mathf.PI * phaseMain);

                float subFreq = 62f + 8f * Mathf.Sin(2f * Mathf.PI * 0.08f * t);
                phaseSub += subFreq / SampleRate;
                if (phaseSub >= 1f) phaseSub -= 1f;
                float drone2 = Mathf.Sin(2f * Mathf.PI * phaseSub) * 0.6f;

                // 2. Noise qua swept bandpass: mô phỏng tiếng rên rỉ
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float sweepFreq = 280f + 180f * Mathf.Sin(2f * Mathf.PI * 0.18f * t); // Quét chậm 0.18Hz
                if (i % 8 == 0) filter1.SetBandpass(sweepFreq, SampleRate, 4.5f);
                float moanNoise = filter1.Process(noise) * 1.8f;

                // 3. Lớp tiếng rít cao rất nhẹ (eerie overtone)
                float eerieFreq = 520f + 100f * Mathf.Sin(2f * Mathf.PI * 0.07f * t);
                if (i % 8 == 0) filter2.SetBandpass(eerieFreq, SampleRate, 8.0f);
                float eerie = filter2.Process(noise) * 0.25f;

                // Envelope: seamless loop với chu kỳ lặp mượt
                float loopEnv = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * progress);

                buffer[i] = (0.35f * drone1 + 0.25f * drone2 + 0.55f * moanNoise + eerie) * loopEnv;
            }

            PostProcessBuffer(buffer, 0.65f);
            return buffer;
        }

        /// <summary>
        /// 10: Tiếng thở lạnh lẽo của ma (Loop 6.0s): Noise qua bandpass hẹp mô phỏng hơi thở lạnh
        /// Cảm giác ai đó thở phả vào gáy bạn
        /// </summary>
        private static float[] SynthesizeGhostBreath(float duration, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var filter = new BiquadFilter();
            float cycleDuration = 3.0f; // 2 chu kỳ thở trong 6s

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float cycleT = t % cycleDuration;

                float sample = 0f;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);

                if (cycleT < 1.8f)
                {
                    // Thở ra lạnh lẽo chậm rãi (0 -> 1.8s)
                    float normT = cycleT / 1.8f;
                    // Envelope hình sin nửa chu kỳ
                    float env = Mathf.Sin(Mathf.PI * normT);
                    env = Mathf.Pow(env, 0.8f); // Mở rộng phần giữa

                    // Bandpass hẹp: hơi thở lạnh ~600-900Hz
                    float centerFreq = Mathf.Lerp(650f, 850f, normT);
                    if (i % 4 == 0) filter.SetBandpass(centerFreq, SampleRate, 2.2f);

                    sample = filter.Process(noise) * env * 0.9f;

                    // Thêm tần số rất thấp tạo cảm giác rùng mình
                    sample += 0.15f * Mathf.Sin(2f * Mathf.PI * 45f * t) * env;
                }
                else if (cycleT < 2.0f)
                {
                    // Im lặng đáng sợ (1.8s -> 2.0s)
                    sample = 0f;
                }
                else
                {
                    // Hít vào yếu ớt rùng rợn (2.0s -> 3.0s)
                    float normT = (cycleT - 2.0f) / 1.0f;
                    float env = Mathf.Sin(Mathf.PI * normT);
                    env *= 0.4f; // Nhẹ hơn thở ra

                    float centerFreq = Mathf.Lerp(500f, 750f, normT);
                    if (i % 4 == 0) filter.SetBandpass(centerFreq, SampleRate, 1.8f);

                    sample = filter.Process(noise) * env;
                }

                buffer[i] = sample;
            }

            PostProcessBuffer(buffer, 0.60f);
            return buffer;
        }

        /// <summary>
        /// 11: Tiếng la/rít đột ngột của ma (0.8-1.5s): Tần số cao swept nhanh + noise burst
        /// Tiếng rít ghê rợn bất ngờ khi ma ở gần
        /// </summary>
        private static float[] SynthesizeGhostScare(float duration, float baseFreq, int seed)
        {
            int numSamples = (int)(SampleRate * duration);
            float[] buffer = new float[numSamples];
            var rnd = new System.Random(seed);

            var filter1 = new BiquadFilter();
            var filter2 = new BiquadFilter();

            for (int i = 0; i < numSamples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                // Envelope: tăng nhanh rồi giảm dần (giật mình)
                float env;
                float attackTime = 0.05f;
                if (t < attackTime)
                {
                    env = Mathf.Pow(t / attackTime, 0.5f); // Tăng rất nhanh
                }
                else
                {
                    env = Mathf.Pow(1f - (t - attackTime) / (duration - attackTime), 1.8f);
                }

                // Lớp 1: Tiếng rít cao swept nhanh lên
                float sweepFreq = baseFreq + 600f * progress; // Tăng dần tần số
                if (i % 4 == 0) filter1.SetBandpass(sweepFreq, SampleRate, 3.5f);
                float noise1 = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float screech = filter1.Process(noise1) * 1.5f;

                // Lớp 2: Drone trầm đáng sợ
                float droneFreq = baseFreq * 0.3f;
                float drone = Mathf.Sin(2f * Mathf.PI * droneFreq * t) * 0.4f;

                // Lớp 3: Noise burst cao (metallic screech)
                float highFreq = 1200f + 800f * Mathf.Sin(2f * Mathf.PI * 3f * t);
                if (i % 8 == 0) filter2.SetBandpass(highFreq, SampleRate, 5.0f);
                float metallic = filter2.Process(noise1) * 0.3f * Mathf.Exp(-3f * t);

                buffer[i] = (screech + drone + metallic) * env;
            }

            PostProcessBuffer(buffer, 0.75f);
            return buffer;
        }

        #endregion

        #region Audio Post-Processing (Fade & Normalization)

        /// <summary>
        /// Áp dụng Fade-in 5ms, Fade-out 5ms chống click và Chuẩn hóa đỉnh về targetPeak (mặc định 0.70)
        /// </summary>
        private static void PostProcessBuffer(float[] buffer, float targetPeak = 0.70f)
        {
            if (buffer == null || buffer.Length == 0) return;

            int fadeSamples = Mathf.Min((int)(0.005f * SampleRate), buffer.Length / 4);

            // 1. Fade-in 5ms (Cosine curve mượt)
            for (int i = 0; i < fadeSamples; i++)
            {
                float gain = 0.5f * (1f - Mathf.Cos(Mathf.PI * i / fadeSamples));
                buffer[i] *= gain;
            }

            // 2. Fade-out 5ms (Cosine curve mượt)
            int n = buffer.Length;
            for (int i = 0; i < fadeSamples; i++)
            {
                float gain = 0.5f * (1f - Mathf.Cos(Mathf.PI * i / fadeSamples));
                buffer[n - 1 - i] *= gain;
            }

            // 3. Tìm đỉnh tuyệt đối
            float maxVal = 0f;
            for (int i = 0; i < n; i++)
            {
                float abs = Mathf.Abs(buffer[i]);
                if (abs > maxVal) maxVal = abs;
            }

            // 4. Chuẩn hóa đỉnh về targetPeak (0.70)
            if (maxVal > 0.0001f)
            {
                float scale = targetPeak / maxVal;
                for (int i = 0; i < n; i++)
                {
                    buffer[i] *= scale;
                }
            }
        }

        #endregion

        #region File IO & AudioImporter

        /// <summary>
        /// Ghi buffer float ra file WAV 16-bit PCM Mono 44.1kHz và cấu hình AudioImporter
        /// </summary>
        private static ClipMetric WriteWavAndImport(string relativePath, string fullPath, float[] buffer, bool isLoopCompressed)
        {
            int numSamples = buffer.Length;

            // Tính toán số liệu thực tế của file
            float maxPeak = 0f;
            double sumSquares = 0.0;
            for (int i = 0; i < numSamples; i++)
            {
                float val = buffer[i];
                float abs = Mathf.Abs(val);
                if (abs > maxPeak) maxPeak = abs;
                sumSquares += val * val;
            }
            float rms = (float)Math.Sqrt(sumSquares / numSamples);
            float duration = numSamples / (float)SampleRate;

            // Ghi file WAV chuẩn RIFF
            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                // RIFF Header
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + numSamples * 2);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);                 // Chunk size
                bw.Write((short)1);           // PCM
                bw.Write((short)1);           // Mono (1 channel)
                bw.Write(SampleRate);         // 44100 Hz
                bw.Write(SampleRate * 2);     // Byte rate (44100 * 1 * 2)
                bw.Write((short)2);           // Block align
                bw.Write((short)16);          // 16-bit

                // data chunk
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(numSamples * 2);

                for (int i = 0; i < numSamples; i++)
                {
                    short sampleShort = (short)Mathf.Clamp(buffer[i] * 32767f, -32768f, 32767f);
                    bw.Write(sampleShort);
                }
            }

            // Tự động cấu hình AudioImporter
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            AudioImporter importer = AssetImporter.GetAtPath(relativePath) as AudioImporter;
            if (importer != null)
            {
                importer.forceToMono = true;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;

                if (isLoopCompressed)
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.8f;
                }
                else
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                }

                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            return new ClipMetric
            {
                FileName = Path.GetFileName(fullPath),
                RelativePath = relativePath,
                Duration = duration,
                Peak = maxPeak,
                Rms = rms,
                Source = "Tổng hợp thủ tục C# (Deterministic DSP)"
            };
        }

        private static void PrintMetricsReport(List<ClipMetric> metrics)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=cyan><b>[Horror Audio] BÁO CÁO THÔNG SỐ CÁC FILE ÂM THANH ĐÃ TẠO:</b></color>");
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------");
            sb.AppendLine(string.Format("{0,-32} | {1,-10} | {2,-8} | {3,-8} | {4}", "Tên File", "Độ dài (s)", "Peak", "RMS", "Nguồn"));
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------");

            foreach (var m in metrics)
            {
                sb.AppendLine(string.Format("{0,-32} | {1,-10:F3} | {2,-8:F3} | {3,-8:F3} | {4}",
                    m.FileName, m.Duration, m.Peak, m.Rms, m.Source));
            }
            sb.AppendLine("----------------------------------------------------------------------------------------------------------------");
            Debug.Log(sb.ToString());
        }

        #endregion

        #region Biquad Filter Helper

        private class BiquadFilter
        {
            private float b0, b1, b2, a1, a2;
            private float x1, x2, y1, y2;

            public void SetLowpass(float cutoff, float fs, float q = 0.707f)
            {
                float w0 = 2f * Mathf.PI * Mathf.Clamp(cutoff, 20f, fs * 0.48f) / fs;
                float cosW = Mathf.Cos(w0);
                float sinW = Mathf.Sin(w0);
                float alpha = sinW / (2f * q);

                float a0 = 1f + alpha;
                b0 = ((1f - cosW) / 2f) / a0;
                b1 = (1f - cosW) / a0;
                b2 = ((1f - cosW) / 2f) / a0;
                a1 = (-2f * cosW) / a0;
                a2 = (1f - alpha) / a0;
            }

            public void SetBandpass(float centerFreq, float fs, float q = 1.8f)
            {
                float w0 = 2f * Mathf.PI * Mathf.Clamp(centerFreq, 30f, fs * 0.48f) / fs;
                float cosW = Mathf.Cos(w0);
                float sinW = Mathf.Sin(w0);
                float alpha = sinW / (2f * q);

                float a0 = 1f + alpha;
                b0 = alpha / a0;
                b1 = 0f;
                b2 = -alpha / a0;
                a1 = (-2f * cosW) / a0;
                a2 = (1f - alpha) / a0;
            }

            public float Process(float inSample)
            {
                float outSample = b0 * inSample + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1;
                x1 = inSample;
                y2 = y1;
                y1 = outSample;
                return outSample;
            }
        }

        #endregion
    }
}
