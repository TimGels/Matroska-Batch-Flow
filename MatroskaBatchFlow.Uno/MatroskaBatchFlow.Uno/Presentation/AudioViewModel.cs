using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class AudioViewModel: ObservableObject
{
   
    [ObservableProperty]
    private IList<TrackConfiguration>? audioTracks = default;
    [ObservableProperty]
    private ImmutableList<MatroskaLanguageOption> languages;
    public AudioViewModel(ILanguageProvider languageProvider)
    {
        languages = languageProvider.Languages;
        AudioTracks = [
            new TrackConfiguration
            {
                TrackType = TrackType.Audio,
                Name = "Main Audio",
                Position = 1,
                Language = "und",
                Default = true,
                Forced = false,
                Remove = false
            },
            new TrackConfiguration
            {
                TrackType = TrackType.Audio,
                Position = 2,
                Name = "Main Audio 2",
                Language = "und",
                Default = true,
                Forced = false,
                Remove = false
            }
        ];
    }
}
