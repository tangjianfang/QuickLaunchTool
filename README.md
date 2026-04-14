# QuickLaunchTool (C++)

A lightweight Windows application launcher built with pure C++ and the Win32 API. No .NET runtime, no MFC, no external dependencies — just a single native executable.

![Main Interface](./Images/screenshot-main.png)

## Features

- **Quick Launch** — Double-click to launch; right-click for a context menu
- **Icon Buttons Toolbar** — Add File, Add Folder, Import Taskbar, Delete, Settings (Segoe MDL2 Assets glyphs)
- **Real-time Search** — Filter the app list as you type
- **Multi-language** — Simplified Chinese, English, Japanese, Korean, French, German, Spanish
- **Light / Dark Theme** — Native Win10 dark title bar via DWM
- **Icon Sizes** — Large (48 px), Medium (32 px), Small (24 px)
- **Sort Modes** — By name, last modified date, or usage count (persisted across sessions)
- **Flexible Management**
  - Add individual `.exe` / `.bat` / `.cmd` files
  - Recursively scan and add an entire folder
  - Import pinned apps from the Windows Taskbar (resolves `.lnk` shortcuts via COM)
  - Ctrl+click multi-select → bulk delete
- **Window Settings** — Always-on-top, opacity (10–100 %), window position and size auto-saved
- **Zero-flicker Rendering** — Double-buffered icon grid

## System Requirements

- Windows 10 or later (x64)
- Visual Studio 2022 (for building from source)

## Building

```bat
build.bat          :: Release build (default)
build.bat Debug    :: Debug build
```

Output: `bin\Release\QuickLaunchToolCpp.exe` (no installer, no runtime required)

### Prerequisites

- [CMake 3.20+](https://cmake.org/download/)
- Visual Studio 2022 with **Desktop development with C++** workload

### Manual build

```bash
cmake -B build -G "Visual Studio 17 2022" -A x64
cmake --build build --config Release
```

## Configuration

Settings are saved automatically to:

```
%APPDATA%\QuickLaunchToolCpp\config.json
```

The file stores window geometry, sort mode, theme, icon size, opacity, language, app paths, and per-app usage counts.

## Usage

| Action | How |
|--------|-----|
| Launch app | Double-click icon |
| Select | Single-click (Ctrl+click for multi-select) |
| Context menu | Right-click icon |
| Search | Type in the search box (top-left) |
| Add file | Toolbar: Add File button |
| Add folder | Toolbar: Add Folder button (recursive scan) |
| Import from Taskbar | Toolbar: Import button |
| Delete selected | Toolbar: Delete button |
| Settings | Toolbar: Settings button (gear icon, far right) |

## Architecture

Pure C++17 / Win32 API. No external libraries.

```
Main.cpp                    Entry point (DPI awareness, COM, CommonControls)
Statics.cpp                 Static singleton members

Models/
  AppInfo.h                 App data: name, path, icon handle, use count
  AppConfig.h               User config + enums (SortMode, ThemeMode, IconSize)

Utils/
  JsonHelper.h              Hand-rolled JSON parser/builder
  ThemeManager.h            Colors, brushes, DWM dark title bar, opacity
  LocalizationManager.h     Inline string tables for 7 languages

Services/
  ConfigManager.h           Reads/writes config.json
  FileScanner.h             Folder scan + IShellLink .lnk resolution
  IconExtractor.h           SHGetImageList-based icon extraction (24/32/48 px)
  ProcessLauncher.h         ShellExecute launch / runas / open location

Controls/
  AppGrid.h / .cpp          Double-buffered scrollable icon grid
                            Posts WMG_LAUNCH / WMG_SELECT / WMG_CONTEXTMENU to parent

Forms/
  MainWindow.h / .cpp       Main window, custom-drawn MDL2 toolbar
  SettingsDialog.h / .cpp   Modal dialog via DialogBoxParam + .rc resource

Resources/
  Resource.h                All resource IDs and custom WM_APP message constants
  Resource.rc               IDD_SETTINGS dialog + DPI / CommonControls manifest
```

**Icon ownership**: `MainWindow::m_apps` owns all `HICON` handles. `AppGrid` and the filtered-index list hold raw pointers/indices only — no double-free risk.

## License

MIT
