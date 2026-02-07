# Matroska Batch Flow - Copilot Instructions

> **Audience**: These instructions are for AI coding assistants, not for human developers.  
> **Human developers**: See [README.md](../README.md) for setup and contribution guidelines.

> **Last Updated**: 2026-02-07  
> **Next Review**: When architecture changes, new patterns are introduced, or build commands change
> 
> **Maintenance**: When making significant code changes, update this file and Serena memories to keep them in sync with the codebase.

## Build, Test, and Run Commands

### Quick Reference
```powershell
# Most common commands (run from repository root)
dotnet build MatroskaBatchFlow.sln         # Build entire solution
dotnet test                                # Run all tests
dotnet run --project src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-windows10.0.19041
```

### Building
```powershell
# Build the WinAppSDK target (Recommended)
dotnet build src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-windows10.0.19041

# Build the Skia Desktop target (Experimental)
dotnet build src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-desktop

# Build entire solution
dotnet build MatroskaBatchFlow.sln
```

### Running
```powershell
# Run the application (automatically builds if needed)
dotnet run --project src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-windows10.0.19041
```

### Testing
```powershell
# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/MatroskaBatchFlow.Core.UnitTests/MatroskaBatchFlow.Core.UnitTests.csproj
dotnet test tests/MatroskaBatchFlow.Uno.UnitTests/MatroskaBatchFlow.Uno.UnitTests.csproj
dotnet test tests/MatroskaBatchFlow.Uno.IntegrationTests/MatroskaBatchFlow.Uno.IntegrationTests.csproj

# Run a single test by filtering
dotnet test --filter "FullyQualifiedName~BatchConfigurationTests"
```

### Publishing
```powershell
# Use the Publish-Application.ps1 script for various distribution modes
.\Publish-Application.ps1 -BuildType SingleFile    # Self-contained single executable
.\Publish-Application.ps1 -BuildType MultiFile     # Multi-file deployment
.\Publish-Application.ps1 -BuildType Store         # MSIX for Microsoft Store
.\Publish-Application.ps1 -BuildType SelfSigned    # MSIX with self-signed certificate
```

## Architecture Overview

### Project Structure
- **MatroskaBatchFlow.Core** - Cross-platform core library containing business logic, file validation, processing engines, and external tool orchestration
- **MatroskaBatchFlow.Uno** - WinUI 3/Uno Platform GUI application (supports WinAppSDK and Skia Desktop)
- **MatroskaBatchFlow.Console** - Reserved for future CLI utility

### Core Components

#### Service Layer
- **File Processing Pipeline**: `IFileScanner` → `IFileValidationEngine` → `IFileProcessingOrchestrator` → `IMkvToolExecutor`
  - `FileScanner`: Discovers and scans `.mkv` files using MediaInfo for metadata extraction
  - `FileValidationEngine`: Validates file consistency using pluggable validation rules
  - `FileProcessingOrchestrator`: Coordinates batch file processing with cancellation support
  - `MkvPropeditExecutor`: Executes `mkvpropedit` commands via `ProcessRunner`

- **Validation System**: Rule-based validation with three strictness modes (Strict, Lenient, Custom)
  - Rules implement `IFileValidationRule` (e.g., `TrackCountConsistencyRule`, `LanguageConsistencyRule`)
  - Results are severity-based: Error, Warning, Information

- **Configuration Management**: `IBatchConfiguration` tracks file-level and track-level configurations
  - `IBatchTrackConfigurationInitializer`: Initializes track configurations from scanned files
  - `ITrackConfigurationFactory`: Creates track configurations by type (Video, Audio, Subtitle, General)

#### Presentation Layer (MVVM + Messaging)
- **Architecture**: MVVM with CommunityToolkit.Mvvm (source generators for commands/properties)
- **Messaging**: Uses `WeakReferenceMessenger` for loosely-coupled communication between ViewModels and Views
  - Message types in `Messages/` folder: `DialogMessage`, `ExceptionDialogMessage`, `ShowValidationDetailsMessage`, etc.
- **Navigation**: `INavigationService` handles page navigation with `INavigationAware` interface for lifecycle hooks
- **Services**: Interface-first design with contracts in `Contracts/Services/` and implementations in `Services/`

#### Key Services
- `IFileListAdapter`: Manages the collection of scanned files with add/remove operations
- `IValidationSettingsService`: Manages validation strictness modes and track-level severity settings
- `IThemeApplierService`: Applies Light/Dark/System themes
- `IUIPreferencesService`: Persists user preferences via `WritableJsonSettings<T>`

