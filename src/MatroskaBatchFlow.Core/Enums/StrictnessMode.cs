namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Defines the batch validation strictness mode.
/// </summary>
public enum StrictnessMode
{
    /// <summary>
    /// Strict validation - enforces track parity and consistency.
    /// </summary>
    Strict,

    /// <summary>
    /// Lenient validation - allows differences, provides info.
    /// </summary>
    Lenient,

    /// <summary>
    /// Custom validation - user-configured rules.
    /// </summary>
    Custom
}
