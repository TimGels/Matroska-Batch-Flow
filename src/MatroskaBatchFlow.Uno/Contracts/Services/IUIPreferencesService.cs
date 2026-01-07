using System.ComponentModel;

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
}
