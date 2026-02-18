using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Represents a validation rule that ensures consistency in forced flags across multiple media files.
/// </summary>
public class ForcedFlagConsistencyRule : IFileValidationRule
{
    /// <summary>
    /// Validates a collection of scanned files to ensure consistency in forced flags across files.
    /// </summary>
    /// <remarks>This method checks the forced flag consistency of tracks across the provided files. 
    /// If fewer than two files are provided, no validation is performed. For each track type, it uses rolling
    /// reference comparison to detect forced flag mismatches even when files have different track counts.
    /// When files differ in track count, overlapping track positions are still validated.</remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An enumerable of <see cref="FileValidationResult"/> objects containing validation results for any detected
    /// inconsistencies. If no inconsistencies are found, the enumerable will be empty.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        var scannedFiles = files.ToList();

        if (scannedFiles.Count < 2)
            yield break;

        var trackTypes = new[] { TrackType.Audio, TrackType.Text };

        foreach (var type in trackTypes)
        {
            // Check if validation is enabled for this track type before building the matrix
            var trackSettings = type switch
            {
                TrackType.Audio => settings.CustomSettings.AudioTrackValidation,
                TrackType.Text => settings.CustomSettings.SubtitleTrackValidation,
                _ => null
            };

            // Skip if validation is disabled or settings are null for this track type
            if (trackSettings == null || trackSettings.ForcedFlag == ValidationSeverity.Off)
                continue;

            // Build a matrix of forced flags for the specified track type across all scanned files.
            var forcedFlagMatrix = BuildForcedFlagMatrix(scannedFiles, type);

            foreach (var result in RollingReferenceComparer.Compare(
                forcedFlagMatrix, scannedFiles, type, trackSettings.ForcedFlag, "Forced flag"))
                yield return result;
        }
    }

    /// <summary>
    /// Builds a matrix of forced flags for the specified track type across all scanned files.
    /// </summary>
    /// <param name="files">A list of <see cref="ScannedFileInfo"/> objects to process.</param>
    /// <param name="type">The type of track to extract forced flags for.</param>
    /// <returns>
    /// A list of lists (matrix), where each inner list contains the forced flags associated with the specified track type for a
    /// single file. The outer list represents all files in the input.
    /// <br /> <br />
    /// Example: forcedFlagMatrix for TrackType.Audio with 3 files, each having 2 audio tracks.
    /// <code>
    /// [
    ///   [true, false],  // File 1: first audio track is forced, second is not
    ///   [true, false],  // File 2: same as File 1
    ///   [false, true]   // File 3: first audio track is not forced, second is forced
    /// ]
    /// </code>
    /// </returns>
    private static List<List<bool>> BuildForcedFlagMatrix(List<ScannedFileInfo> files, TrackType type) =>
        [.. files.Select(f => GetTrackForcedFlags(f, type))];

    /// <summary>
    /// Retrieves a list of forced flags for tracks of the specified type within the given scanned file.
    /// </summary>
    /// <param name="file">The scanned file information containing media track details. Cannot be null.</param>
    /// <param name="type">The type of tracks to filter by.</param>
    /// <returns>A list of forced flag values associated with the tracks of the specified type, ordered by stream kind ID.</returns>
    private static List<bool> GetTrackForcedFlags(ScannedFileInfo file, TrackType type) =>
        file.Result?.Media?.Track
            .Where(t => t.Type == type)
            .OrderBy(t => t.StreamKindID)
            .Select(t => t.Forced)
            .ToList() ?? [];
}
