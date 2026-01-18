using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

/// <summary>
/// ViewModel for the error dialog that displays unhandled exception details.
/// </summary>
public partial class ErrorDialogViewModel : ObservableObject
{
    private readonly ILogger<ErrorDialogViewModel> _logger;

    [ObservableProperty]
    private string title = "Unexpected Error Occurred";

    [ObservableProperty]
    private string summary = "An error occurred while processing your files.";

    [ObservableProperty]
    private string exceptionType = string.Empty;

    [ObservableProperty]
    private string exceptionMessage = string.Empty;

    [ObservableProperty]
    private string stackTrace = string.Empty;

    [ObservableProperty]
    private string technicalDetails = string.Empty;

    [ObservableProperty]
    private DateTimeOffset timestamp = DateTimeOffset.Now;

    [ObservableProperty]
    private bool isDetailsExpanded;

    [ObservableProperty]
    private string? logFilePath;

    public ErrorDialogViewModel(ILogger<ErrorDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the ViewModel with exception data.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="summary">A user-friendly summary of the error.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="timestamp">When the exception occurred.</param>
    public void Initialize(string title, string summary, Exception exception, DateTimeOffset timestamp)
    {
        Title = title;
        Summary = summary;
        ExceptionType = exception.GetType().Name;
        ExceptionMessage = exception.Message;
        StackTrace = FormatStackTrace(exception);
        Timestamp = timestamp;
        TechnicalDetails = FormatTechnicalDetails(exception);
        IsDetailsExpanded = false;
        LogFilePath = null;
    }

    /// <summary>
    /// Gets the formatted exception details for display or copying.
    /// </summary>
    public string FormattedDetails => FormatFullDetails();

    /// <summary>
    /// Copies the full exception details to the clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyDetails()
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(TechnicalDetails);
        Clipboard.SetContent(dataPackage);

        LogDetailsCopied();
    }

    /// <summary>
    /// Saves the exception details to a log file.
    /// </summary>
    [RelayCommand]
    private async Task SaveLogAsync()
    {
        try
        {
            var logsFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("logs", CreationCollisionOption.OpenIfExists);
            var fileName = $"error-report-{Timestamp:yyyyMMdd-HHmmss}.txt";
            var logFile = await logsFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

            await FileIO.WriteTextAsync(logFile, FormattedDetails);

            LogFilePath = logFile.Path;
            LogErrorReportSaved(LogFilePath);
        }
        catch (Exception ex)
        {
            LogErrorReportSaveFailed(ex);
        }
    }

    /// <summary>
    /// Formats the complete exception details including stack trace.
    /// </summary>
    private string FormatFullDetails()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Exception Details ===");
        sb.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}");
        sb.AppendLine($"Exception: {ExceptionType}");
        sb.AppendLine($"Message: {ExceptionMessage}");
        sb.AppendLine();
        sb.AppendLine("=== Stack Trace ===");
        sb.AppendLine(StackTrace);
        return sb.ToString();
    }

    /// <summary>
    /// Formats the technical details shown in the expander - includes all info needed for a bug report.
    /// </summary>
    private string FormatTechnicalDetails(Exception exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}");
        sb.AppendLine($"Exception: {exception.GetType().Name}");
        sb.AppendLine($"Message: {exception.Message}");
        sb.AppendLine();
        sb.AppendLine("Stack Trace:");
        sb.Append(FormatStackTrace(exception));
        return sb.ToString();
    }

    /// <summary>
    /// Formats the stack trace from an exception, including inner exceptions.
    /// </summary>
    private static string FormatStackTrace(Exception exception)
    {
        var sb = new StringBuilder();

        var current = exception;
        var depth = 0;

        while (current != null)
        {
            if (depth > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"--- Inner Exception ({depth}) ---");
                sb.AppendLine($"Type: {current.GetType().Name}");
                sb.AppendLine($"Message: {current.Message}");
            }

            if (!string.IsNullOrWhiteSpace(current.StackTrace))
            {
                sb.AppendLine(current.StackTrace);
            }

            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }
}
