namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="ActivationService"/>.
/// </summary>
public partial class ActivationService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting application activation")]
    private partial void LogActivationStarting();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Shell loaded")]
    private partial void LogShellLoaded();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initialization completed")]
    private partial void LogInitializationCompleted();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Activation handlers completed")]
    private partial void LogActivationHandlersCompleted();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Application activation completed")]
    private partial void LogActivationCompleted();
}
