# generate-icons.ps1 — お守りアイコン生成スクリプト (spec v0.3追補2 §12.3/§12.4)
#
# 原版は SVG ではなくコード管理:
#   - 図形   : TokenOmamoriTool/OmamoriShape.cs の *PathData 定数 (24x24 座標系)
#   - 色     : TokenOmamoriTool/OmamoriTheme.cs の *Hex 定数
# 本スクリプトは両ファイルから正規表現で値を抽出し、WPF (PresentationCore) で描画して
# マルチサイズ .ico (16/32/48/256) を生成する。外部ツール不要 (Windows PowerShell 5.1 + .NET Framework の WPF)。
#
# 生成物:
#   TokenOmamoriTool/Assets/omamori.ico          … 袋=OmamoriHealthy(緑)。アプリアイコン兼トレイ正常時
#   TokenOmamoriTool/Assets/omamori-warning.ico  … 袋=OmamoriWarning(琥珀)。トレイ警告時
#
# 実行方法 (リポジトリルートで):
#   powershell -ExecutionPolicy Bypass -File tools\generate-icons.ps1
#   (-PreviewDir <dir> を付けると確認用 PNG も出力: 各サイズ + 16px の10倍拡大)
#
# 図形・色を変えたら再実行して .ico をコミットし直すこと。
param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$PreviewDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$shapeSource = Get-Content (Join-Path $RepoRoot 'TokenOmamoriTool\OmamoriShape.cs') -Raw
$themeSource = Get-Content (Join-Path $RepoRoot 'TokenOmamoriTool\OmamoriTheme.cs') -Raw
$assetsDir = Join-Path $RepoRoot 'TokenOmamoriTool\Assets'
New-Item -ItemType Directory -Force $assetsDir | Out-Null

function Get-CsConst([string]$source, [string]$name) {
    if ($source -match ('const\s+string\s+' + [regex]::Escape($name) + '\s*=\s*"([^"]+)"')) { return $Matches[1] }
    throw "Constant '$name' not found — did OmamoriShape.cs/OmamoriTheme.cs change format?"
}

$pouchPath = Get-CsConst $shapeSource 'PouchPathData'
$loopPath  = Get-CsConst $shapeSource 'LoopPathData'
$knotPath  = Get-CsConst $shapeSource 'KnotPathData'
$healthyHex = Get-CsConst $themeSource 'HealthyHex'
$warningHex = Get-CsConst $themeSource 'WarningHex'
$accentHex  = Get-CsConst $themeSource 'AccentHex'
$canvasSize = 24.0
if ($shapeSource -match 'CanvasSize\s*=\s*([0-9.]+)') { $canvasSize = [double]$Matches[1] }

function New-Brush([string]$hex) {
    $brush = New-Object System.Windows.Media.SolidColorBrush(
        [System.Windows.Media.ColorConverter]::ConvertFromString($hex))
    $brush.Freeze()
    return $brush
}

$accentBrush = New-Brush $accentHex

# 1辺 $size px のお守りを描画して RenderTargetBitmap を返す。
# §12.3: 16px では紐の結び目(knot)などの細部を省略した抽象形にする。
function Render-Omamori([int]$size, [System.Windows.Media.SolidColorBrush]$pouchBrush) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    $dc = $visual.RenderOpen()
    $scale = $size / $canvasSize
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform($scale, $scale)))
    $dc.DrawGeometry($pouchBrush, $null, [System.Windows.Media.Geometry]::Parse($pouchPath))
    if ($size -ge 32) {
        $dc.DrawGeometry($accentBrush, $null, [System.Windows.Media.Geometry]::Parse($knotPath))
    }
    $dc.DrawGeometry($accentBrush, $null, [System.Windows.Media.Geometry]::Parse($loopPath))
    $dc.Pop()
    $dc.Close()

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
        $size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    return $bitmap
}

function Get-PngBytes([System.Windows.Media.Imaging.BitmapSource]$bitmap) {
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = New-Object System.IO.MemoryStream
    $encoder.Save($stream)
    return ,$stream.ToArray()   # 先頭の , で byte[] のまま返す (PSの配列展開防止)
}

