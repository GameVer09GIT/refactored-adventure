param()

$objPath = "Assets\Ghost\ghost.obj"
$mtlPath = "Assets\Ghost\ghost.mtl"

# 1. Write MTL file
$mtlContent = @"
# Ghost Materials
newmtl M_GhostCloth
Ka 1.000 1.000 1.000
Kd 0.850 0.820 0.780
Ks 0.050 0.050 0.050
d 1.0
illum 2
map_Kd Textures/burlap_cloth.jpg

newmtl M_GhostEyes
Ka 0.000 0.000 0.000
Kd 0.050 0.050 0.050
Ks 0.000 0.000 0.000
d 1.0
illum 1
"@
[System.IO.File]::WriteAllText($mtlPath, $mtlContent)

# 2. Build OBJ file
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# Ghost Model - Sheet Ghost with Hollow Eyes and Jagged Hem')
[void]$sb.AppendLine('mtllib ghost.mtl')
[void]$sb.AppendLine('o Ghost')

$rings = 46
$segs = 48
$verts = New-Object System.Collections.Generic.List[string]
$uvs = New-Object System.Collections.Generic.List[string]

for ($i = 0; $i -le $rings; $i++) {
    $v = $i / [double]$rings
    
    # Height profile
    if ($v -lt 0.22) {
        $hFraction = $v / 0.22
        $y = 2.05 - 0.32 * (1.0 - [Math]::Cos($hFraction * [Math]::PI / 2.0))
        $baseR = 0.33 * [Math]::Sin($hFraction * [Math]::PI / 2.0)
    } elseif ($v -lt 0.45) {
        $f = ($v - 0.22) / (0.45 - 0.22)
        $y = 1.73 - 0.35 * $f
        $baseR = 0.33 + 0.16 * $f
    } elseif ($v -lt 0.88) {
        $f = ($v - 0.45) / (0.88 - 0.45)
        $y = 1.38 - 0.85 * $f
        $baseR = 0.49 + 0.18 * $f
    } else {
        $f = ($v - 0.88) / (1.0 - 0.88)
        $y = 0.53 - 0.25 * $f
        $baseR = 0.67 + 0.05 * $f
    }

    for ($j = 0; $j -lt $segs; $j++) {
        $u = $j / [double]$segs
        $theta = $u * 2.0 * [Math]::PI

        $xScale = 1.05 + 0.12 * [Math]::Sin([Math]::Min(1.0, $v / 0.6) * [Math]::PI)
        $zScale = 0.95

        $fold = 0.0
        if ($v -gt 0.25) {
            $foldWeight = [Math]::Min(1.0, ($v - 0.25) / 0.35)
            $fold = $foldWeight * (0.05 * [Math]::Cos(8.0 * $theta) + 0.03 * [Math]::Cos(14.0 * $theta + 0.8) + 0.02 * [Math]::Sin(4.0 * $theta))
        }

        # Eye socket indentation (front face at theta ~ PI/2)
        $eyeIndent = 0.0
        if ($v -ge 0.18 -and $v -le 0.30) {
            $eyeV = ($v - 0.24) / 0.06
            $vFactor = [Math]::Max(0.0, 1.0 - $eyeV * $eyeV)
            
            $deg = $theta * 180.0 / [Math]::PI
            $leftEyeDist = [Math]::Abs($deg - 74.0)
            $rightEyeDist = [Math]::Abs($deg - 106.0)
            
            if ($leftEyeDist -lt 12.0) {
                $hFactor = [Math]::Cos($leftEyeDist / 12.0 * [Math]::PI / 2.0)
                $eyeIndent = 0.08 * $vFactor * $hFactor
            } elseif ($rightEyeDist -lt 12.0) {
                $hFactor = [Math]::Cos($rightEyeDist / 12.0 * [Math]::PI / 2.0)
                $eyeIndent = 0.08 * $vFactor * $hFactor
            }
        }

        # Jagged bottom hem
        $yOffset = 0.0
        if ($v -ge 0.85) {
            $bottomWeight = ($v - 0.85) / 0.15
            $jagged = 0.15 * [Math]::Abs([Math]::Sin(5.0 * $theta + 0.2)) + 0.08 * [Math]::Sin(9.0 * $theta)
            $yOffset = - $bottomWeight * $jagged
        }

        $r = [Math]::Max(0.02, ($baseR + $fold - $eyeIndent))
        $px = $r * [Math]::Cos($theta) * $xScale
        $pz = $r * [Math]::Sin($theta) * $zScale
        $py = $y + $yOffset

        [void]$verts.Add(('v {0:F4} {1:F4} {2:F4}' -f $px, $py, $pz))
        [void]$uvs.Add(('vt {0:F4} {1:F4}' -f ($u * 3.0), ($v * 3.5)))
    }
}

