namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="TaskbarProgressService"/>.
/// </summary>
public sealed partial class TaskbarProgressService
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Taskbar progress is unavailable and has been disabled")]
    private partial void LogTaskbarProgressUnavailable(Exception ex);
}
