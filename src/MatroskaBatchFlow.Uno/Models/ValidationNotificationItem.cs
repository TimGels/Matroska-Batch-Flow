using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// Represents a validation notification to be displayed in the UI.
/// </summary>
public sealed record ValidationNotificationItem
{
    /// <summary>
    /// Gets the severity level of the validation result.
    /// </summary>
    public ValidationSeverity Severity { get; init; }

    /// <summary>
    /// Gets the file path associated with the validation result.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the validation message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this notification represents an error that blocks file addition.
    /// </summary>
    public bool IsError => Severity == ValidationSeverity.Error;

    /// <summary>
    /// Gets whether this notification represents a warning.
    /// </summary>
    public bool IsWarning => Severity == ValidationSeverity.Warning;

    /// <summary>
    /// Gets whether this notification represents informational content.
    /// </summary>
    public bool IsInfo => Severity == ValidationSeverity.Info;
}
