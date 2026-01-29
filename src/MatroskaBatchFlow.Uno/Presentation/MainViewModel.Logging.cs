namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// LoggerMessage definitions for <see cref="MainViewModel"/>.
/// </summary>
public partial class MainViewModel
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch processing aborted: {ErrorMessage}")]
    private partial void LogBatchProcessingAborted(string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Batch processing was cancelled by user")]
    private partial void LogBatchProcessingCancelled();

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during batch processing")]
    private partial void LogBatchProcessingError(Exception ex);
}
