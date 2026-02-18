using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Represents a validation rule that ensures consistency in track languages across multiple media files.
/// </summary>
public class LanguageConsistencyRule : IFileValidationRule
{
    /// <summary>
    /// Validates a collection of scanned files to ensure consistency in track languages across files.
    /// </summary>
    /// <remarks>This method checks the language consistency of tracks across the provided files. 
    /// If fewer than two files are provided, no validation is performed. For each track type, it uses rolling
    /// reference comparison to detect language mismatches even when files have different track counts.
    /// When files differ in track count, overlapping track positions are still validated.</remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An enumerable of <see cref="FileValidationResult"/> objects containing validation warnings for any detected
    /// inconsistencies. If no inconsistencies are found, the enumerable will be empty.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        var scannedFiles = files.ToList();

        if (scannedFiles.Count < 2)
            yield break;

        var trackTypes = new[] { TrackType.Audio, TrackType.Video, TrackType.Text };

        foreach (var type in trackTypes)
        {
            // Check if validation is enabled for this track type before building the matrix
            var trackSettings = type switch
            {
                TrackType.Audio => settings.CustomSettings.AudioTrackValidation,
                TrackType.Video => settings.CustomSettings.VideoTrackValidation,
                TrackType.Text => settings.CustomSettings.SubtitleTrackValidation,
                _ => null
            };

            // Skip if validation is disabled or settings are null for this track type
            if (trackSettings == null || trackSettings.Language == ValidationSeverity.Off)
                continue;

            // Build a matrix of languages for the specified track type across all scanned files.
            var languageMatrix = BuildLanguageMatrix(scannedFiles, type);

            foreach (var result in RollingReferenceComparer.Compare(
                languageMatrix, scannedFiles, type, trackSettings.Language, "Language"))
                yield return result;
        }
    }

    /// <summary>
    /// Builds a matrix of languages for the specified track type across all scanned files.
    /// </summary>
    /// <param name="files">A list of <see cref="ScannedFileInfo"/> objects to process.</param>
    /// <param name="type">The type of track to extract languages for.</param>
    /// <returns>
    /// A list of lists (matrix), where each inner list contains the languages associated with the specified track type for a
    /// single file. The outer list represents all files in the input.
    /// <br /> <br />
    /// Example: languageMatrix for TrackType.Audio with 3 files, each having 2 audio tracks.
    /// <code>
    /// [
    ///   ["eng", "jpn"], // File 1: first audio track is English, second is Japanese
    ///   ["eng", "jpn"], // File 2: same as File 1
    ///   ["eng", "fre"]  // File 3: first audio track is English, second is French
    /// ]
    /// </code>
    /// </returns>
    private static List<List<string>> BuildLanguageMatrix(List<ScannedFileInfo> files, TrackType type) =>
        [.. files.Select(f => GetTrackLanguages(f, type))];

    /// <summary>
    /// Retrieves a list of languages for tracks of the specified type within the given scanned file.
    /// </summary>
    /// <param name="file">The scanned file information containing media track details. Cannot be null.</param>
    /// <param name="type">The type of tracks to filter by.</param>
    /// <returns>A list of language codes associated with the tracks of the specified type, ordered by stream kind ID.</returns>
    private static List<string> GetTrackLanguages(ScannedFileInfo file, TrackType type) =>
        file.Result?.Media?.Track
            .Where(t => t.Type == type)
            .OrderBy(t => t.StreamKindID)
            .Select(t => t.Language ?? string.Empty)
            .ToList() ?? [];
}
