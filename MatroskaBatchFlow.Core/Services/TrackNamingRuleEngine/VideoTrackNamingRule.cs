using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.TrackNamingRuleEngine
{
    internal class VideoTrackNamingRule : IFileProcessingRule
    {
        public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
        {
            if (scannedFile?.Result?.Media?.Track == null || batchConfig?.VideoTracks == null)
                return;

            foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Video))
            {
                var format = track.Format ?? string.Empty;
                var width = track.Extra?.Chapters.TryGetValue("Width", out var w) == true ? w : string.Empty;
                var height = track.Extra?.Chapters.TryGetValue("Height", out var h) == true ? h : string.Empty;
                var resolution = (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height)) ? $"{width}x{height}" : string.Empty;

                var name = string.IsNullOrWhiteSpace(resolution)
                    ? format
                    : $"{format} {resolution}";

                if (!int.TryParse(track.StreamKindPos, out int position))
                    continue;

                var config = new TrackConfiguration
                {
                    TrackType = TrackType.Video,
                    Position = position,
                    Name = name,
                    Language = track.Language ?? string.Empty,
                    Default = string.Equals(track.Default, "Yes", StringComparison.OrdinalIgnoreCase),
                    Forced = string.Equals(track.Forced, "Yes", StringComparison.OrdinalIgnoreCase),
                    Remove = false
                };

                batchConfig.VideoTracks.Add(config);
            }
        }
    }
}
