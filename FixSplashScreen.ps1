Add-Type -AssemblyName System.Drawing

$wideBannerPath = "C:\Users\chang\Downloads\tawidebanner.png"
$outputPath = "C:\Users\chang\Downloads\TA\TeachAssistApp.Package\Assets\SplashScreen.png"

Write-Host "Creating SplashScreen.png from wide banner..."
Write-Host "Source: $wideBannerPath"
Write-Host "Output: $outputPath"
Write-Host ""

# Load the wide banner
$original = [System.Drawing.Image]::FromFile($wideBannerPath)

Write-Host "Original image size: $($original.Width) x $($original.Height)"

# Create SplashScreen canvas (620x300)
$bmp = New-Object System.Drawing.Bitmap(620, 300)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)

# Set dark background
$bgColor = [System.Drawing.Color]::FromArgb(13, 17, 23)
$gfx.Clear($bgColor)

# Calculate scaling to fit the wide banner
$padding = 20
$availableWidth = 620 - (2 * $padding)
$scale = $availableWidth / $original.Width
$newWidth = [int]($original.Width * $scale)
$newHeight = [int]($original.Height * $scale)

# Center the banner vertically
$x = $padding
$y = [int]((300 - $newHeight) / 2)

Write-Host "Scaled size: $newWidth x $newHeight"
Write-Host "Position: $x, $y"

# Draw the wide banner with high quality
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfx.DrawImage($original, $x, $y, $newWidth, $newHeight)

# Save the result
$bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$gfx.Dispose()
$bmp.Dispose()
$original.Dispose()

Write-Host ""
Write-Host "Successfully created SplashScreen.png using the wide banner!"
Write-Host "Location: $outputPath"
