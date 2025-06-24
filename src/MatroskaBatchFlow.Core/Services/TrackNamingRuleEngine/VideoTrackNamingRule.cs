using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.TrackNamingRuleEngine
{
    public class VideoTrackNamingRule : IFileProcessingRule
    {
        public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
        {
            if (scannedFile?.Result?.Media?.Track == null || batchConfig?.VideoTracks == null)
                return;

            foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Video))
            {
                if (track.StreamKindID < 0)
                    continue;

                var config = batchConfig.VideoTracks.FirstOrDefault(t => t.Position == track.StreamKindID);
                if (config != null)
                {
                    config.Name = string.Empty;
                }
            }
        }
    }
}
