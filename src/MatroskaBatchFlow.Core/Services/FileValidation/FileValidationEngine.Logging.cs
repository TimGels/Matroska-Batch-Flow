using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// LoggerMessage definitions for <see cref="FileValidationEngine"/>.
/// </summary>
public partial class FileValidationEngine
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Validation rules: validating {FileCount} file(s) with {RuleCount} rule(s) using {Mode} mode")]
    private partial void LogValidationStarted(int fileCount, int ruleCount, string mode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Validation rule error for {FilePath}: {ValidationMessage}")]
    private partial void LogValidationError(string filePath, string validationMessage);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation rule warning for {FilePath}: {ValidationMessage}")]
    private partial void LogValidationWarning(string filePath, string validationMessage);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Validation rule info for {FilePath}: {ValidationMessage}")]
    private partial void LogValidationInfo(string filePath, string validationMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rule {RuleName} produced {ResultCount} result(s) for {FileCount} file(s)")]
    private partial void LogRuleCompleted(string ruleName, int resultCount, int fileCount);
}
