using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class AudioViewModel : TrackViewModelBase
{
    private ObservableCollection<TrackConfiguration> _audioTracksInternal = [];
    public ObservableCollection<TrackConfiguration> AudioTracks
    {
        get => _audioTracksInternal;
        set
        {
            if (_audioTracksInternal != value)
            {
                _audioTracksInternal = value;
                OnPropertyChanged(nameof(AudioTracks));
                OnPropertyChanged(nameof(IsTrackSelected));
            }
        }
    }

    public AudioViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
        : base(languageProvider, batchConfiguration)
    {
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

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => AudioTracks;

    /// <inheritdoc />
    protected override void SetupEventHandlers()
    {
        throw new NotImplementedException("SetupEventHandlers is not implemented for AudioViewModel.");
    }
}
