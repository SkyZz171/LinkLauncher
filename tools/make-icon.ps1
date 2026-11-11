# Genere src\LinkLauncher.App\Assets\linklauncher.ico (marque "L" sur fond accentue).
# Script ASCII uniquement : PowerShell 5.1 lit les fichiers sans BOM en ANSI.
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\src\LinkLauncher.App\Assets\linklauncher.ico"),
    [int]$Size = 256
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([System.Drawing.Color]::Transparent)

# Fond arrondi avec la couleur d'accent par defaut (violet).
$radius = [int]($Size * 0.22)
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$diameter = $radius * 2
$path.AddArc(0, 0, $diameter, $diameter, 180, 90)
$path.AddArc($Size - $diameter - 1, 0, $diameter, $diameter, 270, 90)
$path.AddArc($Size - $diameter - 1, $Size - $diameter - 1, $diameter, $diameter, 0, 90)
$path.AddArc(0, $Size - $diameter - 1, $diameter, $diameter, 90, 90)
$path.CloseFigure()

$background = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 124, 92, 255))
$graphics.FillPath($background, $path)

# Marque "L".
$font = New-Object System.Drawing.Font("Segoe UI", [float]($Size * 0.55), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.DrawString("L", $font, $white, (New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)), $format)

$graphics.Dispose()
$font.Dispose()
$format.Dispose()
$white.Dispose()
$background.Dispose()
$path.Dispose()

$stream = New-Object System.IO.MemoryStream
$bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $stream.ToArray()
$stream.Dispose()
$bitmap.Dispose()

$directory = Split-Path -Parent $OutputPath
if (-not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$file = [System.IO.File]::Create($OutputPath)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([UInt16]0)        # reserved
$writer.Write([UInt16]1)        # type : icone
$writer.Write([UInt16]1)        # nombre d'images
$writer.Write([byte]0)          # largeur 256 (0 = 256)
$writer.Write([byte]0)          # hauteur 256
$writer.Write([byte]0)          # couleurs de palette
$writer.Write([byte]0)          # reserve
$writer.Write([UInt16]1)        # plans
$writer.Write([UInt16]32)       # bits par pixel
$writer.Write([UInt32]$png.Length)
$writer.Write([UInt32]22)       # offset des donnees
$writer.Write($png)
$writer.Dispose()
$file.Dispose()

Write-Host "Icone ecrite : $OutputPath ($($png.Length) octets PNG)"
