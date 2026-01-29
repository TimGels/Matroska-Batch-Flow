using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Uno.Activation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Enums;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using MatroskaBatchFlow.Uno.Services;
using MatroskaBatchFlow.Uno.Utilities;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MatroskaBatchFlow.Uno;

public partial class App : Application
{
    /// <summary>
    /// The display name of the application for use in user-facing contexts.
    /// </summary>
    private const string AppDisplayName = "Matroska Batch Flow";

    /// <summary>
    /// Error message prefix for configuration validation failures.
    /// </summary>
    private const string ConfigValidationErrorPrefix = "Configuration validation failed in appsettings.json:\n\n";

    /// <summary>
    /// A PascalCase representation of the application name, which can be used for folder names and other identifiers.
    /// </summary>
    public static string AppName => "MatroskaBatchFlow";

    public static T GetService<T>()
    where T : class
    {
        var app = App.Current as App;
        if (app?.Host?.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Subscribe to unhandled exceptions early to also catch errors during startup.
        this.UnhandledException += OnUnhandledException;
    }

    public static MainWindow? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    private ILogger<App>? _logger;

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Create a level switch for dynamic log level control
        var levelSwitch = new LoggingLevelSwitch();

        // Configure Serilog with file logging
        var logPath = Path.Combine(AppPathHelper.GetLocalAppDataFolder(), "logs", "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                fileSizeLimitBytes: 10_000_000,
                rollOnFileSizeLimit: true)
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        ConfigureHostConfiguration(config =>
        {
            config.SetBasePath(AppContext.BaseDirectory);
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }).
        UseSerilog(Log.Logger, dispose: true).
        ConfigureServices((context, services) =>
        {
            // Register the logging level switch as a singleton
            services.AddSingleton(levelSwitch);

            // Register services.
            services.AddSingleton<ILogLevelService, LogLevelService>();
            services.AddSingleton<ITrackConfigurationFactory, TrackConfigurationFactory>();
            services.AddSingleton<IBatchTrackConfigurationInitializer, BatchTrackConfigurationInitializer>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationViewService, NavigationViewService>();
            services.AddSingleton<IValidationSettingsService, ValidationSettingsService>();
            services.AddSingleton<IUIPreferencesService, UIPreferencesService>();
            services.AddSingleton<ILanguageProvider, LanguageProvider>();
            services.AddSingleton<IFileScanner, FileScanner>();
            services.AddSingleton<IBatchConfiguration, BatchConfiguration>();
            services.AddSingleton<IWritableSettings<UserSettings>>(sp =>
            {
                IOptions<AppConfigOptions> options = sp.GetRequiredService<IOptions<AppConfigOptions>>();
                ILogger<WritableJsonSettings<UserSettings>> logger = sp.GetRequiredService<ILogger<WritableJsonSettings<UserSettings>>>();
                string userSettingsFilePath = options.Value.UserSettingsPath;
                return new WritableJsonSettings<UserSettings>(logger, userSettingsFilePath);
            });
            services.AddSingleton<IFileListAdapter, FileListAdapter>();
            services.AddSingleton<IFilePickerDialogService, FilePickerDialogService>();
            services.AddSingleton<IBatchReportStore, InMemoryBatchReportStore>();
            services.AddSingleton<IFileProcessingOrchestrator, FileProcessingOrchestrator>();
            services.AddSingleton<IMkvToolExecutor, MkvPropeditExecutor>();
            services.AddSingleton<IProcessRunner, ProcessRunner>();
            services.AddSingleton<IMkvPropeditArgumentsGenerator, MkvPropeditArgumentsGenerator>();
            services.AddSingleton<IScannedFileInfoPathComparer, ScannedFileInfoPathComparer>();
            services.AddSingleton<IPlatformService, PlatformService>();

            // Register file validation rules engine service and it's accommodating rules.
            services.AddSingleton<IFileValidationEngine, FileValidationEngine>();
            services.AddSingleton<IFileValidationRule, LanguageConsistencyRule>();
            services.AddSingleton<IFileValidationRule, TrackCountConsistencyRule>();
            services.AddSingleton<IFileValidationRule, FileFormatValidationRule>();

            // Register file processing rule engine service and it's accommodating rules.
            services.AddSingleton<IFileProcessingEngine, FileProcessingEngine>();
            services.AddSingleton<IFileProcessingRule, TrackPositionRule>();
            services.AddSingleton<IFileProcessingRule, SubtitleTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, AudioTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, VideoTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, TrackLanguageRule>();
            services.AddSingleton<IFileProcessingRule, TrackDefaultRule>();
            services.AddSingleton<IFileProcessingRule, TrackForcedRule>();
            services.AddSingleton<IFileProcessingRule, FileTitleNamingRule>();

            // Register view models.
            services.AddSingleton<InputViewModel, InputViewModel>();
            services.AddSingleton<GeneralViewModel, GeneralViewModel>();
            services.AddSingleton<VideoViewModel, VideoViewModel>();
            services.AddSingleton<AudioViewModel, AudioViewModel>();
            services.AddSingleton<SubtitleViewModel, SubtitleViewModel>();
            services.AddSingleton<OutputViewModel, OutputViewModel>();
            services.AddSingleton<ShellViewModel, ShellViewModel>();
            services.AddSingleton<MainViewModel, MainViewModel>();
            services.AddSingleton<BatchResultsViewModel, BatchResultsViewModel>();
            services.AddSingleton<SettingsViewModel, SettingsViewModel>();

            // Register dialog view models (transient so each dialog gets a fresh instance).
            services.AddTransient<ErrorDialogViewModel>();

            // Register pages.
            services.AddSingleton<Shell>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<InputPage>();

            //Configure the app settings.
            services.AddOptions<LanguageOptions>()
                .Bind(context.Configuration.GetSection(nameof(LanguageOptions)))
                .ValidateDataAnnotations();

            services.AddOptions<ScanOptions>()
                .Bind(context.Configuration.GetSection(nameof(ScanOptions)))
                .ValidateDataAnnotations();

            services.AddOptions<AppConfigOptions>()
                .Bind(context.Configuration.GetSection(nameof(AppConfigOptions)))
                .ValidateDataAnnotations();

            services.AddOptions<LoggingOptions>()
                .Bind(context.Configuration.GetSection(nameof(LoggingOptions)))
                .ValidateDataAnnotations();
        }).
        Build();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to build host during application startup.");

            var innermost = ex.GetBaseException();
            var errorMessage = "The application failed to start due to an unexpected error.\n\n" +
                               $"Error: {innermost.Message}";

            ShowConfigurationErrorDialog(errorMessage, ex.ToString());
            return;
        }

