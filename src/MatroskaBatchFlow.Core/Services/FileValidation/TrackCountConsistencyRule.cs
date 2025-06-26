using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Ensures all files have consistent track counts for each track type.
/// </summary>
public class TrackCountConsistencyRule : IFileValidationRule
{
    /// <summary>
    /// Validates a collection of scanned files to ensure consistency in track counts across files based on their types.
    /// </summary>
    /// <remarks>
    /// This method checks for mismatches in the number of audio, video, and text tracks
    /// across the provided files. If fewer than two files are provided, no validation is performed. Validation
    /// results include detailed information about the mismatched track counts.
    /// </remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the scanned files to validate.</param>
    /// <returns>
    /// An enumerable of <see cref="FileValidationResult"/> objects containing validation errors. If no validation
    /// issues are found, the enumerable will be empty.
    /// </returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files)
    {
        var scannedFiles = files.ToList();
        if (scannedFiles.Count < 2)
            yield break;

        var trackTypes = new[] { TrackType.Audio, TrackType.Video, TrackType.Text };
        var referenceFile = scannedFiles[0];

        // Dictionary to hold the reference track counts for each type from the first file.
        var referenceCounts = trackTypes.ToDictionary(
            type => type,
            type => referenceFile.Result?.Media?.Track?.Count(t => t.Type == type) ?? 0
        );

        // Check the track counts of the reference file against all other files.
        foreach (var file in scannedFiles.Skip(1))
        {
            // Collect mismatches for each track type.
            var mismatches = trackTypes
                .Where(type =>
                    (file.Result?.Media?.Track?.Count(t => t.Type == type) ?? 0) != referenceCounts[type])
                .ToList();

            if (mismatches.Count <= 0)
                continue;

            var details = string.Join(
                ", ",
                mismatches.Select(type =>
                    $"{type}: {file.Result?.Media?.Track?.Count(t => t.Type == type) ?? 0} (expected {referenceCounts[type]})"
                )
            );

            yield return new FileValidationResult(
                FileValidationSeverity.Error,
                file.Path,
                $"Track count mismatch for ('{file.Path}'): {details}"
            );
        }
    }
}
