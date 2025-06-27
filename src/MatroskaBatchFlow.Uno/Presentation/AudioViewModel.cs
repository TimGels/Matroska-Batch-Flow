using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class AudioViewModel : TrackViewModelBase
{
    private ObservableCollection<TrackConfiguration> _audioTracks = [];
    public ObservableCollection<TrackConfiguration> AudioTracks
    {
        get => _audioTracks;
        set
        {
            if (_audioTracks != value)
            {
                _audioTracks = value;
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

    /// <summary>
    /// Sets up event handlers to synchronize the state between the <see cref="IBatchConfiguration"/> and the <see cref="AudioTracks"/> collection.
    /// </summary>
    /// <remarks>This method ensures that changes to the <see cref="AudioTracks"/> collection in the ViewModel
    /// are reflected in the underlying batch configuration, and vice versa. It subscribes to property change
    /// notifications and collection change events to maintain synchronization. Additionally, it handles scenarios such
    /// as adding, removing, or resetting items in the collections.</remarks>
    protected override void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;

        AudioTracks.CollectionChanged += (s, e) =>
        {
            _batchConfiguration.AudioTracks = AudioTracks;
        };

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

    /// <summary>
    /// Handles changes to the batch configuration by updating the audio tracks or the selected track's properties.
    /// </summary>
    /// <remarks>This method updates the <see cref="AudioTracks"/> collection if the property name matches 
    /// <see cref="AudioTracks"/>. If the property name corresponds to a property of the selected track  and the
    /// selected track's position is valid, the method updates the corresponding property of the  selected track with
    /// the value from the updated track in the batch configuration.</remarks>
    /// <param name="sender">The source of the event. This parameter is not used in the method.</param>
    /// <param name="eventArgs">The event arguments containing the name of the property that changed.</param>
    private void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is null)
            return;

        if (nameof(AudioTracks).Equals(eventArgs.PropertyName))
        {
            AudioTracks = [.. _batchConfiguration.AudioTracks];
        } else if (SelectedTrack != null && SelectedTrack.Position > 0 && SelectedTrack.Position <= _batchConfiguration.AudioTracks.Count)
        {
            var updatedTrack = _batchConfiguration.AudioTracks[SelectedTrack.Position - 1];
            PropertyInfo? propInfo = typeof(TrackConfiguration).GetProperty(eventArgs.PropertyName);
            if (propInfo != null && propInfo.CanRead && propInfo.CanWrite)
            {
                var value = propInfo.GetValue(updatedTrack);
                propInfo.SetValue(SelectedTrack, value);
            }
        }
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
