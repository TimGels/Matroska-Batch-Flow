using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing;

/// <summary>
/// A file processing rule that attempts to extract the segment title from the general track
/// of a scanned Matroska file and sets the <see cref="IBatchConfiguration.Title"/> property accordingly.
/// </summary>
/// <remarks>
/// This rule inspects the <c>General</c> track of the provided <see cref="ScannedFileInfo"/>.
/// If a general track is found, its <c>Title</c> property is assigned to the batch configuration's <c>Title</c>.
/// Only the first general track is considered, and only one segment is supported.
/// </remarks>
public class FileTitleNamingRule : IFileProcessingRule
{
    /// <summary>
    /// Applies the file title naming rule to the specified scanned file and updates the batch configuration accordingly.
    /// </summary>
    /// <param name="scannedFile">The <see cref="ScannedFileInfo"/> instance containing media information about the file 
    /// to process.</param>
    /// <param name="batchConfig">The <see cref="IBatchConfiguration"/> instance to update with the extracted title, if 
    /// available.</param>
    /// <remarks>If no general track is present the method does nothing.</remarks>
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile is not { Result.Media.Track: var tracks } || batchConfig is null)
            return;

        // Find the general track (segment information) in the scanned file. We only support one general track or segment.
        var scannedTrack = tracks.FirstOrDefault(t => t.Type == TrackType.General);

        if (scannedTrack is not null)
        {
            batchConfig.Title = scannedTrack.Title;
        }
    }
}