        ApplyConfiguredLogLevel(levelSwitch);

        // Validate all configuration options at startup
        // This ensures invalid appsettings.json values are caught early
        // If validation fails, the app exits after showing an error dialog
        var validationErrors = ValidateAllOptions(Host.Services);
        if (validationErrors.Count != 0)
        {
            // Log raw errors without UI formatting
            Log.Fatal("Configuration validation failed: {Errors}", validationErrors);

            // Format errors for display with bullet points
            var formattedErrors = validationErrors.Select(e => $"• {e}");
            var displayMessage = ConfigValidationErrorPrefix + string.Join("\n\n", formattedErrors);
            ShowConfigurationErrorDialog(displayMessage);
            return; // Exit early, the dialog will handle app termination
        }

        _logger = Host.Services.GetRequiredService<ILogger<App>>();
        LogStartupInfo();

        MainWindow = new MainWindow
        {
            Title = AppDisplayName,
            Content = Host.Services.GetRequiredService<Shell>(),
            ExtendsContentIntoTitleBar = false,
        };

#if DEBUG
        //MainWindow.UseStudio();
#endif

#if WINDOWS10_0_19041_0_OR_GREATER
        MainWindow.SetWindowIcon();
#endif

        // Subscribe to theme changes and apply initial theme
        var uiPreferences = GetService<IUIPreferencesService>();
        uiPreferences.PropertyChanged += OnUIPreferencesChanged;

        ApplyTheme(uiPreferences.AppTheme);

