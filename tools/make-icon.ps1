#requires -RunAsAdministrator
<#
    make-icon.ps1
    Generates app.ico for KiraDemorTY Optimizer  (v1.1 identity).

    Artwork: dark rounded tile with a warm inner glow, a thin orange rim and a
    large "K" painted as geometry (not a font) with a cream -> orange gradient.
    The K is deliberately light so it never reads as a black blob on the dark
    Windows taskbar. Every size is rendered at 4x and downsampled for clean
    antialiasing. Sizes 16-48 are 32bpp DIBs, 64+ are PNG.

    Usage:  powershell -ExecutionPolicy Bypass -File .\tools\make-icon.ps1
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$tileTop = [Drawing.Color]::FromArgb(255, 41, 35, 43)   # warm charcoal
$tileBot = [Drawing.Color]::FromArgb(255, 12, 11, 14)   # near black
$rim     = [Drawing.Color]::FromArgb(120, 255, 138, 30)
$rimSoft = [Drawing.Color]::FromArgb(46, 255, 220, 190)
$glow    = [Drawing.Color]::FromArgb(24, 255, 120, 20)
$kTop    = [Drawing.Color]::FromArgb(255, 255, 247, 236) # cream
$kMid    = [Drawing.Color]::FromArgb(255, 255, 196, 118)
$kBot    = [Drawing.Color]::FromArgb(255, 255, 118, 14)  # orange

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$SS    = 4

function New-RoundPath([Drawing.RectangleF]$r, [float]$rad) {
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $d = $rad * 2
    if ($d -gt $r.Width)  { $d = $r.Width }
    if ($d -gt $r.Height) { $d = $r.Height }
    $p.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $p.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $p.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $p.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-KPath([float]$s) {
    # Two strokes: a vertical stem and one continuous arm. Drawn with round
    # caps and a round join so the shape stays soft at 16 px.
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $p.AddLine([float](0.302 * $s), [float](0.238 * $s), [float](0.302 * $s), [float](0.762 * $s))
    $p.StartFigure()
    $p.AddLine([float](0.716 * $s), [float](0.226 * $s), [float](0.352 * $s), [float](0.500 * $s))
    $p.AddLine([float](0.352 * $s), [float](0.500 * $s), [float](0.716 * $s), [float](0.774 * $s))
    return $p
}

function New-Mark([int]$s) {
    $px = $s * $SS
    $big = New-Object Drawing.Bitmap $px, $px, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($big)
    $g.SmoothingMode     = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode   = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([Drawing.Color]::Transparent)

    $margin = [float]($px * 0.025)
    $rect = New-Object Drawing.RectangleF $margin, $margin, ([float]($px - 2 * $margin)), ([float]($px - 2 * $margin))
    $tile = New-RoundPath $rect ([float]($px * 0.225))

    # tile body
    $body = New-Object Drawing.Drawing2D.LinearGradientBrush $rect, $tileTop, $tileBot, 90
    $g.FillPath($body, $tile)
    $body.Dispose()

    # warm glow behind the mark, clipped to the tile
    $g.SetClip($tile)
    $gr = New-Object Drawing.RectangleF ([float]($px * 0.10)), ([float]($px * 0.12)), ([float]($px * 0.80)), ([float]($px * 0.80))
    $gp = New-Object Drawing.Drawing2D.GraphicsPath
    $gp.AddEllipse($gr)
    $pgb = New-Object Drawing.Drawing2D.PathGradientBrush $gp
    $pgb.CenterColor    = $glow
    $pgb.SurroundColors = @([Drawing.Color]::FromArgb(0, 255, 120, 20))
    $pgb.CenterPoint    = New-Object Drawing.PointF ([float]($px * 0.46)), ([float]($px * 0.50))
    $g.FillPath($pgb, $gp)
    $pgb.Dispose()
    $gp.Dispose()
    $g.ResetClip()

    # K: soft halo then gradient stroke
    $k = New-KPath $px
    $w = [float]($px * 0.108)
    $halo = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(26, 255, 128, 24)), ([float]($w * 1.55))
    $halo.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $halo.EndCap   = [Drawing.Drawing2D.LineCap]::Round
    $halo.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($halo, $k)
    $halo.Dispose()

    $kRect = New-Object Drawing.RectangleF 0, ([float]($px * 0.18)), $px, ([float]($px * 0.64))
    $kBrush = New-Object Drawing.Drawing2D.LinearGradientBrush $kRect, $kTop, $kBot, 90
    $blend = New-Object Drawing.Drawing2D.ColorBlend 3
    $blend.Colors    = @($kTop, $kMid, $kBot)
    $blend.Positions = @([float]0, [float]0.46, [float]1)
    $kBrush.InterpolationColors = $blend
    $pen = New-Object Drawing.Pen $kBrush, $w
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap   = [Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($pen, $k)
    $pen.Dispose()
    $kBrush.Dispose()
    $k.Dispose()

    # rim: orange at 1.6% of the width, plus a faint top highlight
    $rimPen = New-Object Drawing.Pen $rim, ([float]($px * 0.011))
    $rimPen.Alignment = [Drawing.Drawing2D.PenAlignment]::Inset
    $g.DrawPath($rimPen, $tile)
    $rimPen.Dispose()

    $hiRect = New-Object Drawing.RectangleF $rect.X, $rect.Y, $rect.Width, ([float]($rect.Height * 0.42))
    $hi = New-Object Drawing.Drawing2D.LinearGradientBrush $hiRect, $rimSoft, ([Drawing.Color]::FromArgb(0, 255, 220, 190)), 90
    $hiPen = New-Object Drawing.Pen $hi, ([float]($px * 0.006))
    $hiPen.Alignment = [Drawing.Drawing2D.PenAlignment]::Inset
    $g.DrawPath($hiPen, $tile)
    $hiPen.Dispose()
    $hi.Dispose()
    $tile.Dispose()
    $g.Dispose()

    # downsample to the requested size
    $bmp = New-Object Drawing.Bitmap $s, $s, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $dg = [Drawing.Graphics]::FromImage($bmp)
    $dg.InterpolationMode  = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $dg.PixelOffsetMode    = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $dg.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
    $dg.CompositingMode    = [Drawing.Drawing2D.CompositingMode]::SourceCopy
    $dg.Clear([Drawing.Color]::Transparent)
    $ia = New-Object Drawing.Imaging.ImageAttributes
    $ia.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
    $dst = New-Object Drawing.Rectangle 0, 0, $s, $s
    $dg.DrawImage($big, $dst, 0, 0, $px, $px, [Drawing.GraphicsUnit]::Pixel, $ia)
    $ia.Dispose()
    $dg.Dispose()
    $big.Dispose()
    return $bmp
}

function Get-Png([Drawing.Bitmap]$bmp) {
    $ms = New-Object IO.MemoryStream
    $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $ms.Dispose()
    return $bytes
}

function Get-Dib([Drawing.Bitmap]$bmp) {
    $s = $bmp.Width
    $ms = New-Object IO.MemoryStream
    $bw = New-Object IO.BinaryWriter $ms

    # BITMAPINFOHEADER (height doubled: XOR + AND mask)
    $bw.Write([UInt32]40)
    $bw.Write([Int32]$s)
    $bw.Write([Int32]($s * 2))
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]0)
    $bw.Write([UInt32]($s * $s * 4))
    $bw.Write([Int32]0)
    $bw.Write([Int32]0)
    $bw.Write([UInt32]0)
    $bw.Write([UInt32]0)

    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) {
            $c = $bmp.GetPixel($x, $y)
            $bw.Write([Byte]$c.B)
            $bw.Write([Byte]$c.G)
            $bw.Write([Byte]$c.R)
            $bw.Write([Byte]$c.A)
        }
    }

    # AND mask, 1bpp rows padded to 4 bytes; all zero because alpha is used
    $maskRow = [int][Math]::Ceiling($s / 32.0) * 4
    $mask = New-Object byte[] ($maskRow * $s)
    $bw.Write($mask)

    $bw.Flush()
    $bytes = $ms.ToArray()
    $bw.Dispose()
    return $bytes
}

