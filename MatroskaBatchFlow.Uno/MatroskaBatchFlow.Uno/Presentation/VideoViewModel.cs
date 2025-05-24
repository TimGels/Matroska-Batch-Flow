using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using MatroskaBatchFlow.Core.Enums;
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

    public VideoViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
    {
        _batchConfiguration = batchConfiguration;
        ClearVideoTracks = new RelayCommand(ClearVideoTracksAction);
        _languages = languageProvider.Languages;

        batchConfiguration.VideoTracks.AddRange([
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
        ]);

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

        foreach (var track in VideoTracks)
        {
            // Subscribe to property changes for each track in the VideoTracks collection.
            track.PropertyChanged += OnTrackPropertyChanged;
        }

        VideoTracks.CollectionChanged += (s, e) =>
        {
            _batchConfiguration.VideoTracks = VideoTracks;
        };
    }

    /// <summary>
    /// Handles property changes for individual tracks in the VideoTracks collection.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTrackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If the model VideoTracks collection itself changed, update the local VideoTracks property
        if (nameof(VideoTracks).Equals(e.PropertyName))
        {
            VideoTracks = [.. _batchConfiguration.VideoTracks];
        }
    }


    /// <summary>
    /// Handles changes to the SelectedTrack property.
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    partial void OnSelectedTrackChanged(TrackConfiguration? oldValue, TrackConfiguration? newValue)
    {
        if (newValue != null)
        {
            // Synchronize properties with the selected track
            IsDefaultTrack = newValue.Default;
            IsEnabledTrack = !newValue.Remove;
            IsForcedTrack = newValue.Forced;
            TrackName = newValue.Name;
        }
    }

    /// <summary>
    /// Updates the properties of the selected track in the batch configuration.
    /// </summary>
    /// <param name="updateAction"></param>
    private void UpdateBatchConfigTrackProperty(Action<TrackConfiguration> updateAction)
    {
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
