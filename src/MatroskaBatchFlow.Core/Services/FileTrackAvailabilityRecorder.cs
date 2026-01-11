using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Records track availability information for scanned files.
/// </summary>
/// <param name="_batchConfig">The batch configuration where availability is recorded.</param>
public class FileTrackAvailabilityRecorder(IBatchConfiguration _batchConfig) : IFileTrackAvailabilityRecorder
{
    /// <inheritdoc/>
    public FileTrackAvailability RecordAvailability(ScannedFileInfo scannedFile)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);

        var tracks = scannedFile.Result?.Media?.Track ?? [];

        var availability = new FileTrackAvailability
        {
            FilePath = scannedFile.Path,
            AudioTrackCount = tracks.Count(t => t.Type == TrackType.Audio),
            VideoTrackCount = tracks.Count(t => t.Type == TrackType.Video),
            SubtitleTrackCount = tracks.Count(t => t.Type == TrackType.Text)
        };

        _batchConfig.FileTrackMap[scannedFile.Id] = availability;

        return availability;
    }
}
