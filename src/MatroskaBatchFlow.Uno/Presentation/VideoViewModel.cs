using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class VideoViewModel : TrackViewModelBase
{
    private ObservableCollection<TrackConfiguration> _videoTracks = [];
    public ObservableCollection<TrackConfiguration> VideoTracks
    {
        get => _videoTracks;
        set
        {
            if (_videoTracks != value)
            {
                _videoTracks = value;
                OnPropertyChanged(nameof(VideoTracks));
                OnPropertyChanged(nameof(IsTrackSelected));
            }
        }
    }

    public ICommand ClearVideoTracks { get; }
    public ICommand MutateNameTrack { get; }

    public VideoViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
        : base(languageProvider, batchConfiguration)
    {
        ClearVideoTracks = new RelayCommand(ClearVideoTracksAction);
        MutateNameTrack = new RelayCommand(() => _batchConfiguration.VideoTracks.RemoveAt(0));
        VideoTracks = [.. _batchConfiguration.VideoTracks];
    }

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => VideoTracks;

    /// <summary>
    /// Sets up event handlers to synchronize the state between the <see cref="IBatchConfiguration"/> and the <see cref="VideoTracks"/> collection.
    /// </summary>
    /// <remarks>This method ensures that changes to the <see cref="VideoTracks"/> collection in the ViewModel
    /// are reflected in the underlying batch configuration, and vice versa. It subscribes to property change
    /// notifications and collection change events to maintain synchronization. Additionally, it handles scenarios such
    /// as adding, removing, or resetting items in the collections.</remarks>
    protected override void SetupEventHandlers()
    {
        // Subscribe to property changes in the VideoTracks collection from this ViewModel.
        VideoTracks.CollectionChanged += (s, e) =>
        {
            _batchConfiguration.VideoTracks = VideoTracks;
        };

        // Subscribe to changes in the VideoTracks collection of the batch configuration.
        _batchConfiguration.VideoTracks.CollectionChanged += OnBatchConfigurationVideoTracksChanged;
    }

    /// <summary>
    /// Handles changes to the collection of video track configurations in a batch configuration.
    /// </summary>
    /// <remarks>This method responds to changes in the video track configurations by subscribing to or
    /// unsubscribing from  property change notifications for the affected items. It also handles the <see
    /// cref="NotifyCollectionChangedAction.Reset"/>  action, ensuring that all items in the collection are properly
    /// updated when the collection is replaced or cleared.</remarks>
    /// <param name="sender">The source of the event, typically the collection that was changed.</param>
    /// <param name="eventArgs">The event data containing information about the changes to the collection, such as added, removed, or replaced
    /// items.</param>
    private void OnBatchConfigurationVideoTracksChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        void Subscribe(IEnumerable<TrackConfiguration>? items) =>
            items?.ToList().ForEach(t => t.PropertyChanged += OnTrackPropertyChanged);

        void Unsubscribe(IEnumerable<TrackConfiguration>? items) =>
            items?.ToList().ForEach(t => t.PropertyChanged -= OnTrackPropertyChanged);

        if (eventArgs.NewItems != null)
            Subscribe(eventArgs.NewItems.Cast<TrackConfiguration>());
        if (eventArgs.OldItems != null)
            Unsubscribe(eventArgs.OldItems.Cast<TrackConfiguration>());

        // Handle the Reset action, which indicates that the entire collection has been replaced or cleared.
        if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
        {
            var all = _batchConfiguration.VideoTracks;
            Unsubscribe(all);
            Subscribe(all);
        }

        VideoTracks = [.. _batchConfiguration.VideoTracks];
    }

    /// <summary>
    /// Clears all video tracks from the batch configuration.
    /// <br />
    /// Temporary development method to reset video tracks.
    /// </summary>
    private void ClearVideoTracksAction()
    {
        _batchConfiguration.Clear();
    }
}
