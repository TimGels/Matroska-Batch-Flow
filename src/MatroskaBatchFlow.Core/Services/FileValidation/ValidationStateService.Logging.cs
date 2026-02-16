using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// LoggerMessage definitions for <see cref="ValidationStateService"/>.
/// </summary>
public sealed partial class ValidationStateService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Re-validation triggered by file list change: {Action}")]
    private partial void LogFileListChangeTriggered(string action);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Validation skipped: no files in batch")]
    private partial void LogValidationSkipped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Validation state updated: {TotalCount} result(s) - {ErrorCount} error(s), {WarningCount} warning(s), {InfoCount} info")]
    private partial void LogValidationCompleted(int totalCount, int errorCount, int warningCount, int infoCount);
}