        if (MainWindow?.Content is not null)
        {
            await GetService<IActivationService>().ActivateAsync(args);
        }
    }

    /// <summary>
    /// Validates all registered configuration options at startup by triggering their lazy validation.
    /// </summary>
    /// <param name="services">The service provider containing the registered options.</param>
    /// <returns>A list of validation error messages, or an empty list if all options are valid.</returns>
    /// <remarks>
    /// <para>
    /// This method uses reflection to access <see cref="IOptions{TOptions}.Value"/>, which triggers
    /// the validation configured via <see cref="OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations{TOptions}(OptionsBuilder{TOptions})"/>
    /// during service registration.
    /// </para>
    /// <para>
    /// To add validation for a new option type:
    /// <list type="number">
    ///   <item><description>Add DataAnnotations validation attributes to the option class.</description></item>
    ///   <item><description>Register with <see cref="OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations{TOptions}(OptionsBuilder{TOptions})"/>.</description></item>
    ///   <item><description>Add the type to the <c>optionTypes</c> array in this method.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static List<string> ValidateAllOptions(IServiceProvider services)
    {
        var validationErrors = new List<string>();

        // Add new option types here to include them in startup validation
        var optionTypes = new[]
        {
            typeof(LoggingOptions),
            typeof(LanguageOptions),
            typeof(ScanOptions),
            typeof(AppConfigOptions)
        };

        foreach (var optionType in optionTypes)
        {
            try
            {
                // Construct IOptions<T> type using reflection
                var genericOptionsType = typeof(IOptions<>).MakeGenericType(optionType);
                var optionsInstance = services.GetRequiredService(genericOptionsType);

                // Get the Value property to trigger lazy validation
                var valueProperty = genericOptionsType.GetProperty("Value");
                if (valueProperty is null)
                {
                    continue; // Skip if Value property doesn't exist (defensive)
                }

                // Access Value to trigger validation (result intentionally discarded)
                _ = valueProperty.GetValue(optionsInstance);
            }
            catch (TargetInvocationException ex) when (ex.GetBaseException() is OptionsValidationException)
            {
                // OptionsValidationException concatenates multiple failures with "; "
                // Split them into individual errors
                var errorMessages = ex.GetBaseException().Message
                    .Split("; ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(msg => $"{optionType.Name}: {msg.Trim()}");

                validationErrors.AddRange(errorMessages);
            }
            catch (Exception ex)
            {
                validationErrors.Add($"{optionType.Name}: {ex.GetBaseException().Message}");
            }
        }

        return validationErrors;
    }

    /// <summary>
    /// Shows a configuration error dialog to the user with details about validation failures.
    /// </summary>
    /// <param name="errorMessage">The validation error message(s) to display to the user.</param>
    /// <param name="errorDetails">Optional detailed error information (e.g., stack trace).</param>
    private static void ShowConfigurationErrorDialog(string errorMessage, string? errorDetails = null)
    {
        var errorWindow = new ConfigurationErrorWindow
        {
            ViewModel =
            {
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails ?? string.Empty
            },
            Title = AppDisplayName
        };

        errorWindow.Activate();
    }

    /// <summary>
    /// Handles changes to UI preferences by responding to property change notifications.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void OnUIPreferencesChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IUIPreferencesService.AppTheme) && sender is IUIPreferencesService preferences)
        {
            ApplyTheme(preferences.AppTheme);
        }
    }

    /// <summary>
    /// Applies the specified application theme to the main window and its title bar.
    /// </summary>
    /// <param name="theme">The preferred theme to apply. Must be a valid value of <see cref="AppThemePreference"/>.</param>
    private static void ApplyTheme(AppThemePreference theme)
    {
        // Apply theme to the window's content
        if (MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme switch
            {
                AppThemePreference.Light => ElementTheme.Light,
                AppThemePreference.Dark => ElementTheme.Dark,
                AppThemePreference.System => ElementTheme.Default,
                _ => ElementTheme.Default
            };
        }

        // Apply theme to title bar
        if (MainWindow?.AppWindow?.TitleBar is not null)
        {
            var titleBarTheme = theme switch
            {
                AppThemePreference.Light => Microsoft.UI.Windowing.TitleBarTheme.Light,
                AppThemePreference.Dark => Microsoft.UI.Windowing.TitleBarTheme.Dark,
                AppThemePreference.System => Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode,
                _ => Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode
            };

            // Currently, Uno Skia Desktop does not support setting title bar theme.
#if WINDOWS10_0_19041_0_OR_GREATER
            MainWindow.AppWindow.TitleBar.PreferredTheme = titleBarTheme;
#endif
        }
    }

    /// <summary>
    /// Handles unhandled exceptions globally by logging them and displaying an error dialog.
    /// </summary>
    /// <param name="sender">The source of the unhandled exception event.</param>
    /// <param name="e">The event arguments containing the exception details.</param>
    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Mark as handled to prevent app termination
        e.Handled = true;

        // Log the exception
        try
        {
            if (_logger is not null)
            {
                LogUnhandledException(e.Exception, e.Message);
            }
        }
        catch
        {
            // If logging fails, we still want to show the dialog
            System.Diagnostics.Debug.WriteLine($"Unhandled exception (logging failed): {e.Exception}");
        }

        // Send message to display error dialog
        WeakReferenceMessenger.Default.Send(new ExceptionDialogMessage(
            Title: "Unexpected Error Occurred",
            Summary: "An unexpected error occurred.\nThe application can attempt to continue or safely exit.",
            Exception: e.Exception,
            Timestamp: DateTimeOffset.Now));
    }

    /// <summary>
    /// Applies the configured log level to the LoggingLevelSwitch.
    /// Priority: appsettings.json > UserSettings.json > default (Information).
    /// </summary>
    /// <param name="levelSwitch">The Serilog level switch to configure.</param>
    private void ApplyConfiguredLogLevel(LoggingLevelSwitch levelSwitch)
    {
        var loggingOptions = Host!.Services.GetRequiredService<IOptions<LoggingOptions>>().Value;
        var userSettings = Host.Services.GetRequiredService<IWritableSettings<UserSettings>>();

        // First try appsettings.json, then fall back to UserSettings.json
        var effectiveLogLevel = !string.IsNullOrWhiteSpace(loggingOptions.MinimumLevel)
            ? loggingOptions.MinimumLevel
            : userSettings.Value.UI.LogLevel;

        if (!string.IsNullOrWhiteSpace(effectiveLogLevel) &&
            Enum.TryParse<LogEventLevel>(effectiveLogLevel, ignoreCase: true, out var configuredLevel))
        {
            levelSwitch.MinimumLevel = configuredLevel;
        }
    }
}