$entries = @()
foreach ($s in $sizes) {
    $bmp = New-Mark $s
    Write-Host ("render {0} -> {1}x{2}" -f $s, $bmp.Width, $bmp.Height)
    if ($s -le 48) {
        [byte[]]$data = Get-Dib $bmp
        $entries += @{ size = $s; png = $false; data = $data }
    } else {
        [byte[]]$data = Get-Png $bmp
        $entries += @{ size = $s; png = $true; data = $data }
    }
    if ($s -eq 256) {
        $bmp.Save((Join-Path $PSScriptRoot 'preview-icon-256.png'), [Drawing.Imaging.ImageFormat]::Png)
    }
    if ($s -eq 48) {
        $bmp.Save((Join-Path $PSScriptRoot 'preview-icon-48.png'), [Drawing.Imaging.ImageFormat]::Png)
    }
    if ($s -eq 32) {
        $bmp.Save((Join-Path $PSScriptRoot 'preview-icon-32.png'), [Drawing.Imaging.ImageFormat]::Png)
    }
    if ($s -eq 16) {
        $bmp.Save((Join-Path $PSScriptRoot 'preview-icon-16.png'), [Drawing.Imaging.ImageFormat]::Png)
    }
    $bmp.Dispose()
}

$icoPath = Join-Path $PSScriptRoot '..\app.ico'
$fs = [IO.File]::Create($icoPath)
$bw = New-Object IO.BinaryWriter $fs
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$entries.Count)

$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    $bw.Write([Byte]($(if ($e.size -ge 256) { 0 } else { $e.size })))
    $bw.Write([Byte]($(if ($e.size -ge 256) { 0 } else { $e.size })))
    $bw.Write([Byte]0)      # palette colours
    $bw.Write([Byte]0)      # reserved
    $bw.Write([UInt16]1)    # planes
    $bw.Write([UInt16]32)   # bits per pixel
    $bw.Write([UInt32]$e.data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $e.data.Length
}
foreach ($e in $entries) { $bw.Write($e.data) }
$bw.Flush()
$bw.Close()
$fs.Close()

Write-Host ('Icon created: {0} ({1} KB, {2} sizes)' -f $icoPath,
    [math]::Round((Get-Item $icoPath).Length / 1KB, 1), $entries.Count) -ForegroundColor Green
Write-Host ('Previews: tools\preview-icon-256.png, tools\preview-icon-48.png') -ForegroundColor Green
