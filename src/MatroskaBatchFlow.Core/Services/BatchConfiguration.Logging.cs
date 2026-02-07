using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

public partial class BatchConfiguration
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "File marked as stale (needs re-scanning): {FilePath}")]
    private partial void LogFileMarkedAsStale(string filePath);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Stale flag cleared for file: {FilePath}")]
    private partial void LogStaleFlagCleared(string filePath);
}
