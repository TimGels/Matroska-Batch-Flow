namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="FileListAdapter"/>.
/// </summary>
public partial class FileListAdapter
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Added {AddedCount} file(s) to batch")]
    private partial void LogFilesAdded(int addedCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removing file from batch: {FilePath}")]
    private partial void LogRemovingFile(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Clearing all files from batch")]
    private partial void LogClearingFiles();
}
