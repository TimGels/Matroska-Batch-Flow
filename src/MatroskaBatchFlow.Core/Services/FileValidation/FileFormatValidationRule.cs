using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Represents a validation rule that checks if files are in a supported file format.
/// </summary>
public class FileFormatValidationRule : IFileValidationRule
{
    private static readonly string[] AllowedExtensions = [".mkv"];
    private static readonly string AllowedFormat = "Matroska";

    /// <summary>
    /// Validates a collection of files to ensure they are in a supported file format.
    /// </summary>
    /// <remarks>Prevents Roy from trying to process files that are not in the Matroska format.</remarks>
    /// <param name="files">The collection of files to validate.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An enumerable collection of <see cref="FileValidationResult"/> indicating the validation results.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        foreach (var file in files)
        {
            // Check extension against allowed array.
            string extension = Path.GetExtension(file.Path);
            bool isAllowedExtension = AllowedExtensions.Any(
                ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)
            );

            // Check if the file is in Matroska format.
            bool isMatroskaFormat = IsMatroskaFormat(file);

            if (!isAllowedExtension || !isMatroskaFormat)
            {
                yield return new FileValidationResult(
                    ValidationSeverity.Error,
                    file.Path,
                    "File is not a supported or valid Matroska file."
                );
            }
        }
    }

    /// <summary>
    /// Checks if the given file is in Matroska format by examining its MediaInfoResult.
    /// </summary>
    /// <param name="file">The file to check.</param>
    /// <returns><see langword="true"/> if the file is in Matroska format; otherwise, <see langword="false"/>.</returns>
    private static bool IsMatroskaFormat(ScannedFileInfo file)
    {
        // Validate that the file is not null and has a valid Result.Media.Track property.
        if (file is not { Result.Media.Track: var tracks } || tracks is null)
            return false;

        var generalTracks = file.Result.Media.Track
            .Where(track => track?.Type == TrackType.General && !string.IsNullOrWhiteSpace(track.Format))
            .ToArray();

        // If there are no general tracks, we cannot determine the format.
        if (generalTracks.Length == 0)
            return false;

        // Check if all general tracks have the format "Matroska".
        return generalTracks.All(generalTrack => generalTrack.Format.Equals(AllowedFormat, StringComparison.OrdinalIgnoreCase));
    }
}
