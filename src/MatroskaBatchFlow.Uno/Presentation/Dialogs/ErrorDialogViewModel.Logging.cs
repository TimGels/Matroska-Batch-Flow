using MatroskaBatchFlow.Uno.Logging;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

/// <summary>
/// LoggerMessage definitions for <see cref="ErrorDialogViewModel"/>.
/// </summary>
public partial class ErrorDialogViewModel
{
    [LoggerMessage(EventId = UnoLogEvents.ErrorDialog.DetailsCopied, Level = LogLevel.Information, Message = "Exception details copied to clipboard")]
    private partial void LogDetailsCopied();

    [LoggerMessage(EventId = UnoLogEvents.ErrorDialog.LogSaved, Level = LogLevel.Information, Message = "Error report saved to: {FilePath}")]
    private partial void LogErrorReportSaved(string filePath);

    [LoggerMessage(EventId = UnoLogEvents.ErrorDialog.SaveLogFailed, Level = LogLevel.Error, Message = "Failed to save error report")]
    private partial void LogErrorReportSaveFailed(Exception ex);
}
