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

    /// <summary>
    /// Gets a value indicating whether the audio page is enabled based on whether files have been added to the program.
    /// </summary>
    public bool IsFileListPopulated => _batchConfiguration.FileList.Count > 0;

    public AudioViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
        : base(languageProvider, batchConfiguration)
    {
        AudioTracks = [.. _batchConfiguration.AudioTracks];

        SetupEventHandlers();
    }

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => AudioTracks;

    /// <inheritdoc />
    /// <remarks>This method subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the 
    /// batch configuration and the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of the audio 
    /// tracks collection.</remarks>
    protected sealed override void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;
        _batchConfiguration.AudioTracks.CollectionChanged += OnBatchConfigurationAudioTracksChanged;

        // Subscribe to FileList changes to update IsFileListPopulated property.
        _batchConfiguration.FileList.CollectionChanged += OnFileListChanged;
    }

    /// <summary>
    /// Handles changes to the FileList collection to update the IsFileListPopulated property.
    /// </summary>
    /// <param name="sender">The source of the event, typically the FileList collection.</param>
    /// <param name="eventArgs">The event data containing information about the changes to the collection.</param>
    private void OnFileListChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        OnPropertyChanged(nameof(IsFileListPopulated));
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
}
