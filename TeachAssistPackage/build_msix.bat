@echo off
echo === Building TeachAssist Desktop MSIX ===

set MAKEAPPX="C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe"
set PUBLISH_DIR=TeachAssistApp\bin\Release\net10.0-windows10.0.17763\win-x64\publish
set OUTPUT=TeachAssistDesktop_3.0.0.0_x64.msix

echo.
echo Step 1: Publishing app...
dotnet publish TeachAssistApp\TeachAssistApp.csproj -c Release -r win-x64 --self-contained false
if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    exit /b 1
)

echo.
echo Step 2: Copying manifest and assets to publish directory...
copy /Y TeachAssistPackage\AppxManifest.xml %PUBLISH_DIR%\
xcopy /Y /I TeachAssistPackage\Assets %PUBLISH_DIR%\Assets\

echo.
echo Step 3: Building MSIX...
%MAKEAPPX% pack /d %PUBLISH_DIR% /p %OUTPUT%
if %ERRORLEVEL% neq 0 (
    echo MSIX build failed!
    exit /b 1
)

echo.
echo === Done! Output: %OUTPUT% ===
