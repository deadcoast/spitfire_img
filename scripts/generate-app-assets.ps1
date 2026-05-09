$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetDirectory = Join-Path $repoRoot 'src\ImageViewer.App\Assets'
$resolvedRepoRoot = $repoRoot.TrimEnd('\') + '\'
$resolvedAssetDirectory = [System.IO.Path]::GetFullPath($assetDirectory)

if (-not $resolvedAssetDirectory.StartsWith($resolvedRepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved asset directory is outside the repository: $resolvedAssetDirectory"
}

$sourceSvg = Join-Path $assetDirectory 'spitfire-icon.svg'
if (-not (Test-Path -LiteralPath $sourceSvg)) {
    throw "Missing source icon asset: $sourceSvg"
}

New-Item -ItemType Directory -Force -Path $assetDirectory | Out-Null
Add-Type -AssemblyName System.Drawing

function New-SpitfireBitmap {
    param(
        [Parameter(Mandatory = $true)]
        [int] $Width,

        [Parameter(Mandatory = $true)]
        [int] $Height
    )

    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 16, 18, 24))

    $scale = [Math]::Min($Width, $Height) / 256.0
    $offsetX = ($Width - (256 * $scale)) / 2.0
    $offsetY = ($Height - (256 * $scale)) / 2.0

    function P([double] $X, [double] $Y) {
        [System.Drawing.PointF]::new([single]($offsetX + ($X * $scale)), [single]($offsetY + ($Y * $scale)))
    }

    $panelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 31, 35, 45))
    $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(100, 0, 0, 0))
    $emberBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 178, 59))
    $flameBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 231, 74, 50))
    $coreBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 246, 209))
    $glowPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 255, 207, 92), [single](12 * $scale))

    try {
        $graphics.FillPolygon($shadowBrush, [System.Drawing.PointF[]] @(
            (P 76 184),
            (P 196 184),
            (P 214 210),
            (P 56 210)
        ))

        $graphics.FillPolygon($panelBrush, [System.Drawing.PointF[]] @(
            (P 56 66),
            (P 186 50),
            (P 210 178),
            (P 72 194)
        ))

        $graphics.DrawLine($glowPen, (P 74 172), (P 198 70))

        $graphics.FillPolygon($flameBrush, [System.Drawing.PointF[]] @(
            (P 70 180),
            (P 122 72),
            (P 144 126),
            (P 190 54),
            (P 172 160),
            (P 214 146),
            (P 160 204)
        ))

        $graphics.FillPolygon($emberBrush, [System.Drawing.PointF[]] @(
            (P 112 182),
            (P 138 116),
            (P 154 156),
            (P 184 116),
            (P 166 194)
        ))

        $graphics.FillEllipse($coreBrush, [single]($offsetX + (96 * $scale)), [single]($offsetY + (92 * $scale)), [single](58 * $scale), [single](58 * $scale))
    }
    finally {
        $panelBrush.Dispose()
        $shadowBrush.Dispose()
        $emberBrush.Dispose()
        $flameBrush.Dispose()
        $coreBrush.Dispose()
        $glowPen.Dispose()
        $graphics.Dispose()
    }

    $bitmap
}

function New-SpitfirePngBytes {
    param(
        [Parameter(Mandatory = $true)]
        [int] $Width,

        [Parameter(Mandatory = $true)]
        [int] $Height
    )

    $bitmap = New-SpitfireBitmap -Width $Width -Height $Height
    $stream = [System.IO.MemoryStream]::new()
    try {
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $stream.ToArray()
    }
    finally {
        $stream.Dispose()
        $bitmap.Dispose()
    }
}

function New-SpitfireIcoImageBytes {
    param(
        [Parameter(Mandatory = $true)]
        [int] $Width,

        [Parameter(Mandatory = $true)]
        [int] $Height
    )

    $bitmap = New-SpitfireBitmap -Width $Width -Height $Height
    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)

    try {
        $xorBytes = $Width * $Height * 4
        $andMaskStride = [int]([Math]::Ceiling($Width / 32.0) * 4)
        $andMaskBytes = $andMaskStride * $Height

        $writer.Write([uint32]40)
        $writer.Write([int32]$Width)
        $writer.Write([int32]($Height * 2))
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]0)
        $writer.Write([uint32]$xorBytes)
        $writer.Write([int32]0)
        $writer.Write([int32]0)
        $writer.Write([uint32]0)
        $writer.Write([uint32]0)

        for ($y = $Height - 1; $y -ge 0; $y--) {
            for ($x = 0; $x -lt $Width; $x++) {
                $pixel = $bitmap.GetPixel($x, $y)
                $writer.Write([byte]$pixel.B)
                $writer.Write([byte]$pixel.G)
                $writer.Write([byte]$pixel.R)
                $writer.Write([byte]$pixel.A)
            }
        }

        for ($i = 0; $i -lt $andMaskBytes; $i++) {
            $writer.Write([byte]0)
        }

        $stream.ToArray()
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
        $bitmap.Dispose()
    }
}

function Write-SpitfirePng {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [int] $Width,

        [Parameter(Mandatory = $true)]
        [int] $Height
    )

    [System.IO.File]::WriteAllBytes($Path, (New-SpitfirePngBytes -Width $Width -Height $Height))
}

function Write-SpitfireIco {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $sizes = @(16, 32, 48, 256)
    $images = [System.Collections.Generic.List[byte[]]]::new()

    foreach ($size in $sizes) {
        $images.Add([byte[]](New-SpitfireIcoImageBytes -Width $size -Height $size))
    }

    $stream = [System.IO.File]::Create($Path)
    $writer = [System.IO.BinaryWriter]::new($stream)

    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$images.Count)

        $offset = 6 + (16 * $images.Count)

        for ($i = 0; $i -lt $images.Count; $i++) {
            $size = $sizes[$i]
            $image = $images[$i]
            $icoDimension = if ($size -eq 256) { 0 } else { $size }
            $writer.Write([byte]$icoDimension)
            $writer.Write([byte]$icoDimension)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$image.Length)
            $writer.Write([uint32]$offset)
            $offset += $image.Length
        }

        foreach ($image in $images) {
            $writer.Write([byte[]]$image)
        }
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

Write-SpitfireIco -Path (Join-Path $assetDirectory 'spitfire.ico')
Write-SpitfirePng -Path (Join-Path $assetDirectory 'Square44x44Logo.png') -Width 44 -Height 44
Write-SpitfirePng -Path (Join-Path $assetDirectory 'Square150x150Logo.png') -Width 150 -Height 150
Write-SpitfirePng -Path (Join-Path $assetDirectory 'Wide310x150Logo.png') -Width 310 -Height 150

Write-Host "Generated app assets in $assetDirectory"
