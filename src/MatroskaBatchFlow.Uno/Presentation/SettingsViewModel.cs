using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Utilities;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IOptionsMonitor<AppConfigOptions> _appOptions;
    private readonly IWritableSettings<UserSettings> _userSettings;

    [ObservableProperty]
    private string customMkvPropeditPath;

    [ObservableProperty]
    private bool isCustomMkvPropeditPathEnabled;

    public ICommand SaveSettingsCommand { get; }

    public SettingsViewModel(IOptionsMonitor<AppConfigOptions> appOptions, IWritableSettings<UserSettings> userSettings)
    {
        _appOptions = appOptions;
        _userSettings = userSettings;
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettings);

        customMkvPropeditPath = ExecutableLocator.FindExecutable(_userSettings.Value.MkvPropedit.CustomPath ?? string.Empty) ?? string.Empty;
        isCustomMkvPropeditPathEnabled = _userSettings.Value.MkvPropedit.IsCustomPathEnabled;
    }

    private async Task SaveSettings()
    {
        await _userSettings.UpdateAsync(settings =>
        {
            settings.MkvPropedit.CustomPath = ExecutableLocator.FindExecutable(CustomMkvPropeditPath);
            settings.MkvPropedit.IsCustomPathEnabled = IsCustomMkvPropeditPathEnabled;
        });
    }
}
