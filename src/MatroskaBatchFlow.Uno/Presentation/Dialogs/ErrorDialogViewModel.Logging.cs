namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

/// <summary>
/// LoggerMessage definitions for <see cref="ErrorDialogViewModel"/>.
/// </summary>
public partial class ErrorDialogViewModel
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Exception details copied to clipboard")]
    private partial void LogDetailsCopied();

    [LoggerMessage(Level = LogLevel.Information, Message = "Error report saved to: {FilePath}")]
    private partial void LogErrorReportSaved(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save error report")]
    private partial void LogErrorReportSaveFailed(Exception ex);
}
