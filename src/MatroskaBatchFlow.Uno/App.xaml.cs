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
    private const string AppName = "Matroska Batch Flow";

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

    public static MainWindow? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
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

        MainWindow = new MainWindow
        {
            Title = AppName,
            Content = Host.Services.GetRequiredService<Shell>(),
        };

#if DEBUG
        //MainWindow.UseStudio();
#endif

#if WINDOWS10_0_19041_0_OR_GREATER
        MainWindow.SetWindowIcon();
#endif

        if (MainWindow?.Content is not null)
        {
            await App.GetService<IActivationService>().ActivateAsync(args);
        }
    }
}