### External Dependencies
- **MediaInfo**: Media file analysis (embedded library via P/Invoke)
- **MKVToolNix (`mkvpropedit`)**: Direct container property editing without re-encoding
- **Uno Platform SDK**: Cross-platform UI framework (version managed via `global.json`)

## Key Conventions

> **For detailed code style guidelines**, see `.serena/memories/code_style_and_conventions.md`  
> This includes formatting rules, naming conventions, control flow patterns, and testing standards.

### Code Organization
- **Never use #region/#endregion blocks** in C# or PowerShell code
  - Organize code with clear comments and whitespace instead
  - Keep sections separated visually without folding directives

### Logging Pattern
- Use **LoggerMessage source generators** for high-performance logging
- Logging definitions go in separate `*.Logging.cs` partial class files
  ```csharp
  // MainViewModel.Logging.cs
  public partial class MainViewModel
  {
      [LoggerMessage(Level = LogLevel.Warning, Message = "Batch processing aborted: {ErrorMessage}")]
      private partial void LogBatchProcessingAborted(string errorMessage);
  }
  ```
- All services and ViewModels requiring logging should follow this pattern

### MVVM Patterns
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `[ObservableProperty]` source generator for properties (avoid manual `INotifyPropertyChanged`)
- Use `[RelayCommand]` source generator for commands
- Global usings in `GlobalUsings.cs` make common types available project-wide
- Track-specific ViewModels inherit from `TrackViewModelBase`

### Dependency Injection
- All services registered as **singletons** in `ServiceCollectionExtensions.cs`
- Interface-first design: all services have an `I{ServiceName}` interface
- Service lifetimes: Singleton for all app services (no scoped/transient)

### Testing
- **xUnit v3** for all test projects with `[Fact]` and `[Theory]` attributes
- **NSubstitute** for mocking
- **Builder pattern** for test data construction (e.g., `MediaInfoResultBuilder`)
- Builders marked with `[ExcludeFromCodeCoverage]`
- Test classes organized to mirror source structure

### Settings and Configuration
- `appsettings.json` for app configuration with DataAnnotations validation via `ConfigurationValidator`
- User settings persisted via `WritableJsonSettings<T>` (writes to local app data as `UserSettings.json`)
- Settings/configuration models use `sealed class` with mutable properties for JSON serialization
- Use `record` types for immutable data transfer objects (e.g., `MatroskaLanguageOption`)
- Configuration validation failures show custom error window before app launch

### Central Package Management
- All package versions managed centrally in `Directory.Packages.props`
- `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` in `Directory.Build.props`
- Uno SDK version specified in `global.json` (see that file for the current `"Uno.Sdk"` version)

### Target Frameworks
- **WinAppSDK target**: `net10.0-windows10.0.19041` (Primary, fully supported)
- **Skia Desktop target**: `net10.0-desktop` (Experimental, cross-platform)
- Build for WinAppSDK by default unless explicitly testing Skia

### Collection Types
- Use `ImmutableList<T>` for read-only reference data (e.g., language lists)
- Use `ObservableCollection<T>` for mutable collections that need change notifications
- Use `ReadOnlyObservableCollection<T>` to expose mutable collections publicly while preventing external modification
- Use `UniqueObservableCollection<T>` (custom type) for collections requiring uniqueness constraints (e.g., file lists)

### Platform-Specific Code
- Use `#if WINDOWS10_0_19041_0_OR_GREATER` for WinAppSDK-specific code
- Platform-specific implementations go in `Platforms/` folders
- Keep platform-specific code isolated; prefer abstractions in Core library

### Error Handling
- Configuration validation failures show custom error window before app launch
- All exceptions during batch processing are captured and reported via `FileProcessingReport`
- Use `ExceptionDialogMessage` to show error dialogs to users
- Log exceptions with appropriate severity using LoggerMessage source generators

### Performance Considerations
- `FileScanner` uses parallel processing for large file sets
- `FileProcessingOrchestrator` supports cancellation via `CancellationToken`
- Validation rules run in sequence; early validation failures can prevent file processing
- ObservableCollection operations on UI thread; batch updates should be marshaled appropriately