foreach ($vert in $verts) { [void]$sb.AppendLine($vert) }
foreach ($uv in $uvs) { [void]$sb.AppendLine($uv) }

[void]$sb.AppendLine('usemtl M_GhostCloth')
[void]$sb.AppendLine('s 1')

for ($i = 0; $i -lt $rings; $i++) {
    for ($j = 0; $j -lt $segs; $j++) {
        $nextJ = ($j + 1) % $segs
        $p1 = $i * $segs + $j + 1
        $p2 = $i * $segs + $nextJ + 1
        $p3 = ($i + 1) * $segs + $nextJ + 1
        $p4 = ($i + 1) * $segs + $j + 1

        # Front face
        [void]$sb.AppendLine(('f {0}/{0} {1}/{1} {2}/{2}' -f $p1, $p2, $p3))
        [void]$sb.AppendLine(('f {0}/{0} {1}/{1} {2}/{2}' -f $p1, $p3, $p4))

        # Back face (inside cloth)
        [void]$sb.AppendLine(('f {0}/{0} {1}/{1} {2}/{2}' -f $p1, $p3, $p2))
        [void]$sb.AppendLine(('f {0}/{0} {1}/{1} {2}/{2}' -f $p1, $p4, $p3))
    }
}

# Hollow eyes mesh
$eyeVertStart = $verts.Count
$eyeVerts = New-Object System.Collections.Generic.List[string]
$eyeUVs = New-Object System.Collections.Generic.List[string]

$leftCenter = [PSCustomObject]@{ X = -0.092; Y = 1.68; Z = 0.28 }
$rightCenter = [PSCustomObject]@{ X = 0.092; Y = 1.68; Z = 0.28 }

$eyeSegs = 14
foreach ($center in @($leftCenter, $rightCenter)) {
    $cIdx = $eyeVerts.Count + $eyeVertStart + 1
    [void]$eyeVerts.Add(('v {0:F4} {1:F4} {2:F4}' -f $center.X, $center.Y, ($center.Z - 0.02)))
    [void]$eyeUVs.Add('vt 0.5 0.5')
    
    for ($k = 0; $k -lt $eyeSegs; $k++) {
        $ang = $k * 2.0 * [Math]::PI / $eyeSegs
        $ex = $center.X + 0.038 * [Math]::Cos($ang)
        $ey = $center.Y + 0.052 * [Math]::Sin($ang)
        $ez = $center.Z
        [void]$eyeVerts.Add(('v {0:F4} {1:F4} {2:F4}' -f $ex, $ey, $ez))
        [void]$eyeUVs.Add(('vt {0:F4} {1:F4}' -f (0.5 + 0.5 * [Math]::Cos($ang)), (0.5 + 0.5 * [Math]::Sin($ang))))
    }
}

foreach ($ev in $eyeVerts) { [void]$sb.AppendLine($ev) }
foreach ($euv in $eyeUVs) { [void]$sb.AppendLine($euv) }

[void]$sb.AppendLine('usemtl M_GhostEyes')
# Left eye
$lCenterIdx = $eyeVertStart + 1
for ($k = 0; $k -lt $eyeSegs; $k++) {
    $nextK = ($k + 1) % $eyeSegs
    $p1 = $lCenterIdx
    $p2 = $lCenterIdx + 1 + $k
    $p3 = $lCenterIdx + 1 + $nextK
    [void]$sb.AppendLine(('f {0}/{0} {1}/{1} {2}/{2}' -f $p1, $p2, $p3))
}

# Right eye
$rCenterIdx = $eyeVertStart + 1 + ($eyeSegs + 1)
for ($k = 0; $k -lt $eyeSegs; $k++) {
    $nextK = ($k + 1) % $eyeSegs
    $p1 = $rCenterIdx
    $p2 = $rCenterIdx + 1 + $k
    $p3 = $rCenterIdx + 1 + $nextK
    [void]$sb.AppendLine(('f {0}/{0} {1}/{1} {2}/{2}' -f $p1, $p2, $p3))
}

[System.IO.File]::WriteAllText($objPath, $sb.ToString())
Write-Output "Successfully generated $objPath with $($verts.Count + $eyeVerts.Count) vertices."
