namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="BatchHistoryService"/>.
/// </summary>
public partial class BatchHistoryService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded {Count} batch runs from {FilePath}")]
    private partial void LogLoaded(int count, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignoring batch history file {FilePath} with unsupported format version {Version}")]
    private partial void LogUnsupportedFile(int? version, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load the batch history from {FilePath}")]
    private partial void LogLoadFailed(Exception ex, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to save the batch history to {FilePath}")]
    private partial void LogSaveFailed(Exception ex, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch history limit changed from {PreviousLimit} to {NewLimit}; removed {RemovedCount} oldest batch runs")]
    private partial void LogLimitChanged(int previousLimit, int newLimit, int removedCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removed {RemovedCount} oldest batch runs beyond the limit of {Limit}")]
    private partial void LogOldestRunsRemoved(int removedCount, int limit);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch history cleared; removed {RemovedCount} batch runs")]
    private partial void LogCleared(int removedCount);
}
