# TeachAssist Desktop

> A modern Windows desktop application for YRDSB students to check their TeachAssist grades with advanced analytics and a polished Fluent UI.

![TeachAssist](https://img.shields.io/badge/version-3.2.0-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows_10%2B-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Microsoft Store](https://img.shields.io/badge/Microsoft_Store-Coming_Soon-0078D4)

## Features

- **Fluent Design UI** — Windows 11 native feel with WPF-UI, Mica backdrop, and dynamic theming (dark/light)
- **Auto-Login** — Opens straight to your grades, remembers credentials securely
- **Full Timetable** — See all your courses including lunch, even before marks are posted
- **Smart Course Decoding** — Automatically decodes Ontario course codes into human-readable descriptions
- **Real School Name** — Displays your actual school from TeachAssist
- **Grade Trends** — Visualize your assignment performance over time with ScottPlot charts
- **What-If Calculator** — Simulate how future assignments affect your final grade
- **Grade Goals** — Set grade targets and track your progress toward them
- **Export Reports** — Export your grades as CSV or HTML (printable as PDF)
- **Color-Coded Grades** — Instant visual feedback on your performance
- **Secure Login** — Credentials stored in Windows Credential Manager
- **Keyboard Shortcuts** — F5 to refresh, Ctrl+E to export, Ctrl+, for settings
- **Check for Updates** — Built-in update checker via GitHub Releases
- **Fast & Lightweight** — Built on .NET 10 and WPF

## Installation

### Microsoft Store (Recommended)

Available on the [Microsoft Store](https://apps.microsoft.com) — search for "TeachAssist Desktop".

### Manual

1. Download the latest `.msix` from [Releases](../../releases)
2. Double-click to install
3. Login with your YRDSB student number and password

### Building from Source

```bash
git clone https://github.com/changcheng967/TeachAssistDesktop.git
cd TeachAssistDesktop
dotnet build TeachAssistApp/TeachAssistApp.csproj -c Release
dotnet run --project TeachAssistApp/TeachAssistApp.csproj
```

## Usage

### Grade Color Coding

| Range | Color | Level |
|-------|-------|-------|
| 90%+ | Green | Level 4, A |
| 80-89% | Gold | Level 3, B |
| 70-79% | Orange | Level 2, C |
| 60-69% | Red | Level 1 |
| < 60% | Dark Red | Below expectations |

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| F5 | Refresh grades |
| Ctrl+E | Export grades |
| Ctrl+, | Open settings |
| Esc | Go back |

## Tech Stack

- **Framework:** .NET 10.0 (WPF)
- **UI Library:** WPF-UI 4.2.0 (Fluent Design)
- **Architecture:** MVVM + DI (Microsoft.Extensions.DependencyInjection)
- **MVVM Toolkit:** CommunityToolkit.Mvvm 8.4.0
- **Charts:** ScottPlot.WPF 5.1.57
- **HTML Parsing:** HtmlAgilityPack
- **Notifications:** Microsoft.Toolkit.Uwp.Notifications
- **Credential Storage:** Windows Credential Manager

## Privacy & Security

- Credentials stored ONLY locally in Windows Credential Manager
- No telemetry or data collection
- No third-party analytics
- All data stored locally on your computer
- Clear all cached data from Settings at any time

**This application is NOT affiliated with or endorsed by YRDSB or TeachAssist.**

## Changelog

### Version 3.2.0
- Auto-login on app open using saved credentials (no login screen needed)
- Login page uses real app logo instead of placeholder
- Dashboard visually dims lunch and no-mark courses for clarity
- Logout via Settings clears credentials for next launch

### Version 3.1.0
- Show all courses in timetable, including lunch blocks
- Display courses without posted marks with "No mark posted" status
- Extract real school name from TeachAssist page
- Course detail shows info banner when report isn't available
- Lunch excluded from GPA/average calculations

### Version 3.0.0
- Complete UI overhaul with WPF-UI Fluent Design (Mica backdrop, dynamic theming)
- Added grade trends visualization (ScottPlot)
- Added What-If Calculator for grade simulation
- Added Grade Goals with progress tracking
- Added CSV and HTML report export
- Added check for updates feature
- Fixed stale username on account switch
- Removed dead code and unused themes
- Production crash handler
- Microsoft Store packaging (MSIX)

### Version 2.0.0
- Dark theme redesign
- Course code decoding
- Grade goals system
- Improved card layout with color-coded accents

### Version 1.0.0
- Initial release
- Basic grade viewing

## Credits

**Developed by:** [changcheng967](https://github.com/changcheng967)

## License

This project is licensed under the MIT License.
