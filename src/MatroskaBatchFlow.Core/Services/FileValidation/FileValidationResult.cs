using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Represents the severity level of a file validation result.
/// </summary>
[Obsolete("Use MatroskaBatchFlow.Core.Enums.ValidationSeverity instead. This enum is maintained for backward compatibility.")]
public enum FileValidationSeverity
{
    /// <summary>
    /// Reserved for future use, currently not used in validation rules.
    /// </summary>
    Info,
    /// <summary>
    /// Represents a warning message, indicating a potential issue that does not 
    /// prevent processing but should be noted. 
    /// </summary>
    /// <remarks> Use this severity for issues that may affect the quality or consistency of 
    /// the media files, such as inconsistent languages across files.</remarks>
    Warning,

    /// <summary>
    /// Represents an error message, indicating a significant issue that prevents the file 
    /// from being processed safely or correctly.
    /// </summary>
    /// <remarks> Use this severity for critical issues that must be resolved before 
    /// processing can continue, such as mismatched track counts or missing metadata.</remarks>
    Error
}

/// <summary>
/// Represents the result of a file validation operation, including the severity of the validation result and an
/// associated message.
/// </summary>
/// <param name="Severity">The severity of the validation result.</param>
/// <param name="ValidatedFilePath"> The file path of the file that was validated during validation.</param>
/// <param name="Message">A message providing details about the validation result.</param>
public sealed record FileValidationResult(
    ValidationSeverity Severity,
    string ValidatedFilePath,
    string Message)
{
    /// <summary>
    /// Gets whether this result is an error (blocks file addition).
    /// </summary>
    public bool IsError => Severity == ValidationSeverity.Error;

    /// <summary>
    /// Gets whether this result is a warning (non-blocking).
    /// </summary>
    public bool IsWarning => Severity == ValidationSeverity.Warning;

    /// <summary>
    /// Gets whether this result is informational (non-blocking).
    /// </summary>
    public bool IsInfo => Severity == ValidationSeverity.Info;

    /// <summary>
    /// Gets whether this result blocks file addition.
    /// </summary>
    public bool IsBlocking => Severity == ValidationSeverity.Error;
}