# 32bpp BGRA の ICO 用 DIB (BITMAPINFOHEADER + ボトムアップXOR + 空ANDマスク) を組み立てる。
function Get-DibBytes([System.Windows.Media.Imaging.BitmapSource]$bitmap) {
    $converted = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap(
        $bitmap, [System.Windows.Media.PixelFormats]::Bgra32, $null, 0)
    $w = $converted.PixelWidth
    $h = $converted.PixelHeight
    $stride = $w * 4
    $pixels = New-Object byte[] ($stride * $h)
    $converted.CopyPixels($pixels, $stride, 0)

    $maskStride = [int]([math]::Ceiling($w / 32.0)) * 4
    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)
    $writer.Write([int]40)              # biSize
    $writer.Write([int]$w)              # biWidth
    $writer.Write([int]($h * 2))        # biHeight (XOR + AND)
    $writer.Write([int16]1)             # biPlanes
    $writer.Write([int16]32)            # biBitCount
    $writer.Write([int]0)               # biCompression (BI_RGB)
    $writer.Write([int]($stride * $h + $maskStride * $h))  # biSizeImage
    $writer.Write([int]0); $writer.Write([int]0)           # biXPelsPerMeter / biYPelsPerMeter
    $writer.Write([int]0); $writer.Write([int]0)           # biClrUsed / biClrImportant
    for ($row = $h - 1; $row -ge 0; $row--) {
        $writer.Write($pixels, $row * $stride, $stride)    # bottom-up
    }
    $writer.Write((New-Object byte[] ($maskStride * $h)))  # AND mask: 全ピクセル可視 (アルファで抜く)
    $writer.Flush()
    return ,$stream.ToArray()   # 先頭の , で byte[] のまま返す (PSの配列展開防止)
}

# entries: @{ Size = <int>; Bytes = <byte[]> } の配列。256 エントリは PNG、他は DIB を渡す。
function Write-Ico([string]$path, [object[]]$entries) {
    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)
    $writer.Write([int16]0)                 # reserved
    $writer.Write([int16]1)                 # type: icon
    $writer.Write([int16]$entries.Count)
    $offset = 6 + 16 * $entries.Count
    foreach ($entry in $entries) {
        $dim = if ($entry.Size -ge 256) { 0 } else { $entry.Size }
        $writer.Write([byte]$dim)            # width (0 = 256)
        $writer.Write([byte]$dim)            # height
        $writer.Write([byte]0)               # palette colors
        $writer.Write([byte]0)               # reserved
        $writer.Write([int16]1)              # planes
        $writer.Write([int16]32)             # bit count
        $writer.Write([int]$entry.Bytes.Length)
        $writer.Write([int]$offset)
        $offset += $entry.Bytes.Length
    }
    foreach ($entry in $entries) { $writer.Write([byte[]]$entry.Bytes) }
    $writer.Flush()
    [System.IO.File]::WriteAllBytes($path, $stream.ToArray())
}

# 16px 判別性チェック用: ニアレストネイバーで拡大した PNG を書き出す。
function Write-EnlargedPreview([System.Windows.Media.Imaging.BitmapSource]$bitmap, [int]$factor, [string]$path) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    [System.Windows.Media.RenderOptions]::SetBitmapScalingMode(
        $visual, [System.Windows.Media.BitmapScalingMode]::NearestNeighbor)
    $target = $bitmap.PixelWidth * $factor
    $dc = $visual.RenderOpen()
    $dc.DrawImage($bitmap, (New-Object System.Windows.Rect(0, 0, $target, $target)))
    $dc.Close()
    $enlarged = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
        $target, $target, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $enlarged.Render($visual)
    [System.IO.File]::WriteAllBytes($path, (Get-PngBytes $enlarged))
}

$variants = @(
    @{ Name = 'omamori';         Brush = (New-Brush $healthyHex) },
    @{ Name = 'omamori-warning'; Brush = (New-Brush $warningHex) }
)
foreach ($variant in $variants) {
    $entries = @()
    $bitmaps = @{}
    foreach ($size in 16, 32, 48, 256) {
        $bitmap = Render-Omamori $size $variant.Brush
        $bitmaps[$size] = $bitmap
        $bytes = if ($size -eq 256) { Get-PngBytes $bitmap } else { Get-DibBytes $bitmap }
        $entries += @{ Size = $size; Bytes = $bytes }
    }
    $icoPath = Join-Path $assetsDir ($variant.Name + '.ico')
    Write-Ico $icoPath $entries
    Write-Host "wrote $icoPath"

    if ($PreviewDir) {
        New-Item -ItemType Directory -Force $PreviewDir | Out-Null
        foreach ($size in 16, 32, 48, 256) {
            $pngPath = Join-Path $PreviewDir ("{0}-{1}.png" -f $variant.Name, $size)
            [System.IO.File]::WriteAllBytes($pngPath, (Get-PngBytes $bitmaps[$size]))
        }
        Write-EnlargedPreview $bitmaps[16] 10 (Join-Path $PreviewDir ($variant.Name + '-16-x10.png'))
        Write-Host "wrote previews to $PreviewDir"
    }
}
