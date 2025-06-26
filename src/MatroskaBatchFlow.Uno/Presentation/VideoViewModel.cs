using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
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
        MutateNameTrack = new RelayCommand(() => _batchConfiguration.VideoTracks[0].Name = DateTime.Now.ToString());
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
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;

        // Subscribe to property changes for the VideoTracks collection itself.
        // This ensures that any changes to the VideoTracks collection in the ViewModel
        // (such as adding, removing, or reordering tracks) are immediately reflected
        // in the BatchConfiguration's VideoTracks property. This keeps the ViewModel
        // and the underlying batch configuration in sync, regardless of where the change originates.
        VideoTracks.CollectionChanged += (s, e) =>
        {
            _batchConfiguration.VideoTracks = VideoTracks;
        };

        // Subscribe to changes in the VideoTracks collection of the batch configuration.
        _batchConfiguration.VideoTracks.CollectionChanged += (s, e) =>
        {
            void Subscribe(IEnumerable<TrackConfiguration>? items) =>
                items?.ToList().ForEach(t => t.PropertyChanged += OnTrackPropertyChanged);

            void Unsubscribe(IEnumerable<TrackConfiguration>? items) =>
                items?.ToList().ForEach(t => t.PropertyChanged -= OnTrackPropertyChanged);

            if (e.NewItems != null)
                Subscribe(e.NewItems.Cast<TrackConfiguration>());
            if (e.OldItems != null)
                Unsubscribe(e.OldItems.Cast<TrackConfiguration>());

            // Handle the Reset action, which indicates that the entire collection has been replaced or cleared.
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                var all = _batchConfiguration.VideoTracks;
                Unsubscribe(all);
                Subscribe(all);
            }

            VideoTracks = [.. _batchConfiguration.VideoTracks];
        };
    }

    /// <summary>
    /// Handles property change notifications from the batch configuration and its contained tracks.
    /// Updates the <see cref="VideoTracks"/> property or synchronizes the <see cref="SelectedTrack"/> property
    /// with the corresponding track in the batch configuration when a property changes.
    /// </summary>
    /// <param name="sender">The source of the property change event (either the batch configuration or a track).</param>
    /// <param name="e">The event arguments containing the name of the changed property.</param>
    private void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
            return;

        // If the VideoTracks collection itself changed, update the local VideoTracks property
        if (nameof(VideoTracks).Equals(e.PropertyName))
        {
            VideoTracks = [.. _batchConfiguration.VideoTracks];
        }
        // Otherwise, if a property of the currently selected track changed, update the SelectedTrack's property
        else if (SelectedTrack != null && SelectedTrack.Position > 0 && SelectedTrack.Position <= _batchConfiguration.VideoTracks.Count)
        {
            // Get the updated track from the batch configuration by position (1-based index)
            var updatedTrack = _batchConfiguration.VideoTracks[SelectedTrack.Position - 1];
            // Use reflection to get the property info for the changed property
            PropertyInfo? propInfo = typeof(TrackConfiguration).GetProperty(e.PropertyName);
            if (propInfo != null && propInfo.CanRead && propInfo.CanWrite)
            {
                // Get the new value from the updated track and set it on the SelectedTrack
                var value = propInfo.GetValue(updatedTrack);
                propInfo.SetValue(SelectedTrack, value);
            }
        }
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
