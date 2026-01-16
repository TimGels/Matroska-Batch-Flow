using System.ComponentModel;
using MatroskaBatchFlow.Uno.Enums;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Service for managing UI preferences with observable property change notifications.
/// </summary>
public interface IUIPreferencesService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets whether track availability text should be shown alongside dots in dropdowns.
    /// </summary>
    bool ShowTrackAvailabilityText { get; set; }

    /// <summary>
    /// Gets or sets the application theme preference.
    /// </summary>
    AppThemePreference AppTheme { get; set; }
}
