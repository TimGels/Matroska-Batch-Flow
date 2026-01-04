using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

public class VideoTrackNamingRule : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        var fileTracks = batchConfig.GetTrackListForFile(scannedFile.Path, TrackType.Video);
        if (fileTracks == null || fileTracks.Count == 0)
            return;

        foreach (var track in scannedFile.Result.Media.Track.Where(t => t.Type == TrackType.Video))
        {
            var config = fileTracks.FirstOrDefault(t => t.Index == track.StreamKindID);
            if (config == null)
                continue;

            config.Name = track.Title ?? string.Empty;
            //var config = batchConfig.VideoTracks.FirstOrDefault(t => t.Index == streamKindID);
            //if (config != null)
            //{
            //    config.Name = "test name" + streamKindID;
            //}
        }
    }
}
