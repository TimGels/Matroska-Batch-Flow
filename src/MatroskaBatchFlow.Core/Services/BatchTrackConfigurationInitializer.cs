using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Initializes global track intent collections based on scanned file information.
/// </summary>
/// <remarks>
/// This class ensures that the global <see cref="IBatchConfiguration"/> track collections
/// (Audio, Video, Subtitle) are expanded to reflect the maximum track count across all files.
/// Per-file values are no longer stored — they are computed on demand via the transform pipeline.
/// </remarks>
/// <param name="batchConfig">The batch configuration to be modified.</param>
/// <param name="trackIntentFactory">The factory for creating track intents.</param>
public class BatchTrackConfigurationInitializer(
    IBatchConfiguration batchConfig,
    ITrackIntentFactory trackIntentFactory) : IBatchTrackConfigurationInitializer
{
    /// <inheritdoc/>
    public void Initialize(ScannedFileInfo scannedFile, params TrackType[] trackTypes)
    {
        if (scannedFile?.Result?.Media?.Track == null || trackTypes.Length == 0)
            return;

        // Update global collections for UI display based on maximum track counts
        UpdateGlobalTracksForMaximumCounts(scannedFile, trackTypes);
    }

    /// <summary>
    /// Updates global track collections to reflect the maximum track count across all files.
    /// This ensures the UI displays all tracks that exist in any file, allowing users to
    /// configure tracks that may not exist in all files.
    /// </summary>
    /// <remarks>
    /// This will not remove any existing global tracks; it only adds new ones as needed.
    /// </remarks>
    /// <param name="scannedFile">The scanned file to check track counts against.</param>
    /// <param name="trackTypes">The track types to update.</param>
    private void UpdateGlobalTracksForMaximumCounts(ScannedFileInfo scannedFile, TrackType[] trackTypes)
    {
        foreach (var trackType in trackTypes)
        {
            var scannedTracks = scannedFile.GetTracks(trackType);
            int fileTrackCount = scannedTracks.Count;
            var batchTracks = batchConfig.GetTrackListForType(trackType);
            int batchTrackCount = batchTracks.Count;

            // Expand global collection if this file has more tracks than currently represented
            for (int i = batchTrackCount; i < fileTrackCount; i++)
            {
                batchTracks.Add(trackIntentFactory.Create(scannedTracks[i], trackType, i));
            }
        }
    }
}
