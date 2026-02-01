using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Extensions;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Services;
using MatroskaBatchFlow.Uno.Utilities;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

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

        // Always create the logging view service to capture logs from startup
        var loggingViewService = new LoggingViewService();

        // Load and validate logging options, then create logger
        var logger = InitializeLogger(levelSwitch, loggingViewService);
        if (logger is null)
        {
            return; // Error dialog already shown
        }

        Log.Logger = logger;

        // Build the host with all services
        if (!TryBuildHost(levelSwitch, loggingViewService))
        {
            return; // Error dialog already shown
        }

        // Resolve log level service to apply configured log level
        _ = Host!.Services.GetRequiredService<ILogLevelService>();

        var configValidator = new ConfigurationValidator(Host!.Services.GetRequiredService<ILogger<ConfigurationValidator>>());
        var validationErrors = configValidator.ValidateAllOptions(Host!.Services);
        if (validationErrors.Count != 0)
        {
            Log.Fatal("Configuration validation failed: {Errors}", validationErrors);
            var formattedErrors = validationErrors.Select(e => $"• {e}");
            var displayMessage = ConfigValidationErrorPrefix + string.Join("\n\n", formattedErrors);
            ShowConfigurationErrorDialog(displayMessage);
            return;
        }

        _logger = Host!.Services.GetRequiredService<ILogger<App>>();
        LogStartupInfo();

        // Create and configure the main window
        MainWindow = CreateMainWindow(loggingViewService);

        // Initialize theme applier (must be after window creation)
        GetService<IThemeApplierService>().Initialize();

        if (MainWindow?.Content is not null)
        {
            await GetService<IActivationService>().ActivateAsync(args);
        }
    }

    /// <summary>
    /// Initializes the Serilog logger with validated configuration.
    /// </summary>
    /// <returns>The configured logger, or null if validation failed.</returns>
    private static Logger? InitializeLogger(LoggingLevelSwitch levelSwitch, LoggingViewService loggingViewService)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var loggingOptions = LoadLoggingOptions(configuration, out var validationErrors);
        if (loggingOptions is null)
        {
            var formattedErrors = validationErrors.Select(e => $"• {e}");
            var displayMessage = ConfigValidationErrorPrefix + string.Join("\n\n", formattedErrors);
            ShowConfigurationErrorDialog(displayMessage);
            return null;
        }

        var logPath = Path.Combine(AppPathHelper.GetLocalAppDataFolder(), "logs", "log-.txt");
        return LoggingFactory.CreateAppLogger(levelSwitch, loggingViewService, loggingOptions, logPath);
    }

    /// <summary>
    /// Builds the application host with all registered services.
    /// </summary>
    /// <returns>True if host was built successfully, false otherwise.</returns>
    private bool TryBuildHost(LoggingLevelSwitch levelSwitch, LoggingViewService loggingViewService)
    {
        try
        {
            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.SetBasePath(AppContext.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .UseSerilog(Log.Logger, dispose: true)
                .ConfigureServices((context, services) =>
                {
                    services.AddCoreServices(levelSwitch, loggingViewService);
                    services.AddFileValidationRules();
                    services.AddFileProcessingRules();
                    services.AddViewModels();
                    services.AddPages();
                    services.AddUserSettings();
                    services.AddAppConfiguration(context.Configuration);
                    services.AddThemeServices();
                })
                .Build();

            return true;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to build host during application startup.");
            var innermost = ex.GetBaseException();
            var errorMessage = "The application failed to start due to an unexpected error.\n\n" +
                               $"Error: {innermost.Message}";
            ShowConfigurationErrorDialog(errorMessage, ex.ToString());
            return false;
        }
    }

    /// <summary>
    /// Creates and configures the main application window.
    /// </summary>
    private MainWindow CreateMainWindow(LoggingViewService loggingViewService)
    {
        var window = new MainWindow
        {
            Title = AppDisplayName,
            Content = Host!.Services.GetRequiredService<Shell>(),
            ExtendsContentIntoTitleBar = false,
        };

        // Set the dispatcher queue for the logging view service to enable UI thread marshalling
        loggingViewService.SetDispatcherQueue(window.DispatcherQueue);

#if DEBUG
        //window.UseStudio();
#endif

#if WINDOWS10_0_19041_0_OR_GREATER
        window.SetWindowIcon();
#endif

        return window;
    }

    /// <summary>
    /// Loads and validates logging options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="validationErrors">The validation errors, if any.</param>
    /// <returns>The validated logging options, or null if validation failed.</returns>
    private static LoggingOptions? LoadLoggingOptions(IConfiguration configuration, out List<string> validationErrors)
    {
        validationErrors = [];
        var loggingOptions = new LoggingOptions();
        configuration.GetSection(nameof(LoggingOptions)).Bind(loggingOptions);

        var validationContext = new ValidationContext(loggingOptions);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(loggingOptions, validationContext, validationResults, validateAllProperties: true))
        {
            validationErrors = validationResults
                .Where(r => r.ErrorMessage is not null)
                .Select(r => r.ErrorMessage!)
                .ToList();

            return null;
        }

        return loggingOptions;
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
}
