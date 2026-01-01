# Architectural Patterns and Design Decisions

This document captures key architectural patterns, design decisions, and conventions used throughout the Matroska Batch Flow project.

## Dependency Injection & Service Registration

### Microsoft.Extensions.Hosting
The application uses `Microsoft.Extensions.Hosting.Host` (not Uno Platform's built-in host) for full control over service registration and configuration.

**Registration happens in `App.xaml.cs` > `OnLaunched` method:**
```csharp
Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) => { /* ... */ })
    .Build();
```

### Service Lifetimes
**Current Implementation: Singleton**
All services, ViewModels, and pages are currently registered as singletons:
```csharp
services.AddSingleton<IBatchConfiguration, BatchConfiguration>();
services.AddSingleton<InputViewModel, InputViewModel>();
```

**Note:** This is the current implementation rather than a strict design requirement. Other lifetimes (scoped/transient) may also work depending on service-specific requirements.

### Service Locator Pattern
`App.GetService<T>()` is available but **used sparingly** - only for retrieving ViewModels in page code-behind.

**Why:** Pages are framework-instantiated (not by DI), but need to expose ViewModels as properties for `x:Bind` in XAML.

**Example:**
```csharp
public sealed partial class AudioPage : Page
{
    public AudioViewModel ViewModel { get; }
    
    public AudioPage()
    {
        ViewModel = App.GetService<AudioViewModel>();
        this.InitializeComponent();
    }
}
```

**Convention:** Prefer constructor injection everywhere else.

## MVVM Pattern Implementation

### ViewModels
**Base Class:** `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
- Use `partial` classes to enable source generators
- Properties decorated with `[ObservableProperty]` auto-generate INotifyPropertyChanged implementation

**Example:**
```csharp
public sealed partial class InputViewModel : ObservableObject, IFilesDropped, INavigationAware
{
    [ObservableProperty]
    private ObservableCollection<ScannedFileViewModel> selectedFiles = [];
}
```

### Commands
Use `CommunityToolkit.Mvvm.Input` for commands:
- `RelayCommand` - for synchronous operations
- `AsyncRelayCommand` - for async operations

```csharp
RemoveSelected = new RelayCommand(RemoveSelectedFiles);
AddFilesCommand = new AsyncRelayCommand(AddFilesAsync);
```

### Track ViewModels
Special base class `TrackViewModelBase` for Audio/Video/Subtitle ViewModels:
- Common properties: `SelectedTrack`, `IsDefaultTrack`, `IsEnabledTrack`, `Languages`
- Update suppression mechanism via `_suppressBatchConfigUpdate` flag
- Derived classes: `AudioViewModel`, `VideoViewModel`, `SubtitleViewModel`

## Navigation Architecture

### Custom Navigation System
Custom navigation implementation **NOT using Uno Platform's NavigationView extension**. Inspired by Microsoft's TemplateStudio.

**Key Components:**
- `INavigationService` - Core navigation service
- `INavigationViewService` - NavigationView-specific logic
- `IPageService` - Page type resolution
- `NavigationHelper` - XAML attached properties for declarative navigation

**XAML Usage:**
```xaml
<NavigationViewItem helpers:NavigationHelper.NavigateTo="FullViewModelTypeName" 
                    helpers:NavigationHelper.IsDefault="True" />
```

### Navigation Awareness
ViewModels can implement `INavigationAware` for lifecycle hooks:
```csharp
public interface INavigationAware
{
    void OnNavigatedTo(object parameter);
    void OnNavigatedFrom();
}
```

### Activation System
**Activation Handlers** manage startup logic before main window activation:
- Base: `ActivationHandler<T>` 
- Default: `DefaultActivationHandler` (handles `LaunchActivatedEventArgs`)
- Pattern allows custom handlers for different activation scenarios

## Messaging & Communication

### CommunityToolkit.Mvvm.Messaging
Used for loosely-coupled communication between components.

**Message Types:**
- `DialogMessage(string Title, string Message)` - Simple record for dialogs
- `DialogStatusMessage` - Status-specific dialogs
- `MkvPropeditArgumentsDialogMessage` - Display mkvpropedit arguments
- `ActivationCompletedMessage` - Signals app activation complete

**Usage Pattern:**
```csharp
WeakReferenceMessenger.Default.Send(new DialogMessage("Error", errorText));
```

## Validation & Processing Rules Engine

### File Validation Rules
**Pattern:** Strategy pattern with composite validation engine

**Interface:**
```csharp
public interface IFileValidationRule
{
    IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files);
}
```

**Rules registered:**
- `LanguageConsistencyRule` - All files must have matching language tracks
- `TrackCountConsistencyRule` - All files must have same number of tracks
- `FileFormatValidationRule` - Only MKV files allowed

**Engine:** `FileValidationEngine` executes all registered rules in sequence.

### File Processing Rules
**Pattern:** Strategy pattern with composite processing engine

**Interface:**
```csharp
public interface IFileProcessingRule
{
    void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig);
}
```

**Rules registered (in order):**
- `TrackPositionRule` - Track positioning logic
- `SubtitleTrackNamingRule` - Subtitle name processing
- `AudioTrackNamingRule` - Audio name processing
- `VideoTrackNamingRule` - Video name processing
- `TrackLanguageRule` - Language processing
- `TrackDefaultRule` - Default flag processing
- `TrackForcedRule` - Forced flag processing
- `FileTitleNamingRule` - File title processing

**Engine:** `FileProcessingEngine` applies rules to update batch configuration.

## Fluent Builder Pattern

### MkvPropedit Arguments Builder
Fluent API for constructing mkvpropedit command-line arguments:

```csharp
var args = new MkvPropeditArgumentsBuilder()
    .SetInputFile(filePath)
    .WithTitle("Movie Title")
    .WithAddTrackStatisticsTags()
    .AddTrack(track => track
        .WithTrackType(TrackType.Audio)
        .WithPosition(1)
        .WithName("English Audio"))
    .Build(); // Returns string[]
