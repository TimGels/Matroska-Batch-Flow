using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

/// <summary>
/// Configures batch validation behavior and strictness levels.
/// </summary>
public sealed class BatchValidationSettings
{
    /// <summary>
    /// Current strictness mode.
    /// </summary>
    public StrictnessMode Mode { get; set; } = StrictnessMode.Strict;

    /// <summary>
    /// Custom validation severity settings. Only used when Mode is Custom.
    /// </summary>
    public ValidationSeveritySettings CustomSettings { get; set; } = new();
}

/// <summary>
/// Validation severity settings for custom mode.
/// </summary>
public sealed class ValidationSeveritySettings
{
    /// <summary>
    /// Validation severity for track count parity (structural).
    /// </summary>
    public ValidationSeverity TrackCountParity { get; set; } = ValidationSeverity.Error;

    /// <summary>
    /// Validation settings for audio tracks.
    /// </summary>
    public TrackPropertyValidationSettings AudioTrackValidation { get; set; } = new();

    /// <summary>
    /// Validation settings for video tracks.
    /// </summary>
    public TrackPropertyValidationSettings VideoTrackValidation { get; set; } = new();

    /// <summary>
    /// Validation settings for subtitle tracks.
    /// </summary>
    public TrackPropertyValidationSettings SubtitleTrackValidation { get; set; } = new();
}
