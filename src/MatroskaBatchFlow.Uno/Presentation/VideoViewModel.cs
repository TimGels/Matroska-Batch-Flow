using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class VideoViewModel : TrackViewModelBase
{
    public ObservableCollection<TrackConfiguration> VideoTracks
    {
        get => _tracks;
        set
        {
            if (_tracks != value)
            {
                _tracks = value;
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

    /// <inheritdoc />
    /// <remarks>This method subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the 
    /// batch configuration and the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of the video 
    /// tracks collection.</remarks>
    protected override void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;
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

    /// <inheritdoc />
    /// <remarks>
    /// This method listens for changes to the <c>VideoTracks</c> property in the batch configuration
    /// and updates the <see cref="VideoTracks"/> property accordingly. If the <c>PropertyName</c> in <paramref
    /// name="eventArgs"/> is <c>null</c> or does not match the <c>VideoTracks</c> property, the method exits without
    /// making changes.
    /// </remarks>
    /// <param name="sender">The source of the event. This parameter is not used in the method.</param>
    /// <param name="eventArgs">The event data containing the name of the changed property. The <see
    /// cref="PropertyChangedEventArgs.PropertyName"/> must not be <see langword="null"/>.</param>
    protected override void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is null)
            return;
        // Replace VideoTracks only if the corresponding property changed.
        if (!nameof(_batchConfiguration.VideoTracks).Equals(eventArgs.PropertyName))
            return;

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
