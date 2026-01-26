Add-Type -AssemblyName System.Drawing

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

    $font = New-Object System.Drawing.Font("Arial", 32)
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $textPointF = 150, 60
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

    $fontTitle = New-Object System.Drawing.Font("Arial", 40)
    $titleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $titleSize = $gfx.MeasureString("TeachAssist", $fontTitle)
    $titleX = [int]((620 - $titleSize.Width) / 2)
    $gfx.DrawString("TeachAssist", $fontTitle, $titleBrush, $titleX, 185)

    $fontTag = New-Object System.Drawing.Font("Arial", 14)
    $tagBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(139, 148, 158))
    $tagSize = $gfx.MeasureString("YRDSB Grade Viewer", $fontTag)
    $tagX = [int]((620 - $tagSize.Width) / 2)
    $gfx.DrawString("YRDSB Grade Viewer", $fontTag, $tagBrush, $tagX, 240)

    $gfx.Dispose()
    $bmp.Dispose()
    $original.Dispose()

    Write-Host "Created: $OutputPath"
}

$logoPath = "C:\Users\chang\Downloads\talogo.png"
$outputDir = "C:\Users\chang\Downloads\TA\TeachAssistApp.Package\Assets"

Write-Host "Creating WideTile and SplashScreen..."
Write-Host ""

Create-WideTile -LogoPath $logoPath -OutputPath "$outputDir\WideTile.png"
Create-SplashScreen -LogoPath $logoPath -OutputPath "$outputDir\SplashScreen.png"

Write-Host ""
Write-Host "Complete!"
