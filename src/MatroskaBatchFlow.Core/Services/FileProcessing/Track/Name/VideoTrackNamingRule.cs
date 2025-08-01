using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

public class VideoTrackNamingRule : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig?.VideoTracks == null)
            return;

        var videoTrackIds = scannedFile.Result.Media.Track
            .Where(t => t.Type == TrackType.Video && t.StreamKindID >= 0)
            .Select(track => track.StreamKindID);

        foreach (var streamKindID in videoTrackIds)
        {
            var config = batchConfig.VideoTracks.FirstOrDefault(t => t.Index == streamKindID);
            if (config != null)
            {
                config.Name = "test name" + streamKindID;
            }
        }
    }
}
