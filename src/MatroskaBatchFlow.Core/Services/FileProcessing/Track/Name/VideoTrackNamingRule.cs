using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

/// <summary>
/// Analyzes per-file video track names and populates global UI properties with smart defaults.
/// Per-file configurations are already populated by <see cref="BatchTrackCountSynchronizer"/>.
/// </summary>
public class VideoTrackNamingRule : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        // Per-file configs already populated by synchronizer - we just populate global UI
        var globalTracks = batchConfig.GetTrackListForType(TrackType.Video);

        for (int i = 0; i < globalTracks.Count; i++)
        {
            // Collect names from all files that have this track
            var names = batchConfig.FileConfigurations.Values
                .Select(fc => fc.GetTrackListForType(TrackType.Video))
                .Where(tracks => i < tracks.Count)
                .Select(tracks => tracks[i].Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            // Business logic: Use common name if all files agree, otherwise use most common or empty
            if (names.Count == 1)
            {
                globalTracks[i].Name = names[0];
            }
            else if (names.Count > 0)
            {
                // Multiple different names - use most common
                var mostCommonName = names
                    .GroupBy(n => n)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;
                globalTracks[i].Name = mostCommonName;
            }
        }
    }
}
