param()

$audioDir = "Assets\Ghost\Audio"
if (-not (Test-Path $audioDir)) {
    New-Item -ItemType Directory -Path $audioDir -Force | Out-Null
}
$wavPath = "Assets\Ghost\Audio\jumpscare_scream.wav"

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

$rnd = New-Object System.Random(12345)

for ($i = 0; $i -lt $numSamples; $i++) {
    $t = $i / [double]$sampleRate
    
    # Sub-bass impact thud
    $impact = [Math]::Exp(- $t * 4.0) * [Math]::Sin(2.0 * [Math]::PI * (65.0 + 50.0 * [Math]::Exp(- $t * 12.0)) * $t) * 0.8
    
    # Chaotic frequency modulation for shrieking scream
    $screamMod = [Math]::Sin(2.0 * [Math]::PI * 19.0 * $t) * 75.0 + [Math]::Sin(2.0 * [Math]::PI * 37.0 * $t) * 55.0
    $f1 = 980.0 + $screamMod
    $f2 = 1520.0 + $screamMod * 1.4
    $f3 = 2280.0 + $screamMod * 1.8
    $f4 = 3450.0 + $screamMod * 2.2
    
    $screech = 0.38 * [Math]::Sin(2.0 * [Math]::PI * $f1 * $t) +
               0.30 * [Math]::Sin(2.0 * [Math]::PI * $f2 * $t) +
               0.24 * [Math]::Sin(2.0 * [Math]::PI * $f3 * $t) +
               0.18 * [Math]::Sin(2.0 * [Math]::PI * $f4 * $t)

    # Harsh breath / vocal rasp noise
    $noise = ($rnd.NextDouble() * 2.0 - 1.0) * 0.28

    # Envelope
    $env = 1.0
    if ($t -lt 0.02) {
        $env = $t / 0.02
    } elseif ($t -gt 2.5) {
        $env = [Math]::Max(0.0, 1.0 - ($t - 2.5) / 0.7)
    }

    $raw = ($impact + $screech + $noise) * $env * 1.8
    $sample = [Math]::Tanh($raw)
    
    $shortVal = [int16]([Math]::Max(-32767, [Math]::Min(32767, [int]($sample * 32600.0))))
    $bw.Write([int16]$shortVal)
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($wavPath, $ms.ToArray())
$bw.Close()
$ms.Close()

Write-Output "Successfully generated $wavPath"
