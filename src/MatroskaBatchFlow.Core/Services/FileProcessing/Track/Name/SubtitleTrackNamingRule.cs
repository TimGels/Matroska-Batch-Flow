using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

/// <summary>
/// Analyzes per-file subtitle track names and populates global TrackIntent properties with smart defaults.
/// This rule can implement advanced naming logic based on subtitle format.
/// </summary>
public class SubtitleTrackNamingRule : IFileProcessingRule
{
    private readonly Dictionary<string, string> _supportedFormatMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SSA", "SSA / ASS" },
        { "ASS", "SSA / ASS" },
        { "WebVTT", "WebVTT" },
        { "SRT", "SubRip" },
        { "UTF-8", "SRT" }
        // Add more mappings as needed
    };

    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        var globalTracks = batchConfig.GetTrackListForType(TrackType.Text);

        for (int i = 0; i < globalTracks.Count; i++)
        {
            var names = batchConfig.FileList
                .Select(f => f.GetTracks(TrackType.Text))
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

            // TODO: Future enhancement - generate smart names based on subtitle format if no title exists
        }
    }
}
