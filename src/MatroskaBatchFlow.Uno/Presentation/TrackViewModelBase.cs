using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public abstract partial class TrackViewModelBase : ObservableObject
{
    protected bool _suppressBatchConfigUpdate = false;
    protected ObservableCollection<TrackConfiguration> _tracks = [];

    protected ImmutableList<MatroskaLanguageOption> _languages;
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

    private TrackConfiguration? _selectedTrack;
    public TrackConfiguration? SelectedTrack
    {
        get => _selectedTrack;
        set
        {
            if (!EqualityComparer<TrackConfiguration?>.Default.Equals(_selectedTrack, value))
            {
                _selectedTrack = value;
                OnPropertyChanged(nameof(SelectedTrack));
                OnSelectedTrackChanged(value);
            }
        }
    }

    protected bool _isDefaultTrack = true;

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

    protected bool _isEnabledTrack = true;

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

    protected bool _isForcedTrack = true;

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

    protected string _trackName = string.Empty;

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

    protected MatroskaLanguageOption? _selectedLanguage = null;

    public MatroskaLanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!EqualityComparer<MatroskaLanguageOption?>.Default.Equals(_selectedLanguage, value))
            {
                _selectedLanguage = value;
                OnPropertyChanged(nameof(SelectedLanguage));
                UpdateBatchConfigTrackProperty(tc => tc.Language = value);
            }
        }
    }

    public bool IsTrackSelected => SelectedTrack is not null && GetTracks().Count > 0;

    protected readonly IBatchConfiguration _batchConfiguration;

    protected TrackViewModelBase(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
    {
        _batchConfiguration = batchConfiguration;
        _languages = languageProvider.Languages;
    }

    /// <summary>
    /// Retrieves a collection of track configurations.
    /// </summary>
    /// <returns>A list of <see cref="TrackConfiguration"/> objects representing the available tracks. 
    /// If no tracks are available, an empty list is returned.</returns>
    protected abstract IList<TrackConfiguration> GetTracks();

    /// <summary>
    /// Sets up event handlers for monitoring changes in the batch configuration and its specific tracks collection. 
    /// Needs to be called in the constructor of derived classes.
    /// </summary>
    protected abstract void SetupEventHandlers();

    /// <summary>
    /// Handles changes to the batch configuration and updates the relevant track collection property when the corresponding configuration property changes.
    /// </summary>
    /// <param name="sender">The source of the event. This parameter is typically the batch configuration object.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed.</param>
    protected abstract void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs);

    /// <summary>
    /// Handles property change notifications for the selected track and updates corresponding properties.
    /// </summary>
    /// <remarks>This method synchronizes the properties of the selected track with the associated UI state. 
    /// It suppresses batch configuration updates during the synchronization process to prevent potential
    /// recursion.</remarks>
    /// <param name="sender">The source of the property change event, typically the selected track.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed.</param>
    protected virtual void OnTrackPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender != SelectedTrack || SelectedTrack is null)
            return;

        // Suppress batch configuration updates while synchronizing properties to avoid (potential) recursion. 
        _suppressBatchConfigUpdate = true;

        switch (eventArgs.PropertyName)
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
            case nameof(TrackConfiguration.Language):
                SelectedLanguage = SelectedTrack.Language;
                break;
        }

        _suppressBatchConfigUpdate = false;
    }

    /// <summary>
    /// Updates the properties of the currently selected track in the batch configuration using the provided update action.
    /// </summary>
    /// <remarks>This method applies the provided update action to the track at the position of the currently
    /// selected track. If updates are suppressed or no track is selected, the method performs no operation.</remarks>
    /// <param name="updateAction">An <see cref="Action{TrackConfiguration}"/> delegate that defines the update to apply to the selected track's
    /// configuration.</param>
    protected virtual void UpdateBatchConfigTrackProperty(Action<TrackConfiguration> updateAction)
    {
        // If suppressing updates, do nothing to avoid (potential) recursion.
        if (_suppressBatchConfigUpdate)
            return;
        if (SelectedTrack == null || GetTracks() == null)
            return;

        int index = SelectedTrack.Index;
        var tracks = GetTracks();
        if (index >= 0 && index < tracks.Count)
        {
            // Apply the update action to the selected track.
            updateAction(tracks[index]);
        }
    }

    /// <summary>
    /// Updates the bound view properties when the selected track changes to reflect the state of the newly selected track.
    /// </summary>
    /// <param name="newValue">The newly selected <see cref="TrackConfiguration"/>, or <see langword="null"/> if no track is currently selected.</param>
    protected virtual void OnSelectedTrackChanged(TrackConfiguration? newValue)
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
        SelectedLanguage = newValue.Language;

        _suppressBatchConfigUpdate = false;
    }
}
