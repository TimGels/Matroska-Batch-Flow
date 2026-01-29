using MatroskaBatchFlow.Uno.Logging;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="WritableJsonSettings{T}"/>.
/// </summary>
public partial class WritableJsonSettings<T>
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Initializing settings from {FilePath}")]
    private partial void LogInitializingSettings(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Settings file not found, using defaults: {FilePath}")]
    private partial void LogSettingsFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deserialize settings from {FilePath}, using defaults")]
    private partial void LogDeserializationFailed(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Settings loaded successfully from {FilePath}")]
    private partial void LogSettingsLoaded(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load settings from {FilePath}, using defaults")]
    private partial void LogSettingsLoadFailed(Exception ex, string filePath);
}
