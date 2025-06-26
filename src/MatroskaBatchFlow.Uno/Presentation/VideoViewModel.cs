using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class VideoViewModel : ObservableObject
{
    private ImmutableList<MatroskaLanguageOption> _languages = [];
    private ObservableCollection<TrackConfiguration> _videoTracks = [];
    private bool _isDefaultTrack = true;
    private bool _isEnabledTrack = true;
    private bool _isForcedTrack = true;
    private string _trackName = string.Empty;
    private bool _suppressBatchConfigUpdate = false;
    public bool IsTrackSelected => SelectedTrack is not null && VideoTracks.Count > 0;
    public ImmutableList<MatroskaLanguageOption> Languages
    {
        get => _languages;
        set
        {
            if (_languages != value)
            {
                _languages = value;
                OnPropertyChanged(nameof(Languages));
            }
        }
    }

    private readonly IBatchConfiguration _batchConfiguration;

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

    [ObservableProperty]
    public TrackConfiguration? _selectedTrack = null!;

    public bool IsDefaultTrack
    {
        get => _isDefaultTrack;
        set
        {
            if (_isDefaultTrack != value)
            {
                _isDefaultTrack = value;
                OnPropertyChanged(nameof(IsDefaultTrack));
                UpdateBatchConfigTrackProperty(tc => tc.Default = value);
            }
        }
    }

    public bool IsEnabledTrack
    {
        get => _isEnabledTrack;
        set
        {
            if (_isEnabledTrack != value)
            {
                _isEnabledTrack = value;
                OnPropertyChanged(nameof(IsEnabledTrack));
                UpdateBatchConfigTrackProperty(tc => tc.Remove = !value);
            }
        }
    }

    public bool IsForcedTrack
    {
        get => _isForcedTrack;
        set
        {
            if (_isForcedTrack != value)
            {
                _isForcedTrack = value;
                OnPropertyChanged(nameof(IsForcedTrack));
                UpdateBatchConfigTrackProperty(tc => tc.Forced = value);
            }
        }
    }

    public string TrackName
    {
        get => _trackName;
        set
        {
            if (_trackName != value)
            {
                _trackName = value;
                OnPropertyChanged(nameof(TrackName));
                UpdateBatchConfigTrackProperty(tc => tc.Name = value);
            }
        }
    }

    public ICommand ClearVideoTracks { get; }
    public ICommand MutateNameTrack { get; }

    public VideoViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
    {
        _batchConfiguration = batchConfiguration;
        ClearVideoTracks = new RelayCommand(ClearVideoTracksAction);
        MutateNameTrack = new RelayCommand(() => _batchConfiguration.VideoTracks[0].Name = DateTime.Now.ToString());
        _languages = languageProvider.Languages;

        SelectedTrack = _batchConfiguration.VideoTracks.FirstOrDefault();

        SetupEventHandlers();

        // Initialize the VideoTracks collection with the tracks from the batch configuration.
        VideoTracks = [.. _batchConfiguration.VideoTracks];
    }

    /// <summary>
    /// Sets up event handlers for property changes in the batch configuration and its video tracks.
    /// </summary>
    private void SetupEventHandlers()
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
    /// Handles property changes for individual tracks in the VideoTracks collection.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTrackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender != SelectedTrack || SelectedTrack is null)
            return;

        // Suppress batch configuration updates while synchronizing properties to avoid (potential) recursion. 
        _suppressBatchConfigUpdate = true;

        switch (e.PropertyName)
        {
            case nameof(TrackConfiguration.Name):
                TrackName = SelectedTrack.Name;
                break;
            case nameof(TrackConfiguration.Default):
                IsDefaultTrack = SelectedTrack.Default;
                break;
            case nameof(TrackConfiguration.Forced):
                IsForcedTrack = SelectedTrack.Forced;
                break;
            case nameof(TrackConfiguration.Remove):
                IsEnabledTrack = !SelectedTrack.Remove;
                break;
        }

        _suppressBatchConfigUpdate = false;
    }

    /// <summary>
    /// Updates properties of the view model when the selected track changes to reflect the state of the newly selected track.
    /// </summary>
    /// <remarks>The method also ensures that during the update, batch configuration updates are suppressed to avoid potential recursion.</remarks>
    /// <param name="oldValue">The previously selected <see cref="TrackConfiguration"/>, or <see langword="null"/> if no track was previously
    /// selected.</param>
    /// <param name="newValue">The newly selected <see cref="TrackConfiguration"/>, or <see langword="null"/> if no track is currently selected.</param>
    partial void OnSelectedTrackChanged(TrackConfiguration? oldValue, TrackConfiguration? newValue)
    {
        // Raise event to re-calculate IsTrackSelected
        OnPropertyChanged(nameof(IsTrackSelected));

        if (newValue == null)
            return;

        // If suppressing updates, do nothing to avoid (potential) recursion.
        _suppressBatchConfigUpdate = true;

        // Synchronize properties with the selected track
        IsDefaultTrack = newValue.Default;
        IsEnabledTrack = !newValue.Remove;
        IsForcedTrack = newValue.Forced;
        TrackName = newValue.Name;

        _suppressBatchConfigUpdate = false;
    }

    /// <summary>
    /// Updates the properties of the currently selected video track in the batch configuration using the specified update
    /// action.
    /// </summary>
    /// <remarks>This method performs no operation if: <list type="bullet"> <item><description>Updates are currently
    /// suppressed.</description></item> <item><description>No track is selected.</description></item>
    /// <item><description>The batch configuration does not contain any video tracks.</description></item> </list> The
    /// method ensures that the selected track's position is valid within the bounds of the video tracks list before
    /// applying the update action.</remarks>
    /// <param name="updateAction">An <see cref="Action{TrackConfiguration}"/> delegate that defines the update to apply to the selected track's
    /// configuration. This action is invoked with the current track's configuration as its parameter.</param>
    private void UpdateBatchConfigTrackProperty(Action<TrackConfiguration> updateAction)
    {
        // If suppressing updates, do nothing to avoid (potential) recursion.
        if (_suppressBatchConfigUpdate)
            return;

        // If no track is selected or the batch configuration does not contain video tracks, do nothing.
        if (SelectedTrack == null || _batchConfiguration.VideoTracks == null)
            return;

        // Position is 1-based, list is 0-based
        int index = SelectedTrack.Position - 1;
        if (index >= 0 && index < _batchConfiguration.VideoTracks.Count)
        {
            // Update the track in the batch configuration using the provided action.
            var track = _batchConfiguration.VideoTracks[index];
            updateAction(track);
        }
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
