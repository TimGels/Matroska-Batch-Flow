using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = [];
    private readonly Dictionary<string, Func<object>> _defaultParameters = [];

    public PageService()
    {
        Configure<BatchResultsViewModel, BatchResultsPage>();
        Configure<MainViewModel, MainPage>();
        Configure<InputViewModel, InputPage>();
        // A menu visit needs its own frame parameter so Back can restore the selected check and expanded rows.
        Configure<ValidationViewModel, ValidationPage>(() => new FileCheckVisit());
        Configure<GeneralViewModel, GeneralPage>();
        Configure<VideoViewModel, VideoPage>();
        Configure<AudioViewModel, AudioPage>();
        Configure<SubtitleViewModel, SubtitlePage>();
        Configure<OutputViewModel, OutputPage>();
        Configure<SettingsViewModel, SettingsPage>();
        Configure<LogViewerViewModel, LogViewerPage>();
    }

    public Type GetPageType(string key)
    {
        Type? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out pageType))
            {
                throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
            }
        }

        return pageType;
    }

    public object? CreateDefaultParameter(string key)
    {
        Func<object>? factory;
        lock (_pages)
        {
            _defaultParameters.TryGetValue(key, out factory);
        }

        return factory?.Invoke();
    }

    private void Configure<VM, V>(Func<object>? defaultParameter = null)
        where VM : ObservableObject
        where V : Page
    {
        lock (_pages)
        {
            var key = typeof(VM).FullName!;
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

            var type = typeof(V);
            if (_pages.ContainsValue(type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
            }

            _pages.Add(key, type);
            if (defaultParameter is not null)
            {
                _defaultParameters.Add(key, defaultParameter);
            }
        }
    }
}
