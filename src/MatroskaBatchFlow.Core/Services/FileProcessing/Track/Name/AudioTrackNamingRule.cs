using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

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
        if (scannedFile?.Result?.Media?.Track == null || batchConfig?.AudioTracks == null)
            return;

        foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Audio))
        {
            var format = track.Format ?? string.Empty;
            var layout = track.ChannelLayout ?? string.Empty;

            // Map channel layout to friendly name
            var layoutName = _channelLayoutMappings.TryGetValue(layout, out var friendly)
                ? friendly
                : layout;

            var name = string.IsNullOrWhiteSpace(layoutName)
                ? format
                : $"{layoutName} {format}";

            if (!int.TryParse(track.StreamKindPos, out int position))
                continue;
        }
    }
}
