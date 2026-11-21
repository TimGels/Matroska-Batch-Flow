using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// The scanned evidence for one track comparison, not proposed edits or expected values.
/// </summary>
/// <param name="TrackType">The track type being compared across supported inputs.</param>
/// <param name="TrackPosition">One-based position within the track type, or null when comparing track counts.</param>
/// <param name="Files">For a property, every file with that position, including matches.
/// For a count, every supported file, including those with zero tracks of this type.</param>
public sealed record ValidationComparison(
    TrackType TrackType,
    int? TrackPosition,
    IReadOnlyList<ValidationFileValue> Files);

/// <summary>
/// A file's raw scanned value, not an expected or corrected value.
/// </summary>
public sealed record ValidationFileValue(string FilePath, string Value);
