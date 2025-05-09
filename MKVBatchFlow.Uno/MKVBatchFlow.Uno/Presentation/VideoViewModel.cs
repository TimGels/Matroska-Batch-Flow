using System.Diagnostics;
using MKVBatchFlow.Core;
using MKVBatchFlow.Core.Enums;

namespace MKVBatchFlow.Uno.Presentation;
public partial class VideoViewModel :ObservableObject {
    [ObservableProperty]
    private bool isDefaultTrack = true;

    [ObservableProperty]
    private bool isEnabledTrack = true;

    [ObservableProperty]
    private bool isForcedTrack = true;

    [ObservableProperty]
    private bool changeDefaultTrack = false;

    [ObservableProperty]
    private bool changeEnabledTrack = false;

    [ObservableProperty]
    private bool changeForcedTrack = false;

    [ObservableProperty]
    private string trackName = string.Empty;

    [ObservableProperty]
    private IList<TrackConfiguration>? videoTracks = default;

    public VideoViewModel () {
        VideoTracks = [
            new TrackConfiguration
            {
                TrackType = TrackType.Video,
                Name = "Main Video",
                Position = 1,
                Language = "und",
                Default = true,
                Forced = false,
                Remove = false
            },
            new TrackConfiguration
            {
                TrackType = TrackType.Video,
                Position = 2,
                Name = "Main Video 2",
                Language = "und",
                Default = true,
                Forced = false,
                Remove = false
            },
            new TrackConfiguration
            {
                TrackType = TrackType.Video,
                Position = 3,
                Name = "Main Video 3",
                Language = "eng",
                Default = true,
                Forced = false,
                Remove = false
            },
        ];
    }

    // This is auto-called when IsDefaultTrackEnabled changes
    partial void OnIsEnabledTrackChanged (bool value) {
        Debug.WriteLine($"Track enabled changed to: {value}");
    }
}
