# Code Style and Conventions

## General Settings
- **Charset:** UTF-8 (UTF-8 BOM for XAML files)
- **Line Endings:** CRLF (Windows style)
- **Indentation:** Spaces (not tabs, except for .sln and .plist files)
- **Trim Trailing Whitespace:** Yes
- **Insert Final Newline:** Yes

## Language-Specific Settings

### C# Files (*.cs)
- **Indent Size:** 4 spaces
- **Tab Width:** 4
- **Trim Trailing Whitespace:** False (intentionally disabled, see GitHub issue #20356)
- **End of Line:** Unset (managed by Git, see GitHub issue #1099)

### XAML Files (*.xaml)
- **Indent Size:** 4 spaces
- **Charset:** UTF-8 with BOM

### Project Files (*.csproj, *.proj, *.props, *.targets)
- **Indent Size:** 4 spaces

### JSON Files (*.json)
- **Indent Size:** 2 spaces
- **Line Endings:** LF

### YAML Files (*.yml, *.yaml)
- **Indent Size:** 2 spaces

## C# Naming Conventions

### Interfaces
- **Style:** PascalCase with "I" prefix
- **Example:** `IBatchConfiguration`, `IFileScanner`
- **Severity:** Suggestion

### Types (Classes, Structs, Enums)
- **Style:** PascalCase
- **Example:** `BatchConfiguration`, `TrackType`, `MediaInfoResult`
- **Severity:** Suggestion

### Non-Field Members (Properties, Methods, Events)
- **Style:** PascalCase
- **Example:** `DirectoryPath`, `ProcessFiles()`, `StateChanged`
- **Severity:** Suggestion

## C# Code Style

### Qualifiers
- **Avoid "this."** for fields, properties, methods, and events when not necessary
- **Severity:** Suggestion

### Directives
- **Sort using directives:** Yes
- **System.* directives first:** Yes

### Operator Placement
- **Wrap operators:** Beginning of line

### Style Preferences
- **Null-coalescing expression:** Preferred (`??`)
- **Null propagation:** Preferred (`?.`)
- **`is null` over `ReferenceEquals`:** Preferred
- **Auto-properties:** Preferred (silent)
- **Object initializers:** Preferred
- **Collection initializers:** Preferred
- **Simplified boolean expressions:** Preferred
- **Conditional over assignment:** Preferred (silent)
- **Conditional over return:** Preferred (silent)
- **Explicit tuple names:** Preferred
- **Inferred tuple names:** Preferred

## Namespace Declaration
- **Style:** File-scoped namespaces (C# 10+)
- **Example:** `namespace MatroskaBatchFlow.Core.Services;`

## Common Patterns

### Service Interfaces
Services typically have corresponding interfaces:
- Implementation: `BatchConfiguration`
- Interface: `IBatchConfiguration`

### Observable Collections
Used for UI-bound collections:
```csharp
private readonly ObservableCollection<ScannedFileInfo> _fileList = [];
```

### Property Change Notifications
Services implement `INotifyPropertyChanged`:
```csharp
public event PropertyChangedEventHandler? PropertyChanged;
```

### Documentation Comments
Use XML documentation comments for public APIs:
```csharp
/// <summary>
/// Represents the configuration for batch processing of media files.
/// </summary>
public class BatchConfiguration : IBatchConfiguration
```

## Testing Conventions

### Test Framework
- **Framework:** xUnit v3
- **Mocking:** NSubstitute
- **Coverage:** Coverlet

### Test File Naming
- **Pattern:** `{ClassUnderTest}Tests.cs`
- **Example:** `BatchConfigurationTests.cs`

### Test Method Naming
- **Pattern:** `MethodName_Scenario_ExpectedResult`
- **Example:** `Clear_WhenCalled_ResetsAllPropertiesAndTrackCollections`

### Test Structure
Follow AAA pattern (Arrange, Act, Assert):
```csharp
[Fact]
public void TestMethod()
{
    // Arrange
    var config = new BatchConfiguration();
    
    // Act
    config.Clear();
    
    // Assert
    Assert.Empty(config.DirectoryPath);
}
```

### Test Builders
Use builder pattern for complex test data:
```csharp
var audioTrackInfo = new MediaInfoResultBuilder()
    .WithCreatingLibrary()
    .AddTrackOfType(TrackType.Audio)
    .Build();
```

## Nullable Reference Types
- **Enabled:** Yes (`<Nullable>enable</Nullable>`)
- Use nullable annotations (`?`) appropriately
- Follow null-safety best practices

## Implicit Usings
- **Enabled:** Yes (`<ImplicitUsings>enable</ImplicitUsings>`)
- Common namespaces are automatically imported
