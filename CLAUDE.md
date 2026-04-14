# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

QuickLaunchTool is a lightweight Windows application launcher built with **pure C++ and Win32 API** (no .NET, no MFC, no external libraries). C++17, requires **Windows 10+**.

> The README.md describes a legacy .NET/WinForms version. Ignore it for architecture details.

## Building

```bat
build.bat          # Release (default)
build.bat Debug    # Debug
```

This runs CMake configure (VS 2022, x64) then `cmake --build`. Output:
- `bin\Release\QuickLaunchToolCpp.exe`
- `bin\Debug\QuickLaunchToolCpp.exe`

Manual CMake:
```bash
cmake -B build -G "Visual Studio 17 2022" -A x64
cmake --build build --config Release
```

Linked libraries: `comctl32`, `shell32`, `ole32`, `shlwapi`, `dwmapi`, `uxtheme`, `comdlg32`

There are no tests.

## Architecture

### File Layout

```
Main.cpp                  # Entry point: DPI, COM, CommonControls, MainWindow
Statics.cpp               # Static member definitions for singletons
Models/
  AppInfo.h               # struct AppInfo  (name, path, hIcon, useCount, isSelected)
  AppConfig.h             # struct AppConfig + enums (SortMode, ThemeMode, IconSize)
Utils/
  JsonHelper.h            # Header-only JSON parse/build (no library)
  ThemeManager.h          # Static color/brush accessors, DWM dark title bar, opacity
  LocalizationManager.h   # Singleton, 7-language inline string tables keyed by enum
Services/
  ConfigManager.h         # Singleton, %APPDATA%\QuickLaunchToolCpp\config.json
  FileScanner.h           # ScanFolder (recursive .exe/.bat/.cmd), GetTaskbarPinnedApps
  IconExtractor.h         # SHGetImageList-based icon extraction at 24/32/48px
  ProcessLauncher.h       # Launch, LaunchAsAdmin, OpenLocation, BaseName
Controls/
  AppGrid.h / .cpp        # Scrollable icon-grid child window (double-buffered)
Forms/
  MainWindow.h / .cpp     # Main window + custom toolbar (MDL2 icon buttons)
  SettingsDialog.h / .cpp # Modal dialog via DialogBoxParam + IDD_SETTINGS resource
Resources/
  Resource.h              # All resource IDs and WMG_* custom message constants
  Resource.rc             # IDD_SETTINGS dialog, DPI/CommonControls manifest
```

### Window Structure

- **Main window** (`QLT_MainWindow`): owns toolbar strip + AppGrid
- **Toolbar** (`QLT_Toolbar`): custom-drawn strip with 5 MDL2 icon buttons + EDIT search box
- **AppGrid** (`QLT_AppGrid`): double-buffered scrollable grid; sends `WMG_LAUNCH`, `WMG_SELECT`, `WMG_CONTEXTMENU` to parent via `PostMessage`
- **SettingsDialog**: standard `DialogBoxParam` modal dialog (no custom message loop)

### Data Flow

`MainWindow` owns two structures:
- `m_apps` — `vector<AppInfo>`, **owns all HICONs** (destroyed in destructor / `LoadApps`)
- `m_filtered` — `vector<int>` (indices into `m_apps`)

`AppGrid` receives **const pointers** to both; call `AppGrid::Update()` after any change.
`filteredApps` never holds HICONs — eliminates the dangling-handle problem.

### Singleton Services

Both use `new`/`delete` with `DestroyInstance()` called from `Main.cpp`:
- **`ConfigManager`**: `Config()` returns `AppConfig&`. `AddPath`/`RemovePath`/`IncrementUseCount` auto-save. `useCountMap` keyed by lowercase path.
- **`LocalizationManager`**: `Get(Key)` returns `const wstring&`. Adding a string requires updating the enum and all 7 language blocks in `Load()`.

### Key Patterns

- All strings: `std::wstring` / `wchar_t`; project compiles with `/utf-8`; string literals use `\uXXXX` escapes
- Icon sizes: Large=48px, Medium=32px, Small=24px
- Toolbar icon font: **Segoe MDL2 Assets** (Win10+); glyphs drawn via `DrawText` into button zones
- Settings dialog labels set at runtime in `WM_INITDIALOG` (supports localization)
- `RefreshMainWindow()` is a free function defined in `MainWindow.cpp`; `SettingsDialog.cpp` calls it via `extern` declaration to avoid circular header includes
- Dark mode: `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE=20)` + `WM_CTLCOLOR*` handlers
