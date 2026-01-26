Add-Type -AssemblyName System.Drawing

function Resize-Image {
    param($InputPath, $OutputPath, $Width, $Height, $BackgroundColor = "#1E1E2E")

    $original = [System.Drawing.Image]::FromFile($InputPath)
    $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)

    $r = [Convert]::ToInt32($BackgroundColor.Substring(1, 2), 16)
    $g = [Convert]::ToInt32($BackgroundColor.Substring(3, 2), 16)
    $b = [Convert]::ToInt32($BackgroundColor.Substring(5, 2), 16)
    $bgColor = [System.Drawing.Color]::FromArgb($r, $g, $b)

    $gfx.Clear($bgColor)

    $scale = [Math]::Min($Width / $original.Width, $Height / $original.Height)
    $newWidth = [int]($original.Width * $scale)
    $newHeight = [int]($original.Height * $scale)
    $x = [int](($Width - $newWidth) / 2)
    $y = [int](($Height - $newHeight) / 2)

    $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gfx.DrawImage($original, $x, $y, $newWidth, $newHeight)

    $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $gfx.Dispose()
    $bmp.Dispose()
    $original.Dispose()

    Write-Host "Created: $OutputPath"
}

function Create-WideTile {
    param($LogoPath, $OutputPath)

    $original = [System.Drawing.Image]::FromFile($LogoPath)
    $bmp = New-Object System.Drawing.Bitmap(310, 150)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)

    $bgColor = [System.Drawing.Color]::FromArgb(30, 30, 46)
    $gfx.Clear($bgColor)

    $logoHeight = 110
    $scale = $logoHeight / $original.Height
    $logoWidth = [int]($original.Width * $scale)
    $logoY = [int]((150 - $logoHeight) / 2)
    $logoX = 20

    $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gfx.DrawImage($original, $logoX, $logoY, $logoWidth, $logoHeight)

    $font = New-Object System.Drawing.Font("Segoe UI Variable Display", 36)
    $textBrush = [System.Drawing.Brushes]::White
    $textPointF = 150, [int]((150 - 48) / 2)
    $gfx.DrawString("TeachAssist", $font, $textBrush, $textPointF)

    $gfx.Dispose()
    $bmp.Dispose()
    $original.Dispose()

    Write-Host "Created: $OutputPath"
}

function Create-SplashScreen {
    param($LogoPath, $OutputPath)

    $original = [System.Drawing.Image]::FromFile($LogoPath)
    $bmp = New-Object System.Drawing.Bitmap(620, 300)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)

    $bgColor = [System.Drawing.Color]::FromArgb(13, 17, 23)
    $gfx.Clear($bgColor)

    $logoSize = 120
    $scale = $logoSize / $original.Height
    $logoWidth = [int]($original.Width * $scale)
    $logoX = [int]((620 - $logoWidth) / 2)
    $logoY = 40

    $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gfx.DrawImage($original, $logoX, $logoY, $logoWidth, $logoSize)

    $fontTitle = New-Object System.Drawing.Font("Segoe UI Variable Display", 42)
    $titleBrush = [System.Drawing.Brushes]::White
    $titleSize = $gfx.MeasureString("TeachAssist", $fontTitle)
    $titleX = [int]((620 - $titleSize.Width) / 2)
    $gfx.DrawString("TeachAssist", $fontTitle, $titleBrush, $titleX, 180)

    $fontTag = New-Object System.Drawing.Font("Segoe UI", 14)
    $tagBrush = [System.Drawing.Brush]::FromArgb(139, 148, 158)
    $tagSize = $gfx.MeasureString("YRDSB Grade Viewer", $fontTag)
    $tagX = [int]((620 - $tagSize.Width) / 2)
    $gfx.DrawString("YRDSB Grade Viewer", $fontTag, $tagBrush, $tagX, 235)

    $gfx.Dispose()
    $bmp.Dispose()
    $original.Dispose()

    Write-Host "Created: $OutputPath"
}

$logoPath = "C:\Users\chang\Downloads\talogo.png"
$outputDir = "C:\Users\chang\Downloads\TA\TeachAssistApp.Package\Assets"

if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Creating Microsoft Store assets..."
Write-Host ""

Resize-Image -InputPath $logoPath -OutputPath "$outputDir\StoreLogo.png" -Width 200 -Height 200
Resize-Image -InputPath $logoPath -OutputPath "$outputDir\SmallTile.png" -Width 71 -Height 71
Resize-Image -InputPath $logoPath -OutputPath "$outputDir\MediumTile.png" -Width 150 -Height 150
Resize-Image -InputPath $logoPath -OutputPath "$outputDir\LargeTile.png" -Width 310 -Height 310
Create-WideTile -LogoPath $logoPath -OutputPath "$outputDir\WideTile.png"
Create-SplashScreen -LogoPath $logoPath -OutputPath "$outputDir\SplashScreen.png"

Write-Host ""
Write-Host "All assets created successfully!"
Write-Host "Location: $outputDir"
