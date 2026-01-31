using System.Diagnostics;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Enums;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Service for managing UI preferences with observable property change notifications.
/// </summary>
public sealed partial class UIPreferencesService : ObservableObject, IUIPreferencesService
{
    private readonly IWritableSettings<UserSettings> _userSettings;

    [ObservableProperty]
    private bool showTrackAvailabilityText;

    [ObservableProperty]
    private AppThemePreference appTheme;

    [ObservableProperty]
    private bool enableLoggingView;

    public UIPreferencesService(IWritableSettings<UserSettings> userSettings)
    {
        _userSettings = userSettings;
        showTrackAvailabilityText = userSettings.Value.UI.ShowTrackAvailabilityText;
        enableLoggingView = userSettings.Value.UI.EnableLoggingView;
        
        // Parse the theme from settings, defaulting to System if invalid
        if (Enum.TryParse<AppThemePreference>(userSettings.Value.UI.Theme, out var parsedTheme))
        {
            appTheme = parsedTheme;
        }
        else
        {
            Debug.WriteLine($"[UIPreferencesService] Unknown theme value '{userSettings.Value.UI.Theme}' in settings, falling back to System");
            appTheme = AppThemePreference.System;
        }
    }

    /// <summary>
    /// Handles changes to the ShowTrackAvailabilityText setting when its value is modified.
    /// </summary>
    /// <param name="value">The new value indicating whether to display track availability text. Set to <see langword="true"/> to show the
    /// text; otherwise, <see langword="false"/>.</param>
    partial void OnShowTrackAvailabilityTextChanged(bool value)
    {
        _ = _userSettings.UpdateAsync(settings => settings.UI.ShowTrackAvailabilityText = value);
    }

    /// <summary>
    /// Handles changes to the application's theme preference and persists the new value to user settings.
    /// </summary>
    /// <param name="value">The new value representing the selected application theme preference.</param>
    partial void OnAppThemeChanged(AppThemePreference value)
    {
        _ = _userSettings.UpdateAsync(settings => settings.UI.Theme = value.ToString());
    }

    /// <summary>
    /// Handles changes to the EnableLoggingView setting and persists the new value to user settings.
    /// </summary>
    /// <param name="value">The new value indicating whether the logging viewer should be enabled.</param>
    partial void OnEnableLoggingViewChanged(bool value)
    {
        _ = _userSettings.UpdateAsync(settings => settings.UI.EnableLoggingView = value);
    }
}
