using System.ComponentModel;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Enums;
using Microsoft.UI.Windowing;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Applies theme settings to the application UI based on user preferences.
/// Observes <see cref="IUIPreferencesService"/> for theme preference changes.
/// </summary>
/// <param name="logger">The logger for theme applier events.</param>
/// <param name="uiPreferences">The UI preferences service to observe for theme changes.</param>
public sealed partial class ThemeApplierService(ILogger<ThemeApplierService> logger, IUIPreferencesService uiPreferences) : IThemeApplierService
{
    /// <inheritdoc />
    public void Initialize()
    {
        uiPreferences.PropertyChanged += OnUIPreferencesChanged;
        ApplyTheme(uiPreferences.AppTheme);
        LogThemeApplierInitialized(uiPreferences.AppTheme);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        uiPreferences.PropertyChanged -= OnUIPreferencesChanged;
    }

    /// <summary>
    /// Applies the specified theme to the application window and title bar.
    /// </summary>
    private static void ApplyTheme(AppThemePreference theme)
    {
        // Apply theme to the window's content
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme switch
            {
                AppThemePreference.Light => ElementTheme.Light,
                AppThemePreference.Dark => ElementTheme.Dark,
                AppThemePreference.System => ElementTheme.Default,
                _ => ElementTheme.Default
            };
        }

        // Currently, Uno Skia Desktop does not support setting title bar theme.
#if WINDOWS10_0_19041_0_OR_GREATER
        // Apply theme to title bar
        if (App.MainWindow?.AppWindow?.TitleBar is not null)
        {
            var titleBarTheme = theme switch
            {
                AppThemePreference.Light => TitleBarTheme.Light,
                AppThemePreference.Dark => TitleBarTheme.Dark,
                AppThemePreference.System => TitleBarTheme.UseDefaultAppMode,
                _ => TitleBarTheme.UseDefaultAppMode
            };

            App.MainWindow.AppWindow.TitleBar.PreferredTheme = titleBarTheme;
        }
#endif
    }

    /// <summary>
    /// Handles changes to UI preferences by responding to property change notifications.
    /// </summary>
    private void OnUIPreferencesChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IUIPreferencesService.AppTheme))
        {
            ApplyTheme(uiPreferences.AppTheme);
            LogThemeChanged(uiPreferences.AppTheme);
        }
    }
}
