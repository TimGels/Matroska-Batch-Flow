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
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        var fileTracks = batchConfig.GetTrackListForFile(scannedFile.Path, TrackType.Audio);
        if (fileTracks == null || fileTracks.Count == 0)
            return;

        foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Audio))
        {
            var config = fileTracks.FirstOrDefault(t => t.Index == track.StreamKindID);
            if (config == null)
                continue;

            config.Name = track.Title ?? string.Empty;
            //var format = track.Format ?? string.Empty;
            //var layout = track.ChannelLayout ?? string.Empty;

            //// Map channel layout to friendly name
            //var layoutName = _channelLayoutMappings.TryGetValue(layout, out var friendly)
            //    ? friendly
            //    : layout;

            //var name = string.IsNullOrWhiteSpace(layoutName)
            //    ? format
            //    : $"{layoutName} {format}";

            //if (!int.TryParse(track.StreamKindPos, out int position))
            //    continue;
        }
    }
}
