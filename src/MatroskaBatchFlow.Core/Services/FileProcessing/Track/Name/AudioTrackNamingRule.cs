using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

/// <summary>
/// Analyzes per-file audio track names and populates global UI properties with smart defaults.
/// Per-file configurations are already populated by <see cref="BatchTrackCountSynchronizer"/>.
/// This rule can implement advanced naming logic based on codec, channel layout, etc.
/// </summary>
public class AudioTrackNamingRule : IFileProcessingRule
{
    private static readonly Dictionary<string, string> _channelLayoutMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "L R", "Stereo" },
        { "L R C LFE Ls Rs", "5.1" },
        // Add more as needed
    };

    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        // Per-file configs already populated by synchronizer - we just populate global UI
        var globalTracks = batchConfig.GetTrackListForType(TrackType.Audio);

        for (int i = 0; i < globalTracks.Count; i++)
        {
            // Collect names from all files that have this track
            var names = batchConfig.FileConfigurations.Values
                .Select(fc => fc.GetTrackListForType(TrackType.Audio))
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

            // TODO: Future enhancement - generate smart names based on codec/channel layout if no title exists
            //var format = track.Format ?? string.Empty;
            //var layout = track.ChannelLayout ?? string.Empty;
            //    : $"{layoutName} {format}";

            //if (!int.TryParse(track.StreamKindPos, out int position))
            //    continue;
        }
    }
}
