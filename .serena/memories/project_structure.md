# Project Structure

## Solution Organization
The solution contains 3 main projects and 2 test projects:

### Source Projects

#### 1. MatroskaBatchFlow.Core
**Location:** `src/MatroskaBatchFlow.Core/`
**Purpose:** Core business logic library
**Target Frameworks:** net10.0, net10.0-windows10.0.19041
**Key Directories:**
- `Binaries/` - Contains MediaInfo.dll, libmediainfo.so, and mkvpropedit.exe (bundled executables)
- `Builders/` - Builder patterns for creating complex objects (e.g., MkvPropeditArguments)
- `Converters/` - JSON converters for serialization
- `Enums/` - Enumerations (MkvPropeditStatus, ProcessingStatus, TrackType)
- `Models/` - Data models (BatchExecutionReport, FileProcessingReport, MediaInfoResult, MkvPropeditResult, ScannedFileInfo, UserSettings, MatroskaLanguageOption)
- `Services/` - Business logic services (BatchConfiguration, FileScanner, batch processing, file validation, track processing)
- `Utilities/` - Helper utilities and MediaInfo library wrapper

**Notable:** 
- Enables unsafe blocks (`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`)
- Uses `EnableWindowsTargeting` for cross-platform development
- Copies native binaries to output directory

#### 2. MatroskaBatchFlow.Uno
**Location:** `src/MatroskaBatchFlow.Uno/`
**Purpose:** GUI application built with Uno Platform
**Target Frameworks:** net10.0-windows10.0.19041, net10.0-desktop, net10.0
**Key Directories:**
- `Activation/` - Application activation logic
- `Assets/` - Images, icons, and resources
- `Behavior/` - XAML behaviors
- `Contracts/` - Service contracts/interfaces
- `Converters/` - Value converters for XAML bindings
- `Extensions/` - Extension methods
- `Helpers/` - Helper classes
- `Messages/` - Messaging/event aggregation
- `Platforms/` - Platform-specific code
- `Presentation/` - XAML pages and dialogs (AudioPage, VideoPage, SubtitlePage, InputPage, OutputPage, BatchResultsPage, SettingsPage, MainWindow)
- `Properties/` - Launch settings
- `Resources/` - App resources and strings
- `Services/` - Application services
- `Strings/` - Localization strings
- `Utilities/` - Application utilities

**Application Info:**
- Assembly Name: MatroskaBatchFlow
- Application Title: "Matroska Batch Flow"
- Publisher: Tim
- Signed with certificate thumbprint

#### 3. MatroskaBatchFlow.Console
**Location:** `src/MatroskaBatchFlow.Console/`
**Purpose:** Command-line utility for development and testing
**Note:** Primarily used for development and interacting with the Core project

### Test Projects

#### 1. MatroskaBatchFlow.Core.Tests
**Location:** `tests/MatroskaBatchFlow.Core/`
**Purpose:** Tests for the Core project
**Framework:** xUnit v3
**Test Directories:**
- `Builders/` - Test builders for creating test data
- `Services/` - Service unit tests

#### 2. MatroskaBatchFlow.Uno.Tests
**Location:** `tests/MatroskaBatchFlow.Uno/`
**Purpose:** Tests for the Uno project
**Framework:** xUnit v3
**Test Directories:**
- `Extensions/` - Extension method tests

## Configuration Files

### Root Level
- `MatroskaBatchFlow.sln` - Main solution file
- `global.json` - SDK configuration
- `Directory.Build.props` - Central package management enabled
- `Directory.Packages.props` - Centralized package versions
- `.editorconfig` - Code style and formatting rules
- `README.md` - Project documentation
- `LICENSE` - License file

### Important Notes
- Central Package Management is enabled for consistent package versions across projects
- Solution supports both Debug and Release configurations
- Platforms: AnyCPU and x64
