Add-Type -AssemblyName System.Drawing

$logoPath = "C:\Users\chang\Downloads\talogo.png"
$outputDir = "C:\Users\chang\Downloads\TA\TeachAssistApp.Package\Assets"

# WideTile (310x150) - logo on left, padding
$original = [System.Drawing.Image]::FromFile($logoPath)
$bmp = New-Object System.Drawing.Bitmap(310, 150)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$bgColor = [System.Drawing.Color]::FromArgb(30, 30, 46)
$gfx.Clear($bgColor)
$logoHeight = 110
$scale = $logoHeight / $original.Height
$logoWidth = [int]($original.Width * $scale)
$logoY = [int]((150 - $logoHeight) / 2)
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfx.DrawImage($original, 20, $logoY, $logoWidth, $logoHeight)
$bmp.Save("$outputDir\WideTile.png", [System.Drawing.Imaging.ImageFormat]::Png)
$gfx.Dispose()
$bmp.Dispose()

# SplashScreen (620x300) - centered logo
$bmp2 = New-Object System.Drawing.Bitmap(620, 300)
$gfx2 = [System.Drawing.Graphics]::FromImage($bmp2)
$bgColor2 = [System.Drawing.Color]::FromArgb(13, 17, 23)
$gfx2.Clear($bgColor2)
$logoSize = 120
$scale2 = $logoSize / $original.Height
$logoWidth2 = [int]($original.Width * $scale2)
$logoX = [int]((620 - $logoWidth2) / 2)
$gfx2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfx2.DrawImage($original, $logoX, 40, $logoWidth2, $logoSize)
$bmp2.Save("$outputDir\SplashScreen.png", [System.Drawing.Imaging.ImageFormat]::Png)
$gfx2.Dispose()
$bmp2.Dispose()
$original.Dispose()

Write-Host "WideTile.png and SplashScreen.png created successfully"
