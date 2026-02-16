namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// LoggerMessage definitions for <see cref="InputViewModel"/>.
/// </summary>
public sealed partial class InputViewModel
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Importing {FileCount} file(s)")]
    private partial void LogImportingFiles(int fileCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipped {DuplicateCount} duplicate file(s)")]
    private partial void LogDuplicatesSkipped(int duplicateCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanned {ScannedCount} file(s)")]
    private partial void LogFilesScanned(int scannedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Re-scanning {Count} file(s) with stale metadata before validation")]
    private partial void LogRescanningStaleFiles(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Re-scanned file: {FilePath}")]
    private partial void LogFileRescanned(string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "File not found during re-scan, removing stale flag: {FilePath}")]
    private partial void LogRescanFailedNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Batch re-scan of {Count} file(s) failed entirely")]
    private partial void LogRescanBatchFailed(int count, Exception ex);
}
