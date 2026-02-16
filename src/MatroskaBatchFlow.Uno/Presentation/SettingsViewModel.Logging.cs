using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// LoggerMessage definitions for <see cref="SettingsViewModel"/>.
/// </summary>
public partial class SettingsViewModel
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save MkvPropedit path enabled setting")]
    private partial void LogSaveMkvPropeditPathEnabledFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save custom MkvPropedit path setting")]
    private partial void LogSaveCustomPathFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save strictness mode setting. Mode: {Mode}")]
    private partial void LogSaveStrictnessFailed(StrictnessMode mode, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save custom validation severity settings")]
    private partial void LogSaveValidationSeverityFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Validation strictness mode changed from {PreviousMode} to {NewMode}")]
    private partial void LogValidationStrictnessChanged(StrictnessMode previousMode, StrictnessMode newMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Validation setting {SettingName} changed from {PreviousValue} to {NewValue}")]
    private partial void LogValidationSeverityChanged(string settingName, ValidationSeverity previousValue, ValidationSeverity newValue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Log level changed to {LogLevel}")]
    private partial void LogLogLevelChanged(string logLevel);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save log level setting")]
    private partial void LogSaveLogLevelFailed(Exception ex);
}
