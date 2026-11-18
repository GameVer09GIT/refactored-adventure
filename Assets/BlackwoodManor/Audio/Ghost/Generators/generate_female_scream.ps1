param()

$wavPath1 = "Assets\Ghost\Audio\jumpscare_scream.wav"
$wavPath2 = "Assets\Ghost\Resources\jumpscare_scream.wav"

$sampleRate = 44100
$duration = 3.2
$numSamples = [int]($sampleRate * $duration)

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# RIFF Header
$bw.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
$bw.Write([int](36 + $numSamples * 2))
$bw.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
$bw.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
$bw.Write([int]16)
$bw.Write([int16]1)        # PCM Mono
$bw.Write([int16]1)        # 1 Channel
$bw.Write([int]$sampleRate)
$bw.Write([int]($sampleRate * 2))
$bw.Write([int16]2)        # Block align
$bw.Write([int16]16)       # 16-bit
$bw.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
$bw.Write([int]($numSamples * 2))

$rnd = New-Object System.Random(42)

for ($i = 0; $i -lt $numSamples; $i++) {
    $t = $i / [double]$sampleRate
    
    # 1. Pitch contour of a female terror shriek
    # Starts sharp at 1250 Hz, peaks at 1480 Hz at t=0.2s, stays frantic around 1150-1350 Hz, slowly descends to 850 Hz at tail
    $basePitch = 1250.0
    if ($t -lt 0.25) {
        $basePitch = 1250.0 + ($t / 0.25) * 230.0
    } elseif ($t -lt 1.8) {
        $basePitch = 1480.0 - (($t - 0.25) / 1.55) * 200.0
    } else {
        $basePitch = 1280.0 - (($t - 1.8) / 1.4) * 420.0
    }

    # Vocal acoustic roughness / chaotic vibrato (32 Hz and 48 Hz warble)
    $vibrato = [Math]::Sin(2.0 * [Math]::PI * 34.0 * $t) * 90.0 + [Math]::Sin(2.0 * [Math]::PI * 52.0 * $t) * 60.0
    $f0 = $basePitch + $vibrato

    # Female vocal formants (open mouth screaming 'AAAAAH')
    # F0 (fundamental female scream ~1200-1400Hz)
    # H2 (second harmonic ~2500Hz)
    # H3 (third harmonic ~3700Hz)
    # H4 (fourth harmonic ~4900Hz)
    # Subharmonic F0/2 (~600Hz vocal cord bifurcation)
    $subH = 0.22 * [Math]::Sin(2.0 * [Math]::PI * ($f0 * 0.5) * $t)
    $h1   = 0.45 * [Math]::Sin(2.0 * [Math]::PI * $f0 * $t)
    $h2   = 0.38 * [Math]::Sin(2.0 * [Math]::PI * ($f0 * 2.0) * $t)
    $h3   = 0.28 * [Math]::Sin(2.0 * [Math]::PI * ($f0 * 3.0) * $t)
    $h4   = 0.18 * [Math]::Sin(2.0 * [Math]::PI * ($f0 * 4.0) * $t)
    $h5   = 0.10 * [Math]::Sin(2.0 * [Math]::PI * ($f0 * 5.0) * $t)

    $vocal = $subH + $h1 + $h2 + $h3 + $h4 + $h5

    # 2. Vocal rasp, breathless turbulence, high frequency shrieking noise
    $rawNoise = ($rnd.NextDouble() * 2.0 - 1.0)
    $breath = $rawNoise * (0.28 + 0.10 * [Math]::Sin(2.0 * [Math]::PI * 70.0 * $t))

    # 3. Horror bass thump at the very start (jump impact shock)
    $thump = 0.0
    if ($t -lt 0.6) {
        $thump = [Math]::Exp(- $t * 6.0) * [Math]::Sin(2.0 * [Math]::PI * 75.0 * $t) * 0.65
    }

    # 4. Amplitude envelope
    # Instant explosive start, high level sustain, natural vocal breath exhaustion fade
    $env = 1.0
    if ($t -lt 0.015) {
        $env = $t / 0.015
    } elseif ($t -gt 2.3) {
        $env = [Math]::Max(0.0, 1.0 - ($t - 2.3) / 0.9)
    }

    # Combine & soft-saturate (tanh) for that ear-piercing visceral scream
    $raw = ($vocal + $breath + $thump) * $env * 1.75
    $sample = [Math]::Tanh($raw)

    $shortVal = [int16]([Math]::Max(-32767, [Math]::Min(32767, [int]($sample * 32700.0))))
    $bw.Write([int16]$shortVal)
}

$bw.Flush()
$bytes = $ms.ToArray()
[System.IO.File]::WriteAllBytes($wavPath1, $bytes)
[System.IO.File]::WriteAllBytes($wavPath2, $bytes)
$bw.Close()
$ms.Close()

Write-Output "Successfully synthesized female horror scream audio to $wavPath1 and $wavPath2"
