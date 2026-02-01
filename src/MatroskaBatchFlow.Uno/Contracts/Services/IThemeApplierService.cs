namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Applies theme settings to the application UI based on user preferences.
/// </summary>
public interface IThemeApplierService : IDisposable
{
    /// <summary>
    /// Initializes theme handling by subscribing to UI preference changes and applying the initial theme.
    /// Must be called after the main window is created.
    /// </summary>
    void Initialize();
}
