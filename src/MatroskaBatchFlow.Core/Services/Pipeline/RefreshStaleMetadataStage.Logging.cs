using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// LoggerMessage definitions for <see cref="RefreshStaleMetadataStage"/>.
/// </summary>
public sealed partial class RefreshStaleMetadataStage
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Re-scanning {Count} file(s) with stale metadata before validation")]
    private partial void LogRescanningStaleFiles(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Re-scanned file: {FilePath}")]
    private partial void LogFileRescanned(string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "File not found during re-scan, removing stale flag: {FilePath}")]
    private partial void LogRescanFailedNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Batch re-scan of {Count} file(s) failed entirely")]
    private partial void LogRescanBatchFailed(int count, Exception ex);
}
