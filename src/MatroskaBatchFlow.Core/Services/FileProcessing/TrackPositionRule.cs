using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.FileProcessing;

/// <summary>
/// Assigns the Position property of each <see cref="TrackConfiguration"/> in the 
/// <see cref="IBatchConfiguration"/>" to match the StreamKindPos of tracks in the scanned file info.
/// </summary>
public class TrackPositionRule : IFileProcessingRule
{

    /// <summary>
    /// Assigns the <c>Position</c> property of each <see cref="TrackConfiguration"/> in the batch configuration.
    /// </summary>
    /// <remarks>
    /// For each supported <see cref="TrackType"/>, this method will assign the <c>Position</c> property of each 
    /// <see cref="TrackConfiguration"/> in the <paramref name="batchConfig"/>.
    /// </remarks>
    /// <param name="file">The <see cref="ScannedFileInfo"/> containing the scanned media file and its tracks. Must not be null.</param>
    /// <param name="batchConfig">The <see cref="IBatchConfiguration"/> whose track configurations will be updated. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="file"/> or <paramref name="batchConfig"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a scanned track's <c>StreamKindPos</c> is missing or not a valid integer.</exception>
    public void Apply(ScannedFileInfo file, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(batchConfig);

        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsEditable()))
        {
            var scannedTracks = file.Result.Media.Track
                .Where(t => t.Type == trackType)
                .ToList();

            var batchTracks = batchConfig.GetTrackListForType(trackType);

            for (int i = 0; i < batchTracks.Count && i < scannedTracks.Count; i++)
            {
                var streamKindPos = scannedTracks[i].StreamKindPos;
                if (string.IsNullOrWhiteSpace(streamKindPos) || !int.TryParse(streamKindPos, out int parsed))
                    throw new InvalidOperationException($"StreamKindPos is missing or invalid for track {i} of type {trackType} in file '{file.Path}'.");

                batchTracks[i].Position = parsed;
            }
        }
    }
}