### Important Files and Paths
- **appsettings.json**: Application configuration (validated on startup)
  - `MkvPropeditPath`: Path to mkvpropedit executable (default: `"mkvpropedit"`)
  - `LanguageOptions.FilePath`: Path to languages.json (default: `"Resources\\languages.json"` in config)
  - `ScanOptions`: Default scanning behavior
- **Resources/languages.json**: ISO 639-2 language codes with display names
- **Binaries/**: Native libraries (MediaInfo.dll, mkvpropedit.exe for Windows; libmediainfo.so for Linux)
- **User settings**: Stored in local app data folder via `WritableJsonSettings<T>`

## Common Workflows

### Adding a New Service
1. Create interface in `Core/Services/` or `Uno/Contracts/Services/`
2. Implement in corresponding implementation folder
3. Add `.Logging.cs` partial class if service needs logging
4. Register in `ServiceCollectionExtensions.cs` (typically as singleton)
5. Update relevant ViewModels to inject and use the service

### Adding a New Validation Rule
1. Create class implementing `IFileValidationRule` in `Core/Services/FileValidation/`
2. Add rule-specific logic in `Validate()` method
3. Return `FileValidationResult` with appropriate severity
4. Register rule in `ServiceCollectionExtensions.AddFileValidationRules()` method
5. Write unit tests in `tests/MatroskaBatchFlow.Core.UnitTests/Services/FileValidation/`

### Adding a New Page/ViewModel
1. Create ViewModel inheriting `ObservableObject` in `Uno/Presentation/`
2. Add `.Logging.cs` partial class if logging is needed
3. Create corresponding `.xaml` and `.xaml.cs` page files
4. Register page in `PageService.cs` Configure method
5. Add navigation menu item in `Shell.xaml` if needed
6. Implement `INavigationAware` if page needs lifecycle hooks

### Working with Track Configurations
1. Track-specific ViewModels inherit from `TrackViewModelBase`
2. Use `_suppressBatchConfigUpdate` flag when bulk-updating properties to prevent excessive batch config updates
3. Track collections are `ObservableCollection<TrackConfiguration>` for change notifications
4. Use `ITrackConfigurationFactory` to create track configurations by type

### Debugging Batch Processing
1. Check `BatchExecutionReport` in `IBatchReportStore` for detailed results
2. Review `FileProcessingReport` for per-file status and errors
3. Enable Debug logging in Settings UI or appsettings.json
4. Check `mkvpropedit` command arguments in debug output before execution
5. Use `MkvPropeditArgumentsDialog` to preview generated commands

## Common Gotchas and Troubleshooting

### Build Issues
- **Missing packages**: Run `dotnet restore` from solution root
- **Wrong target framework**: Ensure `-f net10.0-windows10.0.19041` is specified
- **Uno SDK version mismatch**: Check `global.json` for correct SDK version
- **WinAppSDK issues**: Ensure Windows 11 SDK is installed

### Runtime Issues
- **Configuration validation fails**: Check appsettings.json against DataAnnotations in model classes
- **mkvpropedit not found**: Set `MkvPropeditPath` in appsettings.json or Settings UI
- **Languages not loading**: Verify `Resources/languages.json` exists and is valid JSON
- **File scanning fails**: Check file permissions and ensure MediaInfo.dll is in Binaries folder

### Testing Issues
- **Tests fail to discover**: Ensure xUnit v3 test adapter is installed
- **Mock setup complexity**: Use NSubstitute's fluent API and builder pattern for test data
- **Integration test isolation**: Each test should use its own service container

### Common Code Patterns to Avoid
- Don't use #region/#endregion blocks (use comments and whitespace)
- Don't manually implement INotifyPropertyChanged (use `[ObservableProperty]` source generator)
- Don't create commands manually (use `[RelayCommand]` source generator)
- Don't register services as scoped/transient unless necessary (use singleton)
- Don't update ObservableCollections from background threads (marshal to UI thread)

## Security and Privacy

- **No secrets in code**: Never commit API keys, passwords, or certificates to source control
- **User data**: File paths are logged at Debug level only; no file content is logged
- **External tools**: mkvpropedit is executed with controlled arguments; validate user input before passing to command line
- **File operations**: All file modifications require explicit user action (no automatic writes on scan)

## Commit Message Standards

**Copilot MUST follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) specification when generating commit messages.**

### Format
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Required Types
- `feat:` - A new feature (correlates with MINOR in SemVer)
- `fix:` - A bug fix (correlates with PATCH in SemVer)

### Recommended Types
Additional types from @commitlint/config-conventional (Angular convention):
- `build:` - Changes to build system or dependencies
- `chore:` - Maintenance tasks
- `ci:` - CI/CD configuration changes
- `docs:` - Documentation changes
- `style:` - Code style/formatting changes (no functional changes)
- `refactor:` - Code refactoring without behavior change
- `perf:` - Performance improvements
- `test:` - Adding or updating tests
- `revert:` - Reverting a previous commit

### Scope (Optional)
Scopes represent **what the change is about** (functional/logical areas), not just physical project locations.

**Common scopes used in this codebase:**

*Project layers:*
- `core` - Core library logic (MatroskaBatchFlow.Core)
- `ui` - Views, XAML, and presentation components
- `vm` - ViewModels and MVVM patterns

*Domain-specific:*
- `validation` - Validation system (rules, engine, strictness modes)
- `processing` - File processing pipeline and batch operations
- `tracks` - Track configuration and track-level logic
- `mkvtools` - mkvpropedit/MediaInfo integration

*Infrastructure:*
- `config` - Configuration and settings
- `logging` - Logging system
- `di` - Dependency injection and service registration
- `nav` - Navigation system

*Tooling:*
- `deps` - Package dependencies
- `dev` - Development tooling

*Documentation:*
- `instructions` - AI assistant instructions (copilot-instructions.md, Serena memories)

*Release:*
- `version` - Version management
- `release` - Release and publishing

> **Note:** `build`, `ci`, `docs`, and `test` are commit **types**, not scopes. Use them as types (e.g., `build: update project files`, `test: add validation tests`).

**When to use a scope:**
- Changes focus on a specific functional area or component
- Examples: `feat(logging): add structured logging`, `fix(config): handle invalid JSON`, `refactor(ui): simplify navigation`

**When to omit the scope:**
- Changes are truly repository-wide or don't fit a single category
- Examples: `chore: update all dependencies`, `docs: update README`

**Multiple scopes:**
- Use comma-separated scopes when changes span multiple areas: `refactor(core,ui): redesign batch processing`

**Expanding scopes:**
- The scopes above are common examples, not an exhaustive list
- Create new scopes when needed for clarity (e.g., `feat(dialogs): add confirmation dialog`)

### Breaking Changes
- **Option 1**: Add `!` after type/scope: `feat(core)!: redesign batch processing API`
- **Option 2**: Use footer: `BREAKING CHANGE: description` (MUST be uppercase)
- Breaking changes correlate with MAJOR in SemVer

### Body (Optional)
- Provide additional context after a blank line
- Free-form text, may contain multiple paragraphs
- **Project preference**: Use bullet points for clarity, without blank lines between items

### Footer(s) (Optional)
Footers follow git trailer format (token: value or token #value):
- `BREAKING CHANGE: <description>` - Breaking change details
- `Refs: #123` - Reference issues
- `Closes: #123` or `Fixes: #123` or `Resolves: #123` - Close issues
- `Reviewed-by:`, `Acked-by:`, `See-also:` - Other standard git trailers

### Examples

**Simple feature:**
```
feat(core): add subtitle track reordering support
```

**Bug fix with body and footer:**
```
fix(ui): prevent UI freeze during large batch operations

- Move batch processing to background thread
- Add progress reporting with cancellation support
- Update MainViewModel to marshal UI updates

Closes: #89
```

**Breaking change with scope:**
```
feat(core)!: redesign track configuration API

- Replace TrackConfiguration with immutable TrackInfo
- Update all track ViewModels to use new API
- Migration guide added to docs/migration.md

BREAKING CHANGE: TrackConfiguration class removed, use TrackInfo instead
Refs: #125
```

**Documentation update:**
```
docs: update README with mkvpropedit installation guide

Refs: #42
```

### Important Rules
- Type and description are REQUIRED
- Description MUST immediately follow the colon and space
- Scope MUST be a noun in parentheses if provided
- BREAKING CHANGE footer MUST be uppercase
- Breaking changes can use `!` in prefix OR footer (or both)
- Tokens other than BREAKING CHANGE are case-insensitive

> **Cross-reference**: Detailed Conventional Commits standards are also in Serena's memory (`.serena/memories/task_completion_checklist.md`). Keep both locations synchronized.

## **CRITICAL: Always Use Serena First (#serena MCP server)**

**Serena is a semantic code analysis toolkit that provides IDE-like capabilities to AI assistants. It's designed for:**
- **Symbol-level code understanding** (finding functions, classes, interfaces by name or semantics)
- **Semantic code search** (finding code by meaning, not just text matching)
- **Relationship analysis** (finding what references a symbol, what a symbol references)
- **Precise code editing** (editing at symbol level, not just text replacement)
- **Memory management** (storing and retrieving architectural knowledge)

> **Learn more**: [Serena GitHub Repository](https://github.com/oraios/serena) | [Serena Documentation](https://oraios.github.io/serena/)

**For ALL code analysis, investigation, understanding, and editing tasks, use Serena semantic tools:**

### **What Serena IS For (Use it!)**
✅ Finding symbols/functions/classes by name or semantic meaning  
✅ Understanding code relationships (what calls what, what implements what)  
✅ Getting symbol-level code overviews without reading entire files  
✅ Editing code precisely at function/class boundaries  
✅ Storing and retrieving architectural knowledge as memories  
✅ Analyzing cross-file dependencies and patterns  
✅ Understanding how components integrate without grep/text search  

### **What Serena is NOT For (Use other tools)**
❌ Simple file viewing (use `view` tool)  
❌ Checking if files exist (use `view` tool)  
❌ Running tests or builds (use `powershell` tool)  
❌ Creating new files from scratch (use `create` tool)  
❌ Simple text replacement (use `edit` tool when you know exact location)  

### **Standard Serena Workflow**
1. **Start with Serena memories**: Use Serena to list memories and read relevant ones for context #serena
2. **Use semantic analysis**: Use Serena to find [symbols/functions/patterns] related to [issue] #serena
3. **Get symbol-level insights**: Use Serena to analyze [specific function] and show all referencing symbols #serena
4. **Create new memories**: Use Serena to write a memory about [findings] for future reference #serena

### **Keeping Serena Memories Up to Date**

**When to update or create Serena memories:**
- After refactoring major components or changing architecture
- When new patterns or conventions are established
- After adding new validation rules, file processing rules, or service types
- When fixing significant bugs that reveal non-obvious behavior
- After updating this copilot-instructions.md file

**Memory maintenance workflow:**
1. Before large changes: Read relevant memories to understand context
2. During implementation: Note significant architectural decisions
3. After completion: Update or create memories to reflect new patterns
4. Tag memories with categories: architecture, validation, processing, ui-patterns, etc.

**Example memory updates:**
- "Updated file-processing-pipeline memory to include new cancellation pattern"
- "Created validation-rules-pattern memory documenting how to implement IFileValidationRule"
- "Updated mvvm-messaging memory with new DialogMessage types"

### **Serena-First Examples**

```
# ✅ GOOD - Using Serena for semantic code analysis:

# Finding code by meaning/purpose
"Use Serena to find functions that handle [specific task] #serena"
"Use Serena to find classes related to [feature/component] #serena"

# Understanding code relationships
"Use Serena to show what calls this function and what it calls #serena"
"Use Serena to find all implementations of [interface/base class] #serena"

# Symbol-level code understanding
"Use Serena to get an overview of symbols in [folder/namespace] #serena"
"Use Serena to analyze how [component A] and [component B] interact #serena"

# Architecture and patterns
"Use Serena to read relevant memories about [system/pattern] #serena"
"Use Serena to find all classes that follow [specific pattern] #serena"

# Before/after major changes
"Use Serena to understand how [system] works before making changes #serena"
"Use Serena to update memory about [new pattern/change] #serena"

# ❌ BAD - Using Serena for simple tasks that don't need semantic analysis:

"Use Serena to check if a file exists" → Use: view tool
"Use Serena to read an entire file" → Use: view tool  
"Use Serena to run tests/builds" → Use: powershell tool
"Use Serena to create a new file" → Use: create tool
```

## Keeping This File Updated

**Update this file when:**
- Build, test, or run commands change
- New architectural patterns are introduced
- Significant conventions or practices are established
- External dependencies are added or updated
- New service types or design patterns are adopted

**Update workflow:**
1. Make code changes
2. Update relevant sections in this file
3. Update Serena memories to match
4. Update "Last Updated" date at the top
5. Commit both code changes and instruction updates together using Conventional Commits format

**Review cadence:**
- Minor updates: As needed when patterns change
- Major reviews: Every 3-6 months or after significant feature additions
- Opportunistic reviews: When Copilot sessions identify gaps or outdated information
