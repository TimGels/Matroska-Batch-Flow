using CommunityToolkit.Mvvm.ComponentModel;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Service for managing UI preferences with observable property change notifications.
/// </summary>
public sealed partial class UIPreferencesService : ObservableObject, IUIPreferencesService
{
    private readonly IWritableSettings<UserSettings> _userSettings;

    [ObservableProperty]
    private bool showTrackAvailabilityText;

    public UIPreferencesService(IWritableSettings<UserSettings> userSettings)
    {
        _userSettings = userSettings;
        showTrackAvailabilityText = userSettings.Value.UI.ShowTrackAvailabilityText;
    }

    partial void OnShowTrackAvailabilityTextChanged(bool value)
    {
        _ = _userSettings.UpdateAsync(settings => settings.UI.ShowTrackAvailabilityText = value);
    }
}
