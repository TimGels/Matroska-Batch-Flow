using MatroskaBatchFlow.Core.Logging;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// LoggerMessage definitions for <see cref="FileValidationEngine"/>.
/// </summary>
public partial class FileValidationEngine
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Validating {FileCount} file(s) with {RuleCount} validation rule(s)")]
    private partial void LogValidationStarted(int fileCount, int ruleCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Validation completed: {ErrorCount} error(s), {WarningCount} warning(s)")]
    private partial void LogValidationCompleted(int errorCount, int warningCount);

    [LoggerMessage(EventId = CoreLogEvents.Validation.ValidationError, Level = LogLevel.Error, Message = "Validation error for {FilePath}: {ValidationMessage}")]
    private partial void LogValidationError(string filePath, string validationMessage);

    [LoggerMessage(EventId = CoreLogEvents.Validation.ValidationWarning, Level = LogLevel.Warning, Message = "Validation warning for {FilePath}: {ValidationMessage}")]
    private partial void LogValidationWarning(string filePath, string validationMessage);
}
