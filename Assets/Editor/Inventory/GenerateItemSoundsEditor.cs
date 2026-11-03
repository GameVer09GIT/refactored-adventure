using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HorrorGame.Inventory.Editor
{
    /// <summary>
    /// Tạo các file âm thanh WAV chất lượng 16-bit 44100Hz cho hệ thống vật phẩm (Flashlight, Radar, Hotbar)
    /// </summary>
    public static class GenerateItemSoundsEditor
    {
        private const int SampleRate = 44100;
        private const string OutputFolder = "Assets/Audio/Items";

        [MenuItem("Tools/Audio/Generate Item Sounds")]
        public static void GenerateAllSounds()
        {
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
            }

            GenerateFlashlightClick();
            GenerateRadarScan();
            GenerateRadarBlip();
            GenerateHotbarSwitch();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ConfigureAudioImporters();

            Debug.Log("<color=green><b>[GenerateItemSounds] Đã tạo thành công toàn bộ 4 file âm thanh tại Assets/Audio/Items/!</b></color>");
        }

        private static void GenerateFlashlightClick()
        {
            // Tiếng click đèn pin: ~0.08s, 2 xung cơ khí sắc gọn (bấm và nhả chốt)
            float duration = 0.08f;
            int numSamples = (int)(SampleRate * duration);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Xung 1 tại t = 0.005s (tiếng nhắp công tắc)
                float pulse1 = 0f;
                if (t >= 0.002f && t < 0.025f)
                {
                    float dt1 = t - 0.002f;
                    float env1 = Mathf.Exp(-dt1 * 250f);
                    pulse1 = Mathf.Sin(2f * Mathf.PI * 1800f * dt1) * env1 * 0.75f
                           + Mathf.Sin(2f * Mathf.PI * 650f * dt1) * env1 * 0.35f;
                }

                // Xung 2 tại t = 0.035s (tiếng chốt ăn khớp)
                float pulse2 = 0f;
                if (t >= 0.032f && t < 0.075f)
                {
                    float dt2 = t - 0.032f;
                    float env2 = Mathf.Exp(-dt2 * 180f);
                    pulse2 = Mathf.Sin(2f * Mathf.PI * 2200f * dt2) * env2 * 0.65f
                           + Mathf.Sin(2f * Mathf.PI * 850f * dt2) * env2 * 0.3f;
                }

                float sampleVal = pulse1 + pulse2;

                // Fade 5ms 2 đầu
                sampleVal *= ApplyFade(i, numSamples, 0.005f);
                sampleVal = Mathf.Clamp(sampleVal, -0.72f, 0.72f);

                samples[i] = (short)(sampleVal * 32767f);
            }

            WriteWavFile(Path.Combine(OutputFolder, "flashlight_click.wav"), samples, SampleRate);
        }

        private static void GenerateRadarScan()
        {
            // Tiếng quét radar: ~1.5s, sóng sin trượt tần số từ 420Hz lên 1050Hz kèm xung nhịp
            float duration = 1.5f;
            int numSamples = (int)(SampleRate * duration);
            short[] samples = new short[numSamples];

            double phase = 0;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Tần số trượt từ 420Hz lên 1050Hz
                float currentFreq = Mathf.Lerp(420f, 1050f, Mathf.Pow(progress, 1.4f));
                phase += 2.0 * Math.PI * currentFreq / SampleRate;

                // Sóng sin chính + hài âm nhẹ
                float wave = (float)Math.Sin(phase) * 0.55f
                           + (float)Math.Sin(phase * 2.0) * 0.15f;

                // Điều biến biên độ (AM pulse) tạo cảm giác sóng quét radar điện tử
                float pulse = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 8f * t);
                float sampleVal = wave * pulse;

                // Envelope tổng thể: fade in 50ms, fade out 100ms
                float masterEnv = 1f;
                if (t < 0.05f) masterEnv = t / 0.05f;
                else if (t > duration - 0.12f) masterEnv = (duration - t) / 0.12f;

                sampleVal *= masterEnv;
                sampleVal *= ApplyFade(i, numSamples, 0.005f);
                sampleVal = Mathf.Clamp(sampleVal, -0.70f, 0.70f);

                samples[i] = (short)(sampleVal * 32767f);
            }

            WriteWavFile(Path.Combine(OutputFolder, "radar_scan.wav"), samples, SampleRate);
        }

        private static void GenerateRadarBlip()
        {
            // Tiếng bíp phát hiện ma: ~0.12s, ~880Hz (nốt La), sắc nét và mờ dần
            float duration = 0.12f;
            int numSamples = (int)(SampleRate * duration);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 35f); // Tắt dần theo hàm mũ

                float wave = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.65f
                           + Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.25f;

                float sampleVal = wave * env;
                sampleVal *= ApplyFade(i, numSamples, 0.005f);
                sampleVal = Mathf.Clamp(sampleVal, -0.70f, 0.70f);

                samples[i] = (short)(sampleVal * 32767f);
            }

            WriteWavFile(Path.Combine(OutputFolder, "radar_blip.wav"), samples, SampleRate);
        }

        private static void GenerateHotbarSwitch()
        {
            // Tiếng chuyển ô: ~0.10s, tiếng sột soạt nhẹ và tiếng "tách" êm tai
            float duration = 0.10f;
            int numSamples = (int)(SampleRate * duration);
            short[] samples = new short[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 45f);

                float wave = Mathf.Sin(2f * Mathf.PI * 920f * t) * 0.45f
                           + Mathf.Sin(2f * Mathf.PI * 340f * t) * 0.30f;

                float sampleVal = wave * env;
                sampleVal *= ApplyFade(i, numSamples, 0.005f);
                sampleVal = Mathf.Clamp(sampleVal, -0.65f, 0.65f);

                samples[i] = (short)(sampleVal * 32767f);
            }

            WriteWavFile(Path.Combine(OutputFolder, "hotbar_switch.wav"), samples, SampleRate);
        }

        private static float ApplyFade(int index, int total, float fadeDurationSec)
        {
            int fadeSamples = (int)(SampleRate * fadeDurationSec);
            if (fadeSamples <= 0) return 1f;

            if (index < fadeSamples)
            {
                return (float)index / fadeSamples;
            }
            if (index > total - fadeSamples)
            {
                return (float)(total - index) / fadeSamples;
            }
            return 1f;
        }

        private static void WriteWavFile(string filepath, short[] samples, int sampleRate)
        {
            int byteRate = sampleRate * 2;
            int dataChunkSize = samples.Length * 2;
            int fileSize = 36 + dataChunkSize;

            using (var stream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // RIFF chunk
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(fileSize);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1Size (16 for PCM)
                writer.Write((short)1); // AudioFormat (1 for PCM)
                writer.Write((short)1); // NumChannels (1 = Mono)
                writer.Write(sampleRate); // SampleRate
                writer.Write(byteRate); // ByteRate
                writer.Write((short)2); // BlockAlign (2 bytes per sample for 16-bit mono)
                writer.Write((short)16); // BitsPerSample

                // data chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataChunkSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    writer.Write(samples[i]);
                }
            }
        }

        private static void ConfigureAudioImporters()
        {
            string[] files = Directory.GetFiles(OutputFolder, "*.wav");
            foreach (var f in files)
            {
                string assetPath = f.Replace('\\', '/');
                AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer != null)
                {
                    importer.forceToMono = true;
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    importer.defaultSampleSettings = settings;
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
