using MatroskaBatchFlow.Core.Enums;

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
        if (scannedFile?.Result?.Media?.Track == null || batchConfig?.SubtitleTracks == null)
            return;

        foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Text))
        {
            if (!_supportedFormatMappings.TryGetValue(track.Format ?? string.Empty, out var name))
                continue;

            // Find the corresponding TrackConfiguration by position
            //if (!int.TryParse(track.StreamKindID, out int position))
            //    continue;

            var config = batchConfig.SubtitleTracks.FirstOrDefault(t => t.Position == track.StreamKindID);
            if (config != null)
            {
                config.Name = name;
            }
        }
    }
}
