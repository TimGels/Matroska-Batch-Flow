using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides functionality to ensure that the track count in the batch configuration matches the track count in a
/// reference file for a specified track type.
/// </summary>
/// <remarks>This class is responsible for synchronizing the number of tracks in the batch configuration
/// with the number of tracks found in a scanned media file. It does not modify track properties, only the
/// count.</remarks>
/// <param name="_batchConfig">The batch configuration to be modified.</param>
public class BatchConfigurationTrackInitializer(IBatchConfiguration _batchConfig) : IBatchConfigurationTrackInitializer
{
    /// <inheritdoc/>
    /// <remarks>
    /// <b>CAUTION</b> - This is a <b>destructive</b> operation: it will <b>delete</b> or <b>add</b> <see cref="BatchConfiguration"/> 
    /// tracks as necessary to match the count of <paramref name="referenceFile"/> for the specified <paramref name="trackTypes"/>.
    /// </remarks>
    public void EnsureTrackCount(ScannedFileInfo referenceFile, params TrackType[] trackTypes)
    {
        if (referenceFile?.Result?.Media?.Track == null || trackTypes == null || trackTypes.Length == 0)
            return;

        foreach (var trackType in trackTypes)
        {
            var scannedTracks = referenceFile.Result.Media.Track
                .Where(t => t.Type == trackType)
                .ToList();

            var batchTracks = _batchConfig.GetTrackListForType(trackType);

            // Add tracks until the count matches.
            while (batchTracks.Count < scannedTracks.Count)
            {
                var scannedTrackInfo = scannedTracks[batchTracks.Count];
                batchTracks.Add(new TrackConfiguration(scannedTrackInfo)
                {
                    TrackType = trackType,
                });
            }
            // Remove tracks until the count matches.
            while (batchTracks.Count > scannedTracks.Count)
                batchTracks.RemoveAt(batchTracks.Count - 1);
        }
    }
}
