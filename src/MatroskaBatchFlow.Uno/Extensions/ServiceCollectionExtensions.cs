using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Uno.Activation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using MatroskaBatchFlow.Uno.Services;
using Serilog.Core;

namespace MatroskaBatchFlow.Uno.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers core application services with the dependency injection container.
        /// </summary>
        /// <param name="levelSwitch">The Serilog logging level switch for dynamic log level control.</param>
        /// <param name="loggingViewService">The logging view service instance for UI log display.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddCoreServices(LoggingLevelSwitch levelSwitch, ILoggingViewService loggingViewService)
        {
            ArgumentNullException.ThrowIfNull(levelSwitch);
            ArgumentNullException.ThrowIfNull(loggingViewService);

            // Register the logging level switch as a singleton
            services.AddSingleton(levelSwitch);

            // Register the logging view service (always active, setting only controls UI visibility)
            services.AddSingleton(loggingViewService);

            // Register activation services
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Register navigation services
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationViewService, NavigationViewService>();

            // Register application services
            services.AddSingleton<ILogLevelService, LogLevelService>();
            services.AddSingleton<ITrackConfigurationFactory, TrackConfigurationFactory>();
            services.AddSingleton<IBatchTrackConfigurationInitializer, BatchTrackConfigurationInitializer>();
            services.AddSingleton<IValidationSettingsService, ValidationSettingsService>();
            services.AddSingleton<IUIPreferencesService, UIPreferencesService>();
            services.AddSingleton<ILanguageProvider, LanguageProvider>();
            services.AddSingleton<IFileScanner, FileScanner>();
            services.AddSingleton<IBatchConfiguration, BatchConfiguration>();
            services.AddSingleton<IFileListAdapter, FileListAdapter>();
            services.AddSingleton<IFilePickerDialogService, FilePickerDialogService>();
            services.AddSingleton<IBatchReportStore, InMemoryBatchReportStore>();
            services.AddSingleton<IFileProcessingOrchestrator, FileProcessingOrchestrator>();
            services.AddSingleton<IMkvToolExecutor, MkvPropeditExecutor>();
            services.AddSingleton<IProcessRunner, ProcessRunner>();
            services.AddSingleton<IMkvPropeditArgumentsGenerator, MkvPropeditArgumentsGenerator>();
            services.AddSingleton<IScannedFileInfoPathComparer, ScannedFileInfoPathComparer>();
            services.AddSingleton<IPlatformService, PlatformService>();

            return services;
        }

        /// <summary>
        /// Registers file validation rules with the dependency injection container.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddFileValidationRules()
        {
            services.AddSingleton<IFileValidationEngine, FileValidationEngine>();
            services.AddSingleton<IFileValidationRule, LanguageConsistencyRule>();
            services.AddSingleton<IFileValidationRule, TrackCountConsistencyRule>();
            services.AddSingleton<IFileValidationRule, FileFormatValidationRule>();
            services.AddSingleton<IFileValidationRule, DefaultFlagConsistencyRule>();

            return services;
        }

        /// <summary>
        /// Registers file processing rules with the dependency injection container.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddFileProcessingRules()
        {
            services.AddSingleton<IFileProcessingEngine, FileProcessingEngine>();
            services.AddSingleton<IFileProcessingRule, TrackPositionRule>();
            services.AddSingleton<IFileProcessingRule, SubtitleTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, AudioTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, VideoTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, TrackLanguageRule>();
            services.AddSingleton<IFileProcessingRule, TrackDefaultRule>();
            services.AddSingleton<IFileProcessingRule, TrackForcedRule>();
            services.AddSingleton<IFileProcessingRule, FileTitleNamingRule>();

            return services;
        }

        /// <summary>
        /// Registers view models with the dependency injection container.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddViewModels()
        {
            // Register singleton view models
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
            services.AddSingleton<LogViewerViewModel, LogViewerViewModel>();

            // Register transient dialog view models (each dialog gets a fresh instance)
            services.AddTransient<ErrorDialogViewModel>();

            return services;
        }

        /// <summary>
        /// Registers pages with the dependency injection container.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddPages()
        {
            services.AddSingleton<Shell>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<InputPage>();

            return services;
        }

        /// <summary>
        /// Registers the writable user settings service with the dependency injection container.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddUserSettings()
        {
            services.AddSingleton<IWritableSettings<UserSettings>>(sp =>
            {
                IOptions<AppConfigOptions> options = sp.GetRequiredService<IOptions<AppConfigOptions>>();
                ILogger<WritableJsonSettings<UserSettings>> logger = sp.GetRequiredService<ILogger<WritableJsonSettings<UserSettings>>>();
                string userSettingsFilePath = options.Value.UserSettingsPath;
                return new WritableJsonSettings<UserSettings>(logger, userSettingsFilePath);
            });

            return services;
        }

        /// <summary>
        /// Registers theme services with the dependency injection container.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddThemeServices()
        {
            services.AddSingleton<IThemeApplierService, ThemeApplierService>();

            return services;
        }
    }
}
