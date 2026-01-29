using MatroskaBatchFlow.Core.Logging;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// LoggerMessage definitions for <see cref="MkvPropeditExecutor"/>.
/// </summary>
public partial class MkvPropeditExecutor
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to resolve mkvpropedit executable: {Error}")]
    private partial void LogExecutableResolutionFailed(string error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing mkvpropedit: {Executable} {Arguments}")]
    private partial void LogExecutingCommand(string executable, string arguments);

    [LoggerMessage(Level = LogLevel.Debug, Message = "mkvpropedit completed with exit code {ExitCode}")]
    private partial void LogCommandCompleted(int exitCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception executing mkvpropedit with arguments: {Arguments}")]
    private partial void LogExecutionException(Exception ex, string arguments);
}
