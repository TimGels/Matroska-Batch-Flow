using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

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
    /// If fewer than two files are provided, no validation is performed. For each track type, the method compares
    /// the languages of tracks in the first file against the corresponding tracks in subsequent files. If a mismatch is
    /// detected, a warning is returned indicating the type of track and the files involved.</remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate.</param>
    /// <returns>An enumerable of <see cref="FileValidationResult"/> objects containing validation warnings for any detected
    /// inconsistencies. If no inconsistencies are found, the enumerable will be empty.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files)
    {
        var scannedFiles = files.ToList();

        if (scannedFiles.Count < 2)
            yield break;

        var trackTypes = new[] { TrackType.Audio, TrackType.Video, TrackType.Text };

        foreach (var type in trackTypes)
        {
            // Build a matrix of languages for the specified track type across all scanned files.
            var languageMatrix = BuildLanguageMatrix(scannedFiles, type);

            foreach (var result in CompareLanguages(languageMatrix, scannedFiles, type))
                yield return result;
        }
    }

    /// <summary>
    /// Compares the languages of a tracktype across multiple files and identifies any mismatches.
    /// </summary>
    /// <remarks>This method compares the languages of a specific tracktype of each file against the reference file (the
    /// first file in the list). A mismatch is identified if the language at a specific track position differs
    /// between the reference file and another file.</remarks>
    /// <param name="languageMatrix">A list of lists (matrix), where each inner list represents the languages of a file of a tracktype.</param>
    /// <param name="scannedFiles">A list of <see cref="ScannedFileInfo"/> objects representing the files being validated. The order of files in
    /// this list must correspond to the order of the <paramref name="languageMatrix"/>.</param>
    /// <param name="type">The type of track being compared.</param>
    /// <returns>An enumerable collection of <see cref="FileValidationResult"/> objects. If no mismatches are found, 
    /// the enumerable will be empty.</returns>
    private static IEnumerable<FileValidationResult> CompareLanguages(List<List<string>> languageMatrix, List<ScannedFileInfo> scannedFiles, TrackType type)
    {
        var referenceFileLanguages = languageMatrix[0];
        // Check if the first file's (reference) languages are consistent across the rest of the files.
        for (int i = 1; i < languageMatrix.Count; i++)
        {
            if (referenceFileLanguages.SequenceEqual(languageMatrix[i]))
                continue;

            // TODO: Decide whether this rule should be able to still validate if the track count is different.
            // Skip comparison if the track count is different.
            // Track count consistency validation falls outside the scope of this rule.
            if (referenceFileLanguages.Count != languageMatrix[i].Count)
                continue;

            for (int trackIndex = 0; trackIndex < referenceFileLanguages.Count; trackIndex++)
            {
                // Skip if the track languages match.
                if (referenceFileLanguages[trackIndex] == languageMatrix[i][trackIndex])
                    continue;

                yield return new FileValidationResult(
                    FileValidationSeverity.Warning,
                    scannedFiles[i].Path,
                    $"Language mismatch in {type} tracks at position {trackIndex + 1}: " +
                    $"'{referenceFileLanguages[trackIndex]}' (in '{scannedFiles[0].Path}') " +
                    $"vs '{languageMatrix[i][trackIndex]}' (in '{scannedFiles[i].Path}')."
                );
            }
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
