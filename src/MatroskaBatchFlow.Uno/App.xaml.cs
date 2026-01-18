using System.ComponentModel;
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
using Microsoft.Extensions.Configuration;
using Serilog;

namespace MatroskaBatchFlow.Uno;

public partial class App : Application
{
    /// <summary>
    /// The display name of the application for use in user-facing contexts.
    /// </summary>
    private const string AppDisplayName = "Matroska Batch Flow";

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

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Configure Serilog with file logging
        var logPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs", "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                fileSizeLimitBytes: 10_000_000,
                rollOnFileSizeLimit: true)
            .Enrich.FromLogContext()
            .CreateLogger();

        Log.Information("Application starting - Matroska Batch Flow");

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
            // Register services.
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
                string userSettingsFilePath = options.Value.UserSettingsPath;
                return new WritableJsonSettings<UserSettings>(userSettingsFilePath);
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
        }).
        Build();

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
            var logger = Host?.Services.GetService<ILogger<App>>();
            logger?.LogCritical(e.Exception, "Unhandled exception occurred: {Message}", e.Message);
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
}
