# MSIX Packaging Guide for Microsoft Store

Creating an MSIX package for the Microsoft Store requires Visual Studio 2022 and proper app assets.

## Prerequisites

1. **Visual Studio 2022** (17.8 or later)
   - Install "Windows Application Packaging Project" workload
   - Install ".NET Desktop Development" workload

2. **App Store Account**
   - Microsoft Developer account
   - Registered app name in Microsoft Store

## Step-by-Step Guide

### Step 1: Create Assets

Create these PNG images (required sizes):

**Logo Assets:**
- `StoreLogo.png` - 200x200px (Store listing)
- `SmallTile.png` - 71x71px (Small tile)
- `MediumTile.png` - 150x150px (Medium tile)
- `LargeTile.png` - 310x310px (Large tile)
- `WideTile.png` - 310x150px (Wide tile)
- `SplashScreen.png` - 620x300px (Splash screen)

**Design Guidelines:**
- Use the app's logo/icon (TA or TeachAssist)
- Background: #0D1117 (dark theme)
- Accent color: #FF58A6FF (blue)
- Keep text minimal and readable at small sizes

### Step 2: Create Packaging Project in Visual Studio

1. Open Visual Studio 2022
2. Open `TeachAssist.sln`
3. Right-click Solution → Add → New Project
4. Search for "Windows Application Packaging Project"
5. Name it `TeachAssistApp.Package`
6. Click "Next" → "Create"

### Step 3: Configure Packaging Project

1. In the wizard, select:
   - **Application:** TeachAssistApp
   - **Min Version:** Windows 10 version 1809 (build 17763)
   - **Target Version:** Windows 10 version 2004 or later
   - Click "Finish"

2. The wizard will create:
   - `Package.appxmanifest`
   - `Assets/` folder with placeholders

### Step 4: Replace Assets

Replace the placeholder assets in `TeachAssistApp.Package\Assets\` with your custom images.

### Step 5: Update Package.appxmanifest

Edit `Package.appxmanifest` with these settings:

```xml
<Identity Name="52838TeachAssist.Desktop"
          Publisher="YOUR_PUBLISHER_ID"
          Version="2.0.0.0" />

<Properties>
  <DisplayName>TeachAssist Desktop</DisplayName>
  <PublisherDisplayName>Your Name</PublisherDisplayName>
  <Publisher>Your Publisher ID</Publisher>
  <Logo>Assets\StoreLogo.png</Logo>
  <Description>A modern Windows desktop app for YRDSB students to check TeachAssist grades.</Description>

  <Features>
    <rescap:Capability Name="runFullTrust"/>
  </Features>
</Properties>
```

### Step 6: Build MSIX

**In Visual Studio:**
1. Set configuration to `Release`
2. Set platform to `x64`
3. Build → Build Solution (or Ctrl+Shift+B)
4. Output: `TeachAssistApp.Package\bin\x64\Release\TeachAssistApp.Package_2.0.0.0_x64_Test.msix`

**Or using command line:**
```bash
cd "C:\Users\chang\Downloads\TA"
dotnet build TeachAssist.sln -c Release -p:Platform=x64
```

### Step 7: Test the MSIX

1. Double-click `TeachAssistApp.Package_2.0.0.0_x64_Test.msix`
2. Click "Install" to test the app
3. Verify it runs correctly
4. Check that all features work

### Step 8: Submit to Microsoft Store

1. Go to [Partner Center](https://partner.microsoft.com/dashboard)
2. Create a new app submission
3. Upload the MSIX file
4. Fill in:
   - App name: "TeachAssist Desktop"
   - Description: (use README.md content)
   - Screenshots: (upload app screenshots)
   - Age rating: Calculate in Partner Center
   - Pricing: Free or paid
   - Availability: Select countries
5. Submit for certification

## Quick Alternative: Side-load MSIX

If you want to distribute MSIX without the Store:

1. Build the MSIX as above
2. Distribute the `.msix` file
3. Users double-click to install
4. No developer account required

## Troubleshooting

### Build Errors

**"Assets missing"**: Make sure all 6 PNG files exist in `Assets/` folder

**"Certificate error"**: For testing, Visual Studio creates a temporary certificate automatically

**"Publisher ID"**: Use your own Publisher ID from Partner Center or leave as-is for testing

### App Submission Issues

**"Rejected - Missing capabilities"**: Ensure `<rescap:Capability Name="runFullTrust"/>` is in manifest

**"Rejected - Incomplete metadata"**: Fill all required fields in Partner Center

## Files Created

The packaging structure should look like:

```
TeachAssist/
├── TeachAssist.sln
├── TeachAssistApp/
│   └── (source code)
└── TeachAssistApp.Package/
    ├── TeachAssistApp.Package.wapproj
    ├── Package.appxmanifest
    ├── Package.StoreAssociation.xml
    └── Assets/
        ├── StoreLogo.png (200x200)
        ├── SmallTile.png (71x71)
        ├── MediumTile.png (150x150)
        ├── LargeTile.png (310x310)
        ├── WideTile.png (310x150)
        └── SplashScreen.png (620x300)
```

## Next Steps

1. Create the 6 required PNG images (can use free tools like Paint.NET, GIMP, or Figma)
2. Open solution in Visual Studio 2022
3. Add Windows Application Packaging Project
4. Replace assets
5. Build MSIX
6. Test by double-clicking the `.msix` file
7. Submit to Microsoft Store

---

**Note**: The MSIX packaging I've set up here provides the basic structure. For actual Store submission, you'll need to complete it in Visual Studio 2022 with proper assets.
