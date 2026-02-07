using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Represents a validation rule that ensures consistency in default flags across multiple media files.
/// </summary>
public class DefaultFlagConsistencyRule : IFileValidationRule
{
    /// <summary>
    /// Validates a collection of scanned files to ensure consistency in default flags across files.
    /// </summary>
    /// <remarks>This method checks the default flag consistency of tracks across the provided files. 
    /// If fewer than two files are provided, no validation is performed. For each track type, the method compares
    /// the default flags of tracks in the first file against the corresponding tracks in subsequent files. If a mismatch is
    /// detected, a validation result is returned based on the configured severity level.</remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An enumerable of <see cref="FileValidationResult"/> objects containing validation results for any detected
    /// inconsistencies. If no inconsistencies are found, the enumerable will be empty.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        var scannedFiles = files.ToList();

        if (scannedFiles.Count < 2)
            yield break;

        var trackTypes = new[] { TrackType.Audio, TrackType.Video, TrackType.Text };

        foreach (var type in trackTypes)
        {
            // Build a matrix of default flags for the specified track type across all scanned files.
            var defaultFlagMatrix = BuildDefaultFlagMatrix(scannedFiles, type);

            foreach (var result in CompareDefaultFlags(defaultFlagMatrix, scannedFiles, type, settings))
                yield return result;
        }
    }

    /// <summary>
    /// Compares the default flags of a tracktype across multiple files and identifies any mismatches.
    /// </summary>
    /// <remarks>This method compares the default flags of a specific tracktype of each file against the reference file (the
    /// first file in the list). A mismatch is identified if the default flag at a specific track position differs
    /// between the reference file and another file.</remarks>
    /// <param name="defaultFlagMatrix">A list of lists (matrix), where each inner list represents the default flags of a file of a tracktype.</param>
    /// <param name="scannedFiles">A list of <see cref="ScannedFileInfo"/> objects representing the files being validated. The order of files in
    /// this list must correspond to the order of the <paramref name="defaultFlagMatrix"/>.</param>
    /// <param name="type">The type of track being compared.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An enumerable collection of <see cref="FileValidationResult"/> objects. If no mismatches are found, 
    /// the enumerable will be empty.</returns>
    private static IEnumerable<FileValidationResult> CompareDefaultFlags(
        List<List<bool>> defaultFlagMatrix,
        List<ScannedFileInfo> scannedFiles,
        TrackType type,
        BatchValidationSettings settings)
    {
        // Get the appropriate validation settings for this track type
        var trackSettings = type switch
        {
            TrackType.Audio => settings.CustomSettings.AudioTrackValidation,
            TrackType.Video => settings.CustomSettings.VideoTrackValidation,
            TrackType.Text => settings.CustomSettings.SubtitleTrackValidation,
            _ => null
        };

        // Skip validation if settings are null or default flag validation is disabled
        if (trackSettings == null || trackSettings.DefaultFlag == ValidationSeverity.Off)
            yield break;

        var referenceFileDefaultFlags = defaultFlagMatrix[0];
        // Check if the first file's (reference) default flags are consistent across the rest of the files.
        for (int i = 1; i < defaultFlagMatrix.Count; i++)
        {
            if (referenceFileDefaultFlags.SequenceEqual(defaultFlagMatrix[i]))
                continue;

            // Skip comparison if the track count is different.
            // Track count consistency validation falls outside the scope of this rule.
            if (referenceFileDefaultFlags.Count != defaultFlagMatrix[i].Count)
                continue;

            for (int trackIndex = 0; trackIndex < referenceFileDefaultFlags.Count; trackIndex++)
            {
                // Skip if the track default flags match.
                if (referenceFileDefaultFlags[trackIndex] == defaultFlagMatrix[i][trackIndex])
                    continue;

                yield return new FileValidationResult(
                    trackSettings.DefaultFlag,
                    scannedFiles[i].Path,
                    $"Default flag mismatch in {type} tracks at position {trackIndex + 1}: " +
                    $"'{referenceFileDefaultFlags[trackIndex]}' (in '{scannedFiles[0].Path}') " +
                    $"vs '{defaultFlagMatrix[i][trackIndex]}' (in '{scannedFiles[i].Path}')."
                );
            }
        }
    }

    /// <summary>
    /// Builds a matrix of default flags for the specified track type across all scanned files.
    /// </summary>
    /// <param name="files">A list of <see cref="ScannedFileInfo"/> objects to process.</param>
    /// <param name="type">The type of track to extract default flags for.</param>
    /// <returns>
    /// A list of lists (matrix), where each inner list contains the default flags associated with the specified track type for a
    /// single file. The outer list represents all files in the input.
    /// <br /> <br />
    /// Example: defaultFlagMatrix for TrackType.Audio with 3 files, each having 2 audio tracks.
    /// <code>
    /// [
    ///   [true, false],  // File 1: first audio track is default, second is not
    ///   [true, false],  // File 2: same as File 1
    ///   [false, true]   // File 3: first audio track is not default, second is default
    /// ]
    /// </code>
    /// </returns>
    private static List<List<bool>> BuildDefaultFlagMatrix(List<ScannedFileInfo> files, TrackType type) =>
        [.. files.Select(f => GetTrackDefaultFlags(f, type))];

    /// <summary>
    /// Retrieves a list of default flags for tracks of the specified type within the given scanned file.
    /// </summary>
    /// <param name="file">The scanned file information containing media track details. Cannot be null.</param>
    /// <param name="type">The type of tracks to filter by.</param>
    /// <returns>A list of default flag values associated with the tracks of the specified type, ordered by stream kind ID.</returns>
    private static List<bool> GetTrackDefaultFlags(ScannedFileInfo file, TrackType type) =>
        file.Result?.Media?.Track
            .Where(t => t.Type == type)
            .OrderBy(t => t.StreamKindID)
            .Select(t => t.Default)
            .ToList() ?? [];
}
