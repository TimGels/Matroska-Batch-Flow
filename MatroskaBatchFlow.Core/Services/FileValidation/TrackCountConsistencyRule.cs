using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.FileValidation
{
    /// <summary>
    /// Represents a validation rule that ensures consistency in track counts across a collection of scanned files based on their types.
    /// </summary>
    public class TrackCountConsistencyRule : IFileValidationRule
    {
        /// <summary>
        /// Validates a collection of scanned files to ensure consistency in track counts across files based on their types.
        /// </summary>
        /// <remarks>This method checks for mismatches in the number of audio, video, and text tracks
        /// across the provided files. If fewer than two files are provided, no validation is performed. Validation
        /// results include detailed information about the mismatched track counts.</remarks>
        /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the scanned files to validate.</param>
        /// <returns>An enumerable of <see cref="FileValidationResult"/> objects containing validation errors. If no validation
        /// issues are found, the enumerable will be empty.</returns>
        public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files)
        {
            var scannedFiles = files.ToList();

            if (scannedFiles.Count < 2)
                yield break;

            var trackTypes = new[] { TrackType.Audio, TrackType.Video, TrackType.Text };

            foreach (var type in trackTypes)
            {
                // Get the track counts for each file for the current type.
                var counts = scannedFiles
                    .Select(f => f.Result?.Media?.Track?.Count(t => t.Type == type) ?? 0)
                    .ToList();

                // If there is no variation in track counts for this type, skip it.
                if (counts.Distinct().Count() <= 1)
                    continue;

                var trackCountDetails = string.Join(", ", scannedFiles.Select((f, i) => $"'{f.Path}': {counts[i]}"));
                yield return new FileValidationResult(
                    FileValidationSeverity.Error,
                    $"Track count mismatch for {type} tracks: {trackCountDetails}"
                );
            }
        }
    }
}
