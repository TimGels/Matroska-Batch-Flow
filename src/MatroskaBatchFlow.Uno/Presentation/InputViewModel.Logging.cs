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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Validation blocked import: {ErrorCount} error(s) found")]
    private partial void LogValidationBlocked(int errorCount);
}
