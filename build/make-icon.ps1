# Generates src/ePad/ePad.ico: a white note page on a blue rounded square,
# rendered at every size Windows uses. Re-run only if you want to change the icon.
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$out = Join-Path $PSScriptRoot '..\src\ePad\ePad.ico'
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256

function RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function Render([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'

    # Blue rounded-square background with a subtle vertical gradient.
    $bg = RoundedRect 0 0 ($s - 0.5) ($s - 0.5) ([Math]::Max(2, $s * 0.2))
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, $s), ([System.Drawing.Color]::FromArgb(43, 127, 232)), ([System.Drawing.Color]::FromArgb(16, 78, 168))
    $g.FillPath($grad, $bg)

    # White page with a folded top-right corner.
    $l = $s * 0.24; $t = $s * 0.15; $r = $s * 0.76; $b = $s * 0.85; $fold = $s * 0.16
    $page = New-Object System.Drawing.Drawing2D.GraphicsPath
    $page.AddPolygon([System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF $l, $t),
        (New-Object System.Drawing.PointF ($r - $fold), $t),
        (New-Object System.Drawing.PointF $r, ($t + $fold)),
        (New-Object System.Drawing.PointF $r, $b),
        (New-Object System.Drawing.PointF $l, $b)))
    $g.FillPath([System.Drawing.Brushes]::White, $page)

    # The fold itself, slightly shaded.
    $foldBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(200, 214, 236))
    $g.FillPolygon($foldBrush, [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF ($r - $fold), $t),
        (New-Object System.Drawing.PointF ($r - $fold), ($t + $fold)),
        (New-Object System.Drawing.PointF $r, ($t + $fold))))

    # Text lines (fewer at small sizes so they stay distinct).
    $lineCount = if ($s -lt 24) { 2 } elseif ($s -lt 48) { 3 } else { 4 }
    $thick = [Math]::Max(1, [Math]::Round($s * 0.045))
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(43, 127, 232)), $thick
    $top = $t + $fold + $s * 0.08
    $gap = ($b - $s * 0.1 - $top) / [Math]::Max(1, $lineCount - 1)
    for ($i = 0; $i -lt $lineCount; $i++) {
        $y = [Math]::Round($top + $i * $gap) + 0.5
        $x2 = if ($i -eq $lineCount - 1) { $l + ($r - $l) * 0.6 } else { $r - $s * 0.09 }
        $g.DrawLine($pen, [float]($l + $s * 0.09), [float]$y, [float]$x2, [float]$y)
    }

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output -NoEnumerate ($ms.ToArray())
}

# ICO container with PNG-compressed images (supported since Windows Vista).
$images = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($s in $sizes) { $images.Add([byte[]](Render $s)) }
$fs = [System.IO.File]::Create($out)
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Close()
"Wrote $out"
