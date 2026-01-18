using MatroskaBatchFlow.Core.Logging;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// LoggerMessage definitions for <see cref="ProcessRunner"/>.
/// </summary>
public sealed partial class ProcessRunner
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting process: {FileName}")]
    private partial void LogProcessStarting(string fileName);

    [LoggerMessage(EventId = CoreLogEvents.ToolExecution.ProcessStartFailed, Level = LogLevel.Error, Message = "Failed to start process: {FileName}")]
    private partial void LogProcessStartFailed(string fileName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Process {FileName} was canceled")]
    private partial void LogProcessCanceled(string fileName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Process {FileName} exited with code {ExitCode}")]
    private partial void LogProcessExited(string fileName, int exitCode);
}
