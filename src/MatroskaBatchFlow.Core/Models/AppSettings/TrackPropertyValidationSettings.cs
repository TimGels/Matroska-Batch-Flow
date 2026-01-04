using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

/// <summary>
/// Configures validation strictness for track properties.
/// </summary>
public sealed class TrackPropertyValidationSettings
{
    /// <summary>
    /// Validation severity for track language consistency.
    /// </summary>
    public ValidationSeverity Language { get; set; } = ValidationSeverity.Warning;

    /// <summary>
    /// Validation severity for default flag consistency.
    /// </summary>
    public ValidationSeverity DefaultFlag { get; set; } = ValidationSeverity.Off;

    /// <summary>
    /// Validation severity for forced flag consistency.
    /// </summary>
    public ValidationSeverity ForcedFlag { get; set; } = ValidationSeverity.Off;

    /// <summary>
    /// Validation severity for track name consistency.
    /// </summary>
    public ValidationSeverity TrackName { get; set; } = ValidationSeverity.Off;

    /// <summary>
    /// Validation severity for codec consistency.
    /// </summary>
    public ValidationSeverity Codec { get; set; } = ValidationSeverity.Off;
}
