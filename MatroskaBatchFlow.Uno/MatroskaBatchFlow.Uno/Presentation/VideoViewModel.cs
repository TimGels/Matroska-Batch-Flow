using System.Diagnostics;
using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class VideoViewModel: ObservableObject
{
    [ObservableProperty]
    private bool isDefaultTrack = true;

    [ObservableProperty]
    private bool isEnabledTrack = true;

    [ObservableProperty]
    private bool isForcedTrack = true;

    [ObservableProperty]
    private bool changeDefaultTrack = true;

    [ObservableProperty]
    private bool changeEnabledTrack = true;

    [ObservableProperty]
    private bool changeForcedTrack = true;

    [ObservableProperty]
    private string trackName = string.Empty;

    [ObservableProperty]
    private IList<TrackConfiguration>? videoTracks = default;

    [ObservableProperty]
    private ImmutableList<MatroskaLanguageOption> languages;

    public VideoViewModel(ILanguageProvider languageProvider)
    {
        languages = languageProvider.Languages;
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
    partial void OnIsEnabledTrackChanged(bool value)
    {
        Debug.WriteLine($"Track enabled changed to: {value}");
    }
}
