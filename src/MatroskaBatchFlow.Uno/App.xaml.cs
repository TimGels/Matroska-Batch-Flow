using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Activation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Services;
using Serilog.Core;

namespace MatroskaBatchFlow.Uno;
public partial class App : Application
{
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
#if DEBUG
        RequestedTheme = ApplicationTheme.Dark;
#endif
    }

    public static Window MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
                .UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                // Register Json serializers (ISerializer and ISerializer)
                .UseSerialization((context, services) => services
                    .AddContentSerializer(context)
                    .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                .UseHttp((context, services) =>
                {
#if DEBUG
                    // DelegatingHandler will be automatically injected
                    services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif
                    services.AddSingleton<IWeatherCache, WeatherCache>();
                    services.AddRefitClient<IApiClient>(context);

                })
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif

#if WINDOWS10_0_19041_0_OR_GREATER
        MainWindow.SetWindowIcon();
#endif

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        ConfigureServices((context, services) =>
        {
            // Register services.
            services.AddSingleton<IBatchTrackCountSynchronizer, BatchTrackCountSynchronizer>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationViewService, NavigationViewService>();
            services.AddSingleton<ILanguageProvider, LanguageProvider>();
            services.AddSingleton<IFileScanner, FileScanner>();
            services.AddSingleton<IBatchConfiguration, BatchConfiguration>();
            services.AddSingleton<ILogger, Logger<Logger>>();

            // Register file validation rules engine service and it's accomadating rules.
            services.AddSingleton<IFileValidationEngine, FileValidationEngine>();
            services.AddSingleton<IFileValidationRule, LanguageConsistencyRule>();
            services.AddSingleton<IFileValidationRule, TrackCountConsistencyRule>();

            // Register file processing rule engine service and it's accomadating rules.
            services.AddSingleton<IFileProcessingEngine, FileProcessingEngine>();
            services.AddSingleton<IFileProcessingRule, SubtitleTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, AudioTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, VideoTrackNamingRule>();
            services.AddSingleton<IFileProcessingRule, TrackPositionRule>();
            services.AddSingleton<IFileProcessingRule, TrackLanguageRule>();

            // Register view models.
            services.AddSingleton<InputViewModel, InputViewModel>();
            services.AddSingleton<GeneralViewModel, GeneralViewModel>();
            services.AddSingleton<VideoViewModel, VideoViewModel>();
            services.AddSingleton<AudioViewModel, AudioViewModel>();
            services.AddSingleton<SubtitleViewModel, SubtitleViewModel>();
            services.AddSingleton<OutputViewModel, OutputViewModel>();
            services.AddSingleton<ShellViewModel, ShellViewModel>();
            services.AddSingleton<MainViewModel, MainViewModel>();

            // Register pages.
            services.AddSingleton<Shell>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<InputPage>();

            //Configure the app settings.
            services.Configure<LanguageOptions>(context.Configuration.GetSection(nameof(LanguageOptions)));
            services.Configure<ScanOptions>(context.Configuration.GetSection(nameof(ScanOptions)));
        }).
        Build();

        // Only initialize the application if the main window is not already set.
        if (MainWindow.Content is not Shell)
        {
            await App.GetService<IActivationService>().ActivateAsync(args);
        }
    }
}
