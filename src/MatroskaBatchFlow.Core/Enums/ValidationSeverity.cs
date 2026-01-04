namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Defines the severity level for batch validation rules.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Don't check this validation at all.
    /// </summary>
    Off,

    /// <summary>
    /// Show information message, non-blocking.
    /// </summary>
    Info,

    /// <summary>
    /// Show warning message, non-blocking.
    /// </summary>
    Warning,

    /// <summary>
    /// Show error message, blocks file addition.
    /// </summary>
    Error
}
