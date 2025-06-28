using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class AudioViewModel : TrackViewModelBase
{
    public ObservableCollection<TrackConfiguration> AudioTracks
    {
        get => _tracks;
        set
        {
            if (_tracks != value)
            {
                _tracks = value;
                OnPropertyChanged(nameof(AudioTracks));
                OnPropertyChanged(nameof(IsTrackSelected));
            }
        }
    }

    public ICommand ClearAudioTracks { get; }
    public ICommand MutateNameTrack { get; }

    public AudioViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
        : base(languageProvider, batchConfiguration)
    {
        ClearAudioTracks = new RelayCommand(ClearAudioTracksAction);
        MutateNameTrack = new RelayCommand(() => _batchConfiguration.AudioTracks[0].Name = DateTime.Now.ToString());
        AudioTracks = [.. _batchConfiguration.AudioTracks];
    }

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => AudioTracks;


    /// <inheritdoc />
    /// <remarks>This method subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the 
    /// batch configuration and the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of the audio 
    /// tracks collection.</remarks>
    protected override void SetupEventHandlers() 
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;
        _batchConfiguration.AudioTracks.CollectionChanged += OnBatchConfigurationAudioTracksChanged;
    }

    /// <summary>
    /// Handles changes to the collection of audio tracks in the batch configuration.
    /// </summary>
    /// <remarks>This method updates the internal state to reflect changes in the audio track collection,
    /// subscribing to or unsubscribing from property change notifications for the affected tracks. If the collection is
    /// reset, all existing subscriptions are cleared and re-established.</remarks>
    /// <param name="sender">The source of the event, typically the collection that was changed.</param>
    /// <param name="eventArgs">The event data containing details about the collection change, such as the action 
    /// performed and the items affected.</param>
    private void OnBatchConfigurationAudioTracksChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        void Subscribe(IEnumerable<TrackConfiguration>? items) =>
            items?.ToList().ForEach(t => t.PropertyChanged += OnTrackPropertyChanged);

        void Unsubscribe(IEnumerable<TrackConfiguration>? items) =>
            items?.ToList().ForEach(t => t.PropertyChanged -= OnTrackPropertyChanged);

        if (eventArgs.NewItems != null)
            Subscribe(eventArgs.NewItems.Cast<TrackConfiguration>());
        if (eventArgs.OldItems != null)
            Unsubscribe(eventArgs.OldItems.Cast<TrackConfiguration>());

        if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
        {
            var all = _batchConfiguration.AudioTracks;
            Unsubscribe(all);
            Subscribe(all);
        }

        AudioTracks = [.. _batchConfiguration.AudioTracks];
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method specifically listens for changes to the <c>AudioTracks</c> property of the batch
    /// configuration. If the <c>AudioTracks</c> property changes, the <see cref="AudioTracks"/> property is updated
    /// accordingly.
    /// </remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed. The <see
    /// cref="PropertyChangedEventArgs.PropertyName"/> must not be <see langword="null"/>.</param>
    protected override void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is null)
            return;
        // Replace AudioTracks only if corresponding property changed.
        if (!nameof(_batchConfiguration.AudioTracks).Equals(eventArgs.PropertyName))
            return;

        AudioTracks = [.. _batchConfiguration.AudioTracks];
    }

    /// <summary>
    /// Clears all audio tracks in the batch configuration.
    /// </summary>
    /// <remarks>Used during development only.</remarks>
    private void ClearAudioTracksAction()
    {
        _batchConfiguration.AudioTracks.Clear();
    }
}
