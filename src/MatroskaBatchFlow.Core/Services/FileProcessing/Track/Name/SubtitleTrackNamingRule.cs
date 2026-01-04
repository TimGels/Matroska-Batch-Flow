using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

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

        var fileTracks = batchConfig.GetTrackListForFile(scannedFile.Path, TrackType.Text);
        if (fileTracks == null || fileTracks.Count == 0)
            return;

        foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Text))
        {
            var config = fileTracks.FirstOrDefault(t => t.Index == track.StreamKindID);
            if (config == null)
                continue;

            config.Name = track.Title ?? string.Empty;
        }
    }
}
