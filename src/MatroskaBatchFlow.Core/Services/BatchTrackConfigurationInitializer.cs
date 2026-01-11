using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Initializes per-file track configurations based on scanned file information.
/// </summary>
/// <remarks>
/// This class orchestrates the creation of individual track configurations for each file:
/// <list type="bullet">
/// <item>Delegates track configuration creation to <see cref="ITrackConfigurationFactory"/></item>
/// <item>Populates file-specific track lists in <see cref="IBatchConfiguration.FileConfigurations"/></item>
/// <item>Updates global track collections to reflect maximum track counts for UI display</item>
/// </list>
/// </remarks>
/// <param name="batchConfig">The batch configuration to be modified.</param>
/// <param name="trackConfigFactory">The factory for creating track configurations.</param>
public class BatchTrackConfigurationInitializer(
    IBatchConfiguration batchConfig,
    ITrackConfigurationFactory trackConfigFactory) : IBatchTrackConfigurationInitializer
{
    /// <inheritdoc/>
    public void Initialize(ScannedFileInfo scannedFile, params TrackType[] trackTypes)
    {
        if (scannedFile?.Result?.Media?.Track == null || trackTypes.Length == 0)
            return;

        // Ensure per-file configuration exists
        if (!batchConfig.FileConfigurations.TryGetValue(scannedFile.Id, out FileTrackConfiguration? fileConfig))
        {
            fileConfig = new FileTrackConfiguration
            {
                FilePath = scannedFile.Path
            };
            batchConfig.FileConfigurations[scannedFile.Id] = fileConfig;
        }

        // Populate file-specific track configurations based on what this file has
        foreach (var trackType in trackTypes)
        {
            var scannedTracks = scannedFile.Result.Media.Track
                .Where(t => t.Type == trackType)
                .OrderBy(t => t.StreamKindID)
                .ToList();

            var fileTracks = fileConfig.GetTrackListForType(trackType);

            while (fileTracks.Count < scannedTracks.Count)
            {
                var scannedTrackInfo = scannedTracks[fileTracks.Count];
                fileTracks.Add(trackConfigFactory.Create(scannedTrackInfo, trackType, fileTracks.Count));
            }
        }

        // Update global collections for UI display based on maximum track counts
        UpdateGlobalTracksForMaximumCounts(trackTypes);
    }

    /// <summary>
    /// Updates global track collections to reflect the maximum track count across all files.
    /// This ensures the UI displays all tracks that exist in any file, allowing users to
    /// configure tracks that may not exist in all files.
    /// </summary>
    /// <param name="trackTypes">The track types to update.</param>
    private void UpdateGlobalTracksForMaximumCounts(TrackType[] trackTypes)
    {
        foreach (var trackType in trackTypes)
        {
            // Find the maximum track count for this track type across all files
            int maxTrackCount = 0;
            Guid fileIdWithMaxTracks = Guid.Empty;

            foreach (var file in batchConfig.FileList)
            {
                int trackCount = trackType switch
                {
                    TrackType.Audio => file.AudioTrackCount,
                    TrackType.Video => file.VideoTrackCount,
                    TrackType.Text => file.SubtitleTrackCount,
                    _ => 0
                };

                if (trackCount > maxTrackCount)
                {
                    maxTrackCount = trackCount;
                    fileIdWithMaxTracks = file.Id;
                }
            }

            // If we found a file with tracks, use it to populate the global collection
            if (fileIdWithMaxTracks != Guid.Empty && maxTrackCount > 0)
            {
                var batchTracks = batchConfig.GetTrackListForType(trackType);

                // Get the file's track configuration to use as a template
                if (batchConfig.FileConfigurations.TryGetValue(fileIdWithMaxTracks, out var fileConfig))
                {
                    var fileTracks = fileConfig.GetTrackListForType(trackType);

                    // Ensure global collection matches the maximum count
                    while (batchTracks.Count < fileTracks.Count)
                    {
                        var sourceTrack = fileTracks[batchTracks.Count];
                        batchTracks.Add(trackConfigFactory.Create(sourceTrack.ScannedTrackInfo, trackType, batchTracks.Count));
                    }
                }
            }
        }
    }
}
