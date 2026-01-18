using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="FileScanner"/>.
/// </summary>
public partial class FileScanner
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanning {FileCount} file(s) with MediaInfo")]
    private partial void LogScanningFiles(int fileCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scan completed. {ScannedCount} file(s) analyzed")]
    private partial void LogScanCompleted(int scannedCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanning directory: {DirectoryPath}, Recursive: {Recursive}")]
    private partial void LogScanningDirectory(string directoryPath, bool recursive);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found {FileCount} file(s) matching filter criteria")]
    private partial void LogFilesFound(int fileCount);
}