```

**Key classes:**
- `MkvPropeditArgumentsBuilder` - Main builder
- `TrackOptionsBuilder` - Track-specific options
- Nested builders in `TrackOptions/` directory

## XAML Patterns & Behaviors

### Compiled Bindings (x:Bind)
**Preference:** Use `x:Bind` over `Binding` for compile-time type checking and performance:
```xaml
<!-- Preferred -->
<ComboBox ItemsSource="{x:Bind ViewModel.AudioTracks, Mode=OneWay}" />

<!-- Avoid when x:Bind is possible -->
<ComboBox ItemsSource="{Binding AudioTracks, Mode=OneWay}" />
```

### Custom XAML Behaviors
**Attached Behaviors** for reusable UI logic:

**1. DropFilesBehavior** - Drag & drop file support:
```xaml
<Grid behavior:DropFilesBehavior.IsEnabled="True"
      behavior:DropFilesBehavior.FileDropTarget="{x:Bind ViewModel}" />
```
- Target implements `IFilesDropped` interface
- Handles `DragOver` and `Drop` events

**2. ListViewSelectedItemsBehavior** - Multi-selection binding:
```xaml
<ListView behavior:ListViewSelectedItemsBehavior.BoundSelectedItems="{x:Bind ViewModel.SelectedFiles}" />
```
- Synchronizes ListView selected items with external IList
- Two-way synchronization with collection change tracking

### Value Converters
Common converters in `Converters/`:
- `BoolToIndexConverter` - Convert boolean to index (e.g., for segmented controls)
- `EmptyStateToTextConverter` - Show text when collection empty
- `GreaterThanZeroConverter` - Enable controls when count > 0
- `IntToVisibilityParameterConverter` - Visibility based on int value
- `InverseBoolConverter` - Negate boolean values
- `ZeroBasedToOneBasedConverter` - Array index to display number

## Configuration & Settings

### Configuration Options Pattern
Uses `Microsoft.Extensions.Options` with data annotations validation:

**Option Classes:**
- `AppConfigOptions` - App-level config (mkvpropedit path, user settings path)
- `LanguageOptions` - Language file configuration
- `ScanOptions` - File scanning parameters

**Registration:**
```csharp
services.AddOptions<LanguageOptions>()
    .Bind(context.Configuration.GetSection(nameof(LanguageOptions)))
    .ValidateDataAnnotations();
```

### User Settings Persistence
**Pattern:** Writable settings with custom interface

**Interface:**
```csharp
public interface IWritableSettings<T> where T : class
{
    T Load();
    void Save(T settings);
}
```

**Implementation:** `WritableJsonSettings<UserSettings>` - Thread-safe JSON file persistence

**Registration:**
```csharp
services.AddSingleton<IWritableSettings<UserSettings>>(sp => {
    var path = sp.GetRequiredService<IOptions<AppConfigOptions>>().Value.UserSettingsPath;
    return new WritableJsonSettings<UserSettings>(path);
});
```

## State Management

### BatchConfiguration as Central State
`IBatchConfiguration` serves as central application state:
- **Observable:** Implements `INotifyPropertyChanged`
- **Events:** `StateChanged` event for global state notifications
- **Collections:** `ObservableCollection<T>` for all track collections
- **File List:** Observable file collection with change tracking

### Collection Change Handling
Pattern for responding to collection changes:
```csharp
FileList.CollectionChanged += (sender, e) => {
    if (e.Action is NotifyCollectionChangedAction.Remove 
        or NotifyCollectionChangedAction.Reset 
        or NotifyCollectionChangedAction.Replace)
    {
        OnFileRemoval(sender, e);
        OnStateChanged();
    }
};
```

## Environment Detection

### AppEnvironmentHelper
Cached, exception-safe environment detection:
```csharp
public static bool IsPackagedApp() => _isPackagedApp.Value;
```
- **Lazy initialization** - Computed once, cached forever
- **Platform-specific:** Uses `Package.Current` on Windows, false elsewhere
- **Exception-safe:** Catches exceptions for unpackaged apps

### AppPathHelper
Provides correct file paths for packaged vs unpackaged scenarios.

## Global Usings

**Common imports** in `GlobalUsings.cs`:
```csharp
global using System.Collections.Immutable;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
```

Reduces repetitive using statements across the codebase.

## Key Design Decisions

### Service Lifetime Choice
Currently all services use singleton lifetime, which works for the application's needs. This wasn't a deliberate architectural constraint - other lifetimes (scoped/transient) could be used where appropriate for specific services.

### Why Custom Navigation?
Uno Platform's NavigationView extension had limitations:
- Couldn't navigate to settings page correctly
- Less control over navigation logic
- Custom implementation provides learning opportunity and flexibility

### Why Separate Validation & Processing Engines?
**Separation of concerns:**
- **Validation** - Ensures files are compatible (fails fast)
- **Processing** - Transforms data for batch operations
- Each engine can be extended independently

### Why Record Types for Models?
`sealed record` for immutable data models:
- Value equality semantics
- Immutability by default
- Concise syntax with positional parameters
- Examples: `MediaInfoResult`, `MkvPropeditResult`, `DialogMessage`

### Why ObservableObject for ViewModels?
CommunityToolkit.Mvvm provides:
- Source generators reduce boilerplate
- Consistent property change notification
- Built-in command support
- Industry-standard MVVM toolkit
