# Release Notes - Version 2.0.0

## 🎉 Major Update - Complete UI/UX Redesign

This is a major release featuring a complete redesign of the TeachAssist Desktop Application with a modern dark theme, smart course code decoding, and enhanced analytics features.

---

## ✨ What's New

### 🎨 Complete UI/UX Redesign
- **Premium Dark Theme** - Modern interface inspired by GitHub's design language
- **Enhanced Card Layout** - Larger cards (320x250px) with better spacing
- **Color-Coded Top Borders** - 4px accent bars showing grade performance at a glance
- **Improved Typography** - Segoe UI Variable Display font for better readability
- **Glassmorphism Effects** - Subtle shadows and glows throughout the UI

### 🧠 Smart Course Code Decoding
Ontario curriculum course codes are now automatically decoded:
- `ICS4U1-03` → **Computer Science • Grade 12 University**
- `MHF4U1-02` → **Advanced Functions • Grade 12 University**
- `CGC1W1-08` → **Geography of Canada • Grade 9 Destreamed**
- `ESLEO1-02` → **ESL • Level E**

### 🎯 Grade Goals System
- Set custom grade goals (90%, 85%, 80%, etc.)
- Visual progress bar showing progress toward goal
- Motivational messages based on performance
- Goals persist between sessions

### 📊 Enhanced Analytics
- **Grade Trends** - Visualize assignment performance over time
- **What-If Calculator** - Simulate how future assignments affect your grade
- **Decimal Precision** - Header stats now show one decimal place (e.g., 90.5%)

### 🎨 Visual Improvements
- **Grade Color Coding:**
  - 🟢 Green (90%+) - Level 4, A range
  - 🟡 Gold (80-89%) - Level 3, B range
  - 🟠 Orange (70-79%) - Level 2, C range
  - 🔴 Red (60-69%) - Level 1, passing
  - ⚫ Dark Red (Below 60%) - Below expectations
  - ⚪ Gray (N/A) - No marks yet

- **Enhanced Cards:**
  - Course code (small, muted gray)
  - Decoded course name (prominent)
  - Large grade percentage (52px)
  - Full level text display (no cut off)
  - Letter grade badges (A+, B+, A-)
  - Room and Block info in footer

---

## 🐛 Bug Fixes

### Fixed Issues
- ✅ Level text no longer cut off - full text "Level 4+ (Excellent!)" now displays
- ✅ Letter grade badges now visible on all cards
- ✅ Room and Block information added back to cards
- ✅ Login page icons fixed (no more broken characters)
- ✅ Overlapping elements in course cards resolved
- ✅ Footer no longer overlaps letter badges
- ✅ Cards properly scrollable in viewport

---

## 🔧 Technical Improvements

### Performance
- Optimized card rendering
- Improved loading states with skeleton screens
- Enhanced error handling

### Code Quality
- Added `CourseCodeParser` helper class for Ontario curriculum
- Improved MVVM architecture
- Better separation of concerns
- Comprehensive error handling

---

## 📦 Installation

### System Requirements
- **OS:** Windows 10 or Windows 11 (64-bit)
- **Disk Space:** 150 MB free space
- **Runtime:** Self-contained (no .NET installation required)

### Installation Steps
1. Download `TeachAssistApp-Release.zip` (61 MB)
2. Extract to any folder
3. Run `TeachAssistApp.exe`
4. Login with YRDSB credentials

**Demo Mode:** Use username `demo` to test with sample data

---

## 📚 Documentation

- **README.md** - Comprehensive user guide
- **Built-in Help** - Tips and instructions throughout the app
- **Settings** - Clear data, logout, and theme options

---

## 🔒 Privacy & Security

- ✅ Credentials stored ONLY in Windows Credential Manager
- ✅ No telemetry or data collection
- ✅ Works offline after login (cached data)
- ✅ All data stored locally

---

## 🙏 Acknowledgments

- Built with [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- Uses [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- Uses [HtmlAgilityPack](https://html-agility-pack.net/)

---

## 📝 Known Issues

None! This release has been thoroughly tested.

---

## 🚀 Upcoming Features

Potential future enhancements:
- Grade history charts
- Multiple account support
- Export to CSV
- System tray notifications
- Semester comparisons

---

## 📞 Support

For issues or questions:
- Check the Troubleshooting section in README.md
- Verify `ta.yrdsb.ca` is accessible
- Try the demo account (username: `demo`)

---

**Version:** 2.0.0
**Release Date:** January 25, 2026
**Developer:** [changcheng967](https://github.com/changcheng967)

---

⭐ **If you find this app helpful, please star it on GitHub!** ⭐
