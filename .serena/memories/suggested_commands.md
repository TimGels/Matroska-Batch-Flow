# Suggested Commands

## Prerequisites
Ensure you have:
- .NET 10.0 SDK installed
- Windows 10/11 (for WinAppSDK targets)
- MKVToolNix installed (mkvpropedit is bundled for Windows)

## Building the Projects

### Build Core Library
```powershell
dotnet build src/MatroskaBatchFlow.Core/MatroskaBatchFlow.Core.csproj
```

### Build Uno GUI Application (Desktop Target)
```powershell
dotnet build src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj /property:TargetFramework=net10.0-desktop
```

### Build Uno GUI Application (Windows Target)
```powershell
dotnet build src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj /property:TargetFramework=net10.0-windows10.0.19041
```

### Build Entire Solution
```powershell
dotnet build MatroskaBatchFlow.sln
```

### Build with VS Code Task (Desktop)
Use the configured task: `build-desktop`

## Running the Application

### Run Desktop Application
```powershell
dotnet run --project src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-desktop
```

### Run Windows Application
```powershell
dotnet run --project src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-windows10.0.19041
```

### Run with Launch Profiles
The application has several launch profiles defined in `launchSettings.json`:
- **WinAppSDK Unpackaged** - Run as unpackaged Windows app
- **WinAppSDK Packaged** - Run as MSIX packaged app
- **Desktop** - Run using Skia Desktop renderer
- **Desktop WSL2** - Run in WSL2 environment (Linux)

## Publishing

### Publish Desktop Application
```powershell
dotnet publish src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj /property:TargetFramework=net10.0-desktop
```

### Publish with VS Code Task
Use the configured task: `publish-desktop`

## Testing

### Run All Tests
```powershell
dotnet test
```

### Run Core Tests Only
```powershell
dotnet test tests/MatroskaBatchFlow.Core/MatroskaBatchFlow.Core.Tests.csproj
```

### Run Uno Tests Only
```powershell
dotnet test tests/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.Tests.csproj
```

### Run Tests with Coverage
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test
```powershell
dotnet test --filter "FullyQualifiedName~BatchConfigurationTests"
```

## Cleaning

### Clean Build Artifacts
```powershell
dotnet clean
```

### Clean and Rebuild
```powershell
dotnet clean; dotnet build
```

### Deep Clean (Remove bin/obj folders)
```powershell
Get-ChildItem -Recurse -Include bin,obj | Remove-Item -Recurse -Force
```

## Package Management

### Restore NuGet Packages
```powershell
dotnet restore
```

### List Outdated Packages
```powershell
dotnet list package --outdated
```

### Update Package Version (in Directory.Packages.props)
Manually edit `Directory.Packages.props` since Central Package Management is enabled.

## Windows-Specific Utilities

### List Directory Contents
```powershell
Get-ChildItem
# Or shorthand
ls
```

### Find Files
```powershell
Get-ChildItem -Recurse -Filter "*.cs"
```

### Search File Contents (grep equivalent)
```powershell
Select-String -Path "*.cs" -Pattern "BatchConfiguration"
```

### Current Directory
```powershell
Get-Location
# Or shorthand
pwd
```

### Change Directory
```powershell
Set-Location "src/MatroskaBatchFlow.Core"
# Or shorthand
cd src/MatroskaBatchFlow.Core
```

## Git Commands

### Check Status
```powershell
git status
```

### View Changes
```powershell
git diff
```

### Stage Changes
```powershell
git add .
```

### Commit Changes
```powershell
git commit -m "Your commit message"
```

### View Log
```powershell
git log --oneline
```

### Create Branch
```powershell
git checkout -b feature/your-feature-name
```

## Hot Reload (for Development)

### Enable Hot Reload for Desktop
```powershell
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"; dotnet run --project src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-desktop
```

## Debugging

### Run with Verbose Logging
```powershell
dotnet build -v detailed
```

### Check .NET SDK Version
```powershell
dotnet --version
```

### List Installed SDKs
```powershell
dotnet --list-sdks
```

### List Installed Runtimes
```powershell
dotnet --list-runtimes
```
