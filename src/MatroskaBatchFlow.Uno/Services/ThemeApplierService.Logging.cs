using MatroskaBatchFlow.Uno.Enums;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="ThemeApplierService"/>.
/// </summary>
public sealed partial class ThemeApplierService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Theme applier initialized with theme: {Theme}")]
    private partial void LogThemeApplierInitialized(AppThemePreference theme);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Theme changed to: {Theme}")]
    private partial void LogThemeChanged(AppThemePreference theme);
}
