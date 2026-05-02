using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

/// <summary>
/// Analyzes per-file video track names and populates global TrackIntent properties with smart defaults.
/// </summary>
public class VideoTrackNamingRule : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        var globalTracks = batchConfig.GetTrackListForType(TrackType.Video);

        for (int i = 0; i < globalTracks.Count; i++)
        {
            var names = batchConfig.FileList
                .Select(f => f.GetTracks(TrackType.Video))
                .Where(tracks => i < tracks.Count)
                .Select(tracks => tracks[i].Title ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            if (names.Count == 1)
            {
                globalTracks[i].Name = names[0];
            }
            else if (names.Count > 0)
            {
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
