# QuickLaunchTool

A lightweight and efficient application launcher for Windows that helps you quickly launch your frequently used applications.

![Main Interface](./Images/screenshot-main.png)

## Features

- **Quick Launch**: Double-click to launch applications, or right-click for more options
- **Multi-language Support**: Supports 7 languages (Simplified Chinese, English, Japanese, Korean, French, German, Spanish)
- **Theme Support**: Light and Dark themes
- **Customizable UI**: Adjustable icon sizes (Large, Medium, Small)
- **Smart Search**: Real-time search and filter applications
- **Flexible Management**:
  - Add individual files
  - Add entire folders with recursive scanning
  - Import from Windows Taskbar
  - Delete applications with confirmation
- **Sorting Options**: Sort by name, modified time, or usage count
- **Auto-Save**: Application list is automatically saved and restored on next launch
- **Context Menu**: Right-click menu with multiple options:
  - Launch
  - Run as Administrator
  - Open file location
  - View properties
  - Remove from list

## System Requirements

- Windows 7 or later
- .NET 6.0 Runtime or higher

## Usage

### Adding Applications

1. **Add Files**: Click the "Add File" button and select executable files
2. **Add Folder**: Click the "Add Folder" button to scan and add all executables from a folder
3. **Import Taskbar**: Click "Import Taskbar" to import pinned applications from Windows Taskbar

### Managing Applications

- **Launch**: Double-click an app icon to launch
- **Select Multiple**: Hold Ctrl and click to select multiple applications
- **Delete Selected**: Select apps and click "Delete Selected" button
- **Context Menu**: Right-click an app icon for additional options

### Search and Filter

- Type in the search box at the top to filter applications in real-time

### Settings

Click the "Settings" button to configure:
- Language
- Theme (Light/Dark)
- Icon size
- Window position and opacity
- Application sort mode
- Always on top mode

## Architecture

### Core Components

- **MainForm**: Main window and application management logic
- **AppButton**: Custom control for individual application buttons
- **ProcessLauncher**: Service for launching applications
- **FileScanner**: Service for scanning directories
- **ConfigManager**: Configuration persistence
- **LocalizationManager**: Multi-language support
- **ThemeManager**: Theme management
- **IconExtractor**: Icon extraction from applications

### Data Structure

- **AppInfo**: Stores application information (name, path, icon, usage count, etc.)
- **AppConfig**: Stores user configuration (theme, language, window state, etc.)

## Technical Details

- Built with WinForms (.NET 6.0)
- Resource-based localization system
- Asynchronous icon loading for better UI responsiveness
- COM-based shortcuts parsing for taskbar import
- Automatic icon caching

## Building and Publishing

### Building from Source

```bash
dotnet build
```

### Publishing Self-Contained Executable

To create a standalone executable that doesn't require .NET runtime installation:

```bash
dotnet publish -c Release -r win10-x64 --self-contained true -p:PublishSingleFile=true
```

The output will be located in:
```
bin\Release\net6.0-windows\win10-x64\publish\
```

### Publishing Framework-Dependent Executable

To create a smaller executable that requires .NET 6.0 runtime:

```bash
dotnet publish -c Release
```

The output will be located in:
```
bin\Release\net6.0-windows\publish\
```

### Creating Installer

1. Install [Inno Setup 6](https://jrsoftware.org/isdl.php)
2. Choose one of the installer scripts:
   - **QuickLaunchTool.iss**: Full installer with bundled .NET runtime (~60MB)
   - **QuickLaunchTool-Lite.iss**: Lite installer, requires users to download .NET 6.0 runtime (~5MB)
3. Open the chosen `.iss` file in Inno Setup Compiler
4. Click "Compile" to generate the installer
5. The installer will be created in the `installer\` directory

## Configuration

Settings are saved in:
```
%APPDATA%\QuickLaunchTool\config.json
```

Application cache is stored in the same configuration file.

## File Structure

```
QuickLaunchTool/
├── Forms/              # UI forms
│   ├── MainForm.cs
│   └── SettingsForm.cs
├── Controls/           # Custom controls
│   └── AppButton.cs
├── Services/           # Business logic
│   ├── FileScanner.cs
│   ├── ProcessLauncher.cs
│   ├── IconExtractor.cs
│   └── ConfigManager.cs
├── Utils/              # Utilities
│   ├── LocalizationManager.cs
│   ├── ThemeManager.cs
│   ├── CacheManager.cs
│   └── Logger.cs
├── Models/             # Data models
│   ├── AppInfo.cs
│   └── AppConfig.cs
└── Resources/          # Localization resources
    └── Strings.*.resx
```

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature suggestions.
