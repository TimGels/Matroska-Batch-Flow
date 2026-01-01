# Technology Stack

## Core Technologies

### .NET Platform
- **.NET Version:** 10.0
- **Target Frameworks:**
  - `net10.0` (cross-platform)
  - `net10.0-windows10.0.19041` (Windows-specific)
  - `net10.0-desktop` (Skia Desktop)
- **Language Version:** C# 12+ (with .NET 10)
- **Features Enabled:**
  - Implicit usings
  - Nullable reference types
  - File-scoped namespaces
  - Unsafe blocks (Core project only)

### UI Framework
- **Primary Framework:** Uno Platform SDK 6.5.0-dev.100
- **UI Paradigm:** WinUI 3
- **Rendering:** Skia renderer for cross-platform support
- **Uno Features Used:**
  - Lottie (animations)
  - MediaElement (media playback)
  - Hosting (dependency injection)
  - Toolkit (UI controls)
  - Logging & LoggingSerilog
  - Mvvm (MVVM pattern support)
  - Configuration (settings management)
  - Serialization (JSON)
  - Localization (multi-language support)
  - ThemeService (theme management)
  - SkiaRenderer (cross-platform rendering)

## Key Dependencies

### UI Components
- CommunityToolkit.WinUI.Controls.Primitives
- CommunityToolkit.WinUI.Controls.SettingsControls

### Configuration & Options
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.Options
- Microsoft.Extensions.Options.DataAnnotations

### Testing
- **xUnit v3** - Testing framework
- **xunit.runner.visualstudio** - Visual Studio test runner
- **NSubstitute** - Mocking framework
- **coverlet.collector** - Code coverage
- **Microsoft.NET.Test.Sdk** - .NET test SDK

## External Tools & Libraries

### Media Processing
- **MediaInfo:** Native library for extracting media file information
  - `MediaInfo.dll` (Windows)
  - `libmediainfo.so` (Linux)
  - Bundled in `Binaries/` folder
  - Wrapper in `Utilities/MediaInfoLib/`

- **MKVToolNix:** Specifically mkvpropedit
  - `mkvpropedit.exe` (bundled for Windows)
  - Used for editing Matroska container properties
  - Fast, direct container editing without re-encoding

## Architecture Patterns

### Design Patterns Used
- **MVVM (Model-View-ViewModel):** Primary UI pattern
- **Dependency Injection:** Using Microsoft.Extensions.Hosting
- **Repository Pattern:** Services abstract data access
- **Builder Pattern:** For complex object construction (e.g., MediaInfoResultBuilder)
- **Observer Pattern:** INotifyPropertyChanged, ObservableCollection

### Separation of Concerns
- **MatroskaBatchFlow.Core:** Pure business logic, no UI dependencies
- **MatroskaBatchFlow.Uno:** UI layer, depends on Core
- **Tests:** Unit tests separate from production code

## Platform-Specific Features

### Windows (Primary Target)
- **WinAppSDK/WinUI 3:** Primary target platform (net10.0-windows10.0.19041)
- Packaged and unpackaged deployment support
- Windows 10 SDK targeting (10.0.19041)
- Native Windows UI with full WinUI 3 feature set

### Desktop (Secondary - Cross-Platform)
- **Skia Desktop renderer:** Secondary target (net10.0-desktop)
- Cross-platform rendering for Windows, Linux, macOS
- WSL2 support configured
- Note: Some platform-specific behaviors may differ from WinAppSDK target

## Development Tools

### Package Management
- **Central Package Management:** Enabled via `Directory.Build.props`
- **Package Versions:** Defined in `Directory.Packages.props`
- Ensures consistent versions across all projects

### Build System
- **MSBuild:** Primary build system
- **Uno.Sdk:** Custom SDK for Uno Platform projects
- **SingleProject:** Enabled for multi-platform targeting

### IDE Support
- **Visual Studio 2022** (recommended for Windows)
- **Visual Studio Code** (with tasks configured)
- **Rider** (JetBrains IDE)

## Configuration Management

### Settings Files
- `appsettings.json` - Application settings
- `launchSettings.json` - Launch profiles for different platforms
- `.editorconfig` - Code style enforcement

### Manifest Files
- `app.manifest` - Windows application manifest
- `Package.appxmanifest` - UWP/WinAppSDK package manifest
- `Package.StoreAssociation.xml` - Store association

## Localization
- Built-in support via Uno Platform Localization feature
- Strings organized in `Strings/` directory
- XAML `x:Uid` for string resource binding

## Logging
- **Serilog** integration via Uno Platform
- Structured logging support
- Configurable log levels and sinks
