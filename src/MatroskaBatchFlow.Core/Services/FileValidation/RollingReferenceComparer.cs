using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Compares track property values across multiple files using a rolling reference strategy.
/// When files have different track counts, the first file (in user-provided order) that has
/// a given track position serves as the reference for comparing against all subsequent files
/// with that track.
/// </summary>
internal static class RollingReferenceComparer
{
    /// <summary>
    /// Compares track property values across files using rolling reference validation.
    /// </summary>
    /// <typeparam name="T">The type of the track property value (e.g., bool for flags, string for languages).</typeparam>
    /// <param name="matrix">A read-only list of lists where each inner list contains property values per track position
    /// for a single file. The outer list index corresponds to the file index in <paramref name="files"/>.</param>
    /// <param name="files">The scanned files being validated, in user-provided order.</param>
    /// <param name="trackType">The type of track being compared (Audio, Video, Text).</param>
    /// <param name="severity">The severity level for any mismatches found.</param>
    /// <param name="propertyName">A human-readable property name for error messages (e.g., "Default flag", "Language").</param>
    /// <returns>Validation results for any detected mismatches.</returns>
    internal static IEnumerable<FileValidationResult> Compare<T>(
        IReadOnlyList<IReadOnlyList<T>> matrix,
        IReadOnlyList<ScannedFileInfo> files,
        TrackType trackType,
        ValidationSeverity severity,
        string propertyName)
    {
        if (matrix.Count < 2)
            yield break;

        // Find the maximum track count across all files to determine how many positions to check.
        int maxTrackCount = matrix.Max(row => row.Count);

        for (int trackIndex = 0; trackIndex < maxTrackCount; trackIndex++)
        {
            // Find the first file that has this track position — it becomes the reference.
            int referenceFileIndex = -1;
            for (int fileIndex = 0; fileIndex < matrix.Count; fileIndex++)
            {
                if (trackIndex < matrix[fileIndex].Count)
                {
                    referenceFileIndex = fileIndex;
                    break;
                }
            }

            // No file has this track position (shouldn't happen, but guard).
            if (referenceFileIndex < 0)
                continue;

            T referenceValue = matrix[referenceFileIndex][trackIndex];

            // Compare all subsequent files that have this track position against the reference.
            for (int fileIndex = referenceFileIndex + 1; fileIndex < matrix.Count; fileIndex++)
            {
                // Skip files that don't have this track position.
                if (trackIndex >= matrix[fileIndex].Count)
                    continue;

                T currentValue = matrix[fileIndex][trackIndex];

                if (EqualityComparer<T>.Default.Equals(referenceValue, currentValue))
                    continue;

                yield return new FileValidationResult(
                    severity,
                    files[fileIndex].Path,
                    $"{propertyName} mismatch in {trackType} tracks at position {trackIndex + 1}: " +
                    $"'{referenceValue}' (in '{files[referenceFileIndex].Path}') " +
                    $"vs '{currentValue}' (in '{files[fileIndex].Path}')."
                );
            }
        }
    }
}
