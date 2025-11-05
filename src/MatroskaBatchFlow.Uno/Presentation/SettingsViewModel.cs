using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Utilities;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IWritableSettings<UserSettings> _userSettings;

    [ObservableProperty]
    private string customMkvPropeditPath;

    [ObservableProperty]
    private bool isCustomMkvPropeditPathEnabled;

    public SettingsViewModel(IWritableSettings<UserSettings> userSettings)
    {
        _userSettings = userSettings;

        customMkvPropeditPath = ExecutableLocator.FindExecutable(_userSettings.Value.MkvPropedit.CustomPath ?? string.Empty) ?? string.Empty;
        // If Skia is being used, we force the custom path to be enabled since the bundled mkvpropedit executable is only for Windows.
        isCustomMkvPropeditPathEnabled = PlatformInfo.IsSkia || _userSettings.Value.MkvPropedit.IsCustomPathEnabled;
    }

    private async Task SaveSettingsAsync()
    {
        await _userSettings.UpdateAsync(settings =>
        {
            settings.MkvPropedit.CustomPath = ExecutableLocator.FindExecutable(CustomMkvPropeditPath);
            settings.MkvPropedit.IsCustomPathEnabled = IsCustomMkvPropeditPathEnabled;
        });
    }

    /// <summary>
    /// Handles changes to the IsCustomMkvPropeditPathEnabled property.
    /// </summary>
    /// <param name="value">The new value of the property.</param>
    async partial void OnIsCustomMkvPropeditPathEnabledChanged(bool value)
    {
        try
        {
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            // TODO: Handle exception properly (e.g., log it, show a message to the user, etc.)
        }
    }

    /// <summary>
    /// Handles changes to the CustomMkvPropeditPath property.
    /// </summary>
    /// <param name="value">The new value of the property.</param>
    async partial void OnCustomMkvPropeditPathChanged(string value)
    {
        try
        {
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            // TODO: Handle exception properly (e.g., log it, show a message to the user, etc.)
        }
    }
}
