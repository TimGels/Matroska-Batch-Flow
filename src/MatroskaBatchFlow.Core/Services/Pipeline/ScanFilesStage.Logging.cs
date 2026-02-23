using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// LoggerMessage definitions for <see cref="ScanFilesStage"/>.
/// </summary>
public sealed partial class ScanFilesStage
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanning {FileCount} file(s) with MediaInfo")]
    private partial void LogScanningFiles(int fileCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scanned {ScannedCount} file(s)")]
    private partial void LogFilesScanned(int scannedCount);
}
