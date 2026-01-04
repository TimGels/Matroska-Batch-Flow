using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides functionality to initialize per-file track configurations based on scanned file information.
/// </summary>
/// <remarks>
/// This class creates individual track configurations for each file, recording track availability
/// and populating file-specific track lists. Global track collections are populated from the files
/// with the maximum track counts for UI display purposes, allowing editing of all tracks that exist
/// in any file.
/// </remarks>
/// <param name="_batchConfig">The batch configuration to be modified.</param>
public class BatchTrackCountSynchronizer(IBatchConfiguration _batchConfig) : IBatchTrackCountSynchronizer
{
    /// <inheritdoc/>
    /// <remarks>
    /// This method creates per-file configurations for the specified file:
    /// <list type="bullet">
    /// <item>Records track availability in <see cref="IBatchConfiguration.FileTrackMap"/></item>
    /// <item>Creates file-specific track configurations in <see cref="IBatchConfiguration.FileConfigurations"/></item>
    /// <item>Updates global track collections to reflect maximum track counts across all files</item>
    /// </list>
    /// </remarks>
    public void SynchronizeTrackCount(ScannedFileInfo referenceFile, params TrackType[] trackTypes)
    {
        if (referenceFile?.Result?.Media?.Track == null || trackTypes == null || trackTypes.Length == 0)
            return;

        // Record track availability for this file
        var availability = new FileTrackAvailability
        {
            FilePath = referenceFile.Path,
            AudioTrackCount = referenceFile.Result.Media.Track.Count(t => t.Type == TrackType.Audio),
            VideoTrackCount = referenceFile.Result.Media.Track.Count(t => t.Type == TrackType.Video),
            SubtitleTrackCount = referenceFile.Result.Media.Track.Count(t => t.Type == TrackType.Text)
        };

        _batchConfig.FileTrackMap[referenceFile.Path] = availability;

        // Always create per-file configuration
        if (!_batchConfig.FileConfigurations.TryGetValue(referenceFile.Path, out FileTrackConfiguration? fileConfig))
        {
            fileConfig = new FileTrackConfiguration
            {
                FilePath = referenceFile.Path
            };
            _batchConfig.FileConfigurations[referenceFile.Path] = fileConfig;
        }

        // Populate file-specific track configurations based on what this file has
        foreach (var trackType in trackTypes)
        {
            var scannedTracks = referenceFile.Result.Media.Track
                .Where(t => t.Type == trackType)
                .OrderBy(t => t.StreamKindID)
                .ToList();

            var fileTracks = fileConfig.GetTrackListForType(trackType);

            while (fileTracks.Count < scannedTracks.Count)
            {
                var scannedTrackInfo = scannedTracks[fileTracks.Count];
                fileTracks.Add(new TrackConfiguration(scannedTrackInfo)
                {
                    Type = trackType,
                    Index = fileTracks.Count
                });
            }
        }

        // Update global collections for UI display based on maximum track counts
        // These are reference-only and not used for processing
        UpdateGlobalTracksForMaximumCounts(trackTypes);
    }

    /// <summary>
    /// Updates global track collections to reflect the maximum track count across all files.
    /// This ensures the UI displays all tracks that exist in any file, allowing users to
    /// configure tracks that may not exist in all files (when TrackCountParity is OFF).
    /// </summary>
    /// <param name="trackTypes">The track types to update.</param>
    private void UpdateGlobalTracksForMaximumCounts(TrackType[] trackTypes)
    {
        foreach (var trackType in trackTypes)
        {
            // Find the maximum track count for this track type across all files
            int maxTrackCount = 0;
            string? filePathWithMaxTracks = null;

            foreach (var kvp in _batchConfig.FileTrackMap)
            {
                int trackCount = trackType switch
                {
                    TrackType.Audio => kvp.Value.AudioTrackCount,
                    TrackType.Video => kvp.Value.VideoTrackCount,
                    TrackType.Text => kvp.Value.SubtitleTrackCount,
                    _ => 0
                };

                if (trackCount > maxTrackCount)
                {
                    maxTrackCount = trackCount;
                    filePathWithMaxTracks = kvp.Key;
                }
            }

            // If we found a file with tracks, use it to populate the global collection
            if (!string.IsNullOrEmpty(filePathWithMaxTracks) && maxTrackCount > 0)
            {
                var batchTracks = _batchConfig.GetTrackListForType(trackType);

                // Get the file's track configuration to use as a template
                if (_batchConfig.FileConfigurations.TryGetValue(filePathWithMaxTracks, out var fileConfig))
                {
                    var fileTracks = fileConfig.GetTrackListForType(trackType);

                    // Ensure global collection matches the maximum count
                    while (batchTracks.Count < fileTracks.Count)
                    {
                        // Clone the track configuration for the global collection
                        var sourceTrack = fileTracks[batchTracks.Count];
                        batchTracks.Add(new TrackConfiguration(sourceTrack.ScannedTrackInfo)
                        {
                            Type = trackType,
                            Index = batchTracks.Count
                        });
                    }
                }
            }
        }
    }
}
