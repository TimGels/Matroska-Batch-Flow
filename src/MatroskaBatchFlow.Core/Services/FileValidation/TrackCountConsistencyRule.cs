using System.Globalization;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

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
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>
    /// An enumerable of <see cref="FileValidationResult"/> objects containing validation errors. If no validation
    /// issues are found, the enumerable will be empty.
    /// </returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        // Check if track count parity validation is disabled
        if (settings.CustomSettings.TrackCountParity == ValidationSeverity.Off)
            yield break;

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
        var comparisons = new Dictionary<TrackType, ValidationComparison>();

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

            foreach (var type in mismatches)
            {
                // Include matching and zero-count files so the review can show the full distribution for this track type.
                // The snapshot covers the whole batch, so build it once and share it with every later mismatch.
                if (comparisons.ContainsKey(type))
                    continue;

                var fileValues = new List<ValidationFileValue>();
                foreach (var scannedFile in scannedFiles)
                {
                    int trackCount = scannedFile.Result?.Media?.Track?.Count(track => track.Type == type) ?? 0;
                    string value = trackCount.ToString(CultureInfo.InvariantCulture);
                    fileValues.Add(new ValidationFileValue(scannedFile.Path, value));
                }

                comparisons[type] = new ValidationComparison(type, null, fileValues);
            }

            yield return new FileValidationResult(
                settings.CustomSettings.TrackCountParity,
                file.Path,
                $"Track count mismatch for ('{file.Path}'): {details}"
            )
            {
                CheckKind = ValidationCheckKind.TrackCount,
                Comparisons = [.. mismatches.Select(type => comparisons[type])]
            };
        }
    }
}
