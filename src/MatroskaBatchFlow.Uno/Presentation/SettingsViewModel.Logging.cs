using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Uno.Logging;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// LoggerMessage definitions for <see cref="SettingsViewModel"/>.
/// </summary>
public partial class SettingsViewModel
{
    [LoggerMessage(EventId = UnoLogEvents.Settings.SaveFailed, Level = LogLevel.Error, Message = "Failed to save MkvPropedit path enabled setting")]
    private partial void LogSaveMkvPropeditPathEnabledFailed(Exception ex);

    [LoggerMessage(EventId = UnoLogEvents.Settings.SaveFailed, Level = LogLevel.Error, Message = "Failed to save custom MkvPropedit path setting")]
    private partial void LogSaveCustomPathFailed(Exception ex);

    [LoggerMessage(EventId = UnoLogEvents.Settings.SaveFailed, Level = LogLevel.Error, Message = "Failed to save strictness mode setting. Mode: {Mode}")]
    private partial void LogSaveStrictnessModeFailed(Exception ex, StrictnessMode mode);

    [LoggerMessage(EventId = UnoLogEvents.Settings.SaveFailed, Level = LogLevel.Error, Message = "Failed to save custom validation severity settings")]
    private partial void LogSaveValidationSeverityFailed(Exception ex);
}
