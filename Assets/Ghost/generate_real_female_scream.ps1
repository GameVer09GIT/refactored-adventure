Add-Type -TypeDefinition @"
using System;
using System.IO;

public class ScreamGenerator
{
    private class BiquadFilter
    {
        private double b0, b1, b2, a1, a2;
        private double x1, x2, y1, y2;

        public static BiquadFilter CreateBandpass(double sampleRate, double freq, double q)
        {
            var filter = new BiquadFilter();
            double omega = 2.0 * Math.PI * freq / sampleRate;
            double alpha = Math.Sin(omega) / (2.0 * q);
            double cosW = Math.Cos(omega);

            double a0 = 1.0 + alpha;
            filter.b0 = (alpha * q) / a0; // scaled for formant peak
            filter.b1 = 0.0;
            filter.b2 = -(alpha * q) / a0;
            filter.a1 = (-2.0 * cosW) / a0;
            filter.a2 = (1.0 - alpha) / a0;
            return filter;
        }

        public double Process(double x)
        {
            double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x;
            y2 = y1; y1 = y;
            return y;
        }
    }

    public static void GenerateScream(string outputPath, double duration = 3.3, int sampleRate = 44100)
    {
        int numSamples = (int)(duration * sampleRate);
        double[] output = new double[numSamples];
        Random rnd = new Random(1337);

        // Vocal tract formant resonators for female scream 'AAAAH!'
        BiquadFilter f1_a = BiquadFilter.CreateBandpass(sampleRate, 1150.0, 4.5);
        BiquadFilter f2_a = BiquadFilter.CreateBandpass(sampleRate, 2400.0, 5.5);
        BiquadFilter f3_a = BiquadFilter.CreateBandpass(sampleRate, 3650.0, 6.0);
        BiquadFilter f4_a = BiquadFilter.CreateBandpass(sampleRate, 5050.0, 4.5);

        BiquadFilter f1_b = BiquadFilter.CreateBandpass(sampleRate, 1250.0, 4.5);
        BiquadFilter f2_b = BiquadFilter.CreateBandpass(sampleRate, 2600.0, 5.5);
        BiquadFilter f3_b = BiquadFilter.CreateBandpass(sampleRate, 3850.0, 6.0);

        double phase1 = 0.0;
        double phase2 = 0.0;

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;

            // 1. Pitch trajectory (Frantic terror scream: sharp strike, peaks at 0.3s, holds agonizingly, drops as breath empties)
            double basePitch1;
            if (t < 0.2)
                basePitch1 = 820.0 + (t / 0.2) * 380.0; // 820 -> 1200 Hz
            else if (t < 1.7)
                basePitch1 = 1200.0 - ((t - 0.2) / 1.5) * 150.0; // 1200 -> 1050 Hz
            else
                basePitch1 = 1050.0 - ((t - 1.7) / (duration - 1.7)) * 400.0; // 1050 -> 650 Hz

            // Tremolo / flutter (vocal cord strain under terror)
            double flutter = Math.Sin(2.0 * Math.PI * 7.5 * t) * 35.0 
                           + Math.Sin(2.0 * Math.PI * 45.0 * t) * 22.0
                           + (rnd.NextDouble() - 0.5) * 18.0; // jitter

            double pitch1 = Math.Max(200.0, basePitch1 + flutter);
            double pitch2 = pitch1 * 1.28 + Math.Sin(2.0 * Math.PI * 9.0 * t) * 25.0; // secondary higher shrieking harmony

            // Proper phase accumulation
            phase1 += (2.0 * Math.PI * pitch1) / sampleRate;
            if (phase1 > 2.0 * Math.PI) phase1 -= 2.0 * Math.PI;

            phase2 += (2.0 * Math.PI * pitch2) / sampleRate;
            if (phase2 > 2.0 * Math.PI) phase2 -= 2.0 * Math.PI;

            // Glottal excitation wave (asymmetric pulse + subharmonics from vocal cord bifurcation)
            double glottal1 = Math.Sin(phase1) + 0.55 * Math.Sin(phase1 * 2.0) + 0.35 * Math.Sin(phase1 * 3.0) + 0.3 * Math.Sin(phase1 * 0.5);
            double glottal2 = Math.Sin(phase2) + 0.45 * Math.Sin(phase2 * 2.0) + 0.25 * Math.Sin(phase2 * 3.0);

            // Breath turbulence / aspirated rushing air
            double noise = (rnd.NextDouble() * 2.0 - 1.0);
            double breathExcitation = noise * (0.35 + 0.25 * Math.Sin(2.0 * Math.PI * 55.0 * t));

            double voiceExA = glottal1 + breathExcitation * 0.8;
            double voiceExB = glottal2 + breathExcitation * 0.5;

            // Formant filtering through vocal tract
            double formantsA = 0.45 * f1_a.Process(voiceExA) 
                             + 0.40 * f2_a.Process(voiceExA) 
                             + 0.30 * f3_a.Process(voiceExA)
                             + 0.18 * f4_a.Process(voiceExA);

            double formantsB = 0.35 * f1_b.Process(voiceExB) 
                             + 0.32 * f2_b.Process(voiceExB) 
                             + 0.22 * f3_b.Process(voiceExB);

            double screamVoice = formantsA + formantsB;

            // 2. Jumpscare Stinger (Sub-bass chest thud + metallic horror dissonance at t=0)
            double jumpscareSting = 0.0;
            if (t < 0.8)
            {
                double bassEnv = Math.Exp(-t * 5.0);
                double bass = Math.Sin(2.0 * Math.PI * (75.0 - t * 45.0) * t) * bassEnv * 0.9;
                
                // Dissonant metallic horror clash
                double metalEnv = Math.Exp(-t * 6.5);
                double metal = (Math.Sin(2.0 * Math.PI * 2310.0 * t) + Math.Sin(2.0 * Math.PI * 2435.0 * t)) * metalEnv * 0.35;
                
                jumpscareSting = bass + metal;
            }

            // 3. Amplitude envelope
            double env;
            if (t < 0.012)
                env = t / 0.012; // Explosive immediate shock attack
            else if (t < 2.2)
                env = 1.0; // High sustain
            else
                env = Math.Max(0.0, 1.0 - (t - 2.2) / (duration - 2.2)); // Dying breath gasp fade

            double combined = (screamVoice * 0.85 + jumpscareSting) * env;

            // Soft saturation / scream distortion for maximum throat tearing intensity
            output[i] = Math.Tanh(combined * 1.8);
        }

        // Normalize to -0.3 dB
        double maxAmp = 0.0001;
        for (int i = 0; i < numSamples; i++)
        {
            double abs = Math.Abs(output[i]);
            if (abs > maxAmp) maxAmp = abs;
        }

        double gain = 0.97 / maxAmp;

        // Write 16-bit PCM WAV
        using (var fs = new FileStream(outputPath, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + numSamples * 2);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1); // PCM
            bw.Write((short)1); // Mono
            bw.Write(sampleRate);
            bw.Write(sampleRate * 2);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(numSamples * 2);

            for (int i = 0; i < numSamples; i++)
            {
                short val = (short)Math.Max(-32767, Math.Min(32767, (int)(output[i] * gain * 32700.0)));
                bw.Write(val);
            }
        }
    }
}
"@

$target1 = "c:\Users\ngomi\refactored-adventure\Assets\Ghost\Audio\jumpscare_scream.wav"
$target2 = "c:\Users\ngomi\refactored-adventure\Assets\Ghost\Resources\jumpscare_scream.wav"

[ScreamGenerator]::GenerateScream($target1, 3.2, 44100)
Copy-Item $target1 $target2 -Force
Write-Host "Successfully generated high-quality female horror scream to $target1 and $target2"
