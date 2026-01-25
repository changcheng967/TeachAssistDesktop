# TeachAssist Desktop Application

> A modern Windows desktop application for YRDSB students to check their TeachAssist grades with advanced analytics and a beautiful UI.

![TeachAssist](https://img.shields.io/badge/version-2.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- Modern Dark UI - Clean, professional interface
- Smart Course Decoding - Automatically decodes Ontario course codes
- Grade Goals - Set grade goals and track your progress
- Grade Trends - Visualize your assignment performance over time
- What-If Calculator - Simulate how future assignments affect your grade
- Color-Coded Grades - Instant visual feedback on your performance
- Secure Login - Credentials stored securely using Windows Credential Manager
- Fast & Lightweight - Built with .NET 9 and WPF

## Getting Started

### Prerequisites

- Windows 10 or Windows 11 (64-bit)
- Approximately 150 MB free disk space

### Installation

1. Download the latest release from [Releases](../../releases)
2. Extract `TeachAssistApp-Release.zip`
3. Run `TeachAssistApp.exe`
4. Login with your YRDSB credentials

**Demo Mode:** Use username `demo` to test with sample data.

### Building from Source

```bash
# Clone the repository
git clone https://github.com/changcheng967/TeachAssist.git

# Navigate to the project directory
cd TeachAssist

# Open in Visual Studio 2022
# Or build using CLI:
dotnet build --configuration Release

# Run the application
dotnet run
```

## Usage

### Course Code Decoding

The application automatically decodes Ontario course codes:

| Code | Decoded |
|------|---------|
| ICS4U1-03 | Computer Science • Grade 12 University |
| MHF4U1-02 | Advanced Functions • Grade 12 University |
| CGC1W1-08 | Geography of Canada • Grade 9 Destreamed |
| ESLEO1-02 | ESL • Level E |

### Grade Color Coding

- Green (90%+) - Level 4, A range
- Gold (80-89%) - Level 3, B range
- Orange (70-79%) - Level 2, C range
- Red (60-69%) - Level 1, passing
- Dark Red (Below 60%) - Below expectations
- Gray (N/A) - No marks yet

## Tech Stack

- **Framework:** .NET 9.0
- **UI:** WPF (Windows Presentation Foundation)
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Language:** C#
- **HTML Parsing:** HtmlAgilityPack
- **Credential Storage:** Windows Credential Manager

## Project Structure

```
TeachAssistApp/
├── Models/           # Data models (Course, Assignment, etc.)
├── ViewModels/       # MVVM ViewModels
├── Views/            # XAML views
├── Services/         # Business logic (TeachAssist, Credentials)
├── Helpers/          # Utility classes (CourseCodeParser, Converters)
├── Converters/       # WPF value converters
└── Resources/        # Styles and templates
```

## Privacy & Security

- Credentials stored ONLY locally in Windows Credential Manager
- No telemetry or data collection
- No internet connection required after login (cached data)
- All data stored locally on your computer
- Clear all data option in Settings

**This application is NOT affiliated with or endorsed by YRDSB or TeachAssist.**

## Troubleshooting

### Login Issues
- Verify your YRDSB student number and password
- Ensure `ta.yrdsb.ca` is accessible
- Try the demo account (username: `demo`)

### Grades Not Loading
- Check your internet connection
- Click the "Refresh" button
- Clear cached data in Settings

## Changelog

### Version 2.0.0
- Complete UI/UX redesign with premium dark theme
- Course code decoding for Ontario curriculum
- Grade goals system with progress tracking
- Enhanced card layout with color-coded accents
- Improved login page
- Fixed text overlap issues
- Added room and block info to cards
- Fixed decimal precision in stats

### Version 1.0.0
- Initial release
- Basic grade viewing
- Dashboard with course cards

## Credits

**Developed by:** [changcheng967](https://github.com/changcheng967)

**Built with:**
- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [HtmlAgilityPack](https://html-agility-pack.net/)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
