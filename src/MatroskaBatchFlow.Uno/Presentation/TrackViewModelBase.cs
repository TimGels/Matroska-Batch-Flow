using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public abstract partial class TrackViewModelBase : ObservableObject
{
    private readonly ILogger _logger;
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
                UpdateBatchConfigTrackProperty(tc => tc.Default = value, ftv => ftv.Default = value);
            }
        }
    }

    protected bool _isDefaultFlagModificationEnabled = false;

    public bool IsDefaultFlagModificationEnabled
    {
        get => _isDefaultFlagModificationEnabled;
        set
        {
            if (_isDefaultFlagModificationEnabled != value)
            {
                _isDefaultFlagModificationEnabled = value;
                OnPropertyChanged(nameof(IsDefaultFlagModificationEnabled));
                UpdateGlobalModificationFlag(tc => tc.ShouldModifyDefaultFlag = value);
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
                UpdateBatchConfigTrackProperty(tc => tc.Enabled = value, ftv => ftv.Enabled = value);
            }
        }
    }

    protected bool _isEnabledFlagModificationEnabled = false;

    public bool IsEnabledFlagModificationEnabled
    {
        get => _isEnabledFlagModificationEnabled;
        set
        {
            if (_isEnabledFlagModificationEnabled != value)
            {
                _isEnabledFlagModificationEnabled = value;
                OnPropertyChanged(nameof(IsEnabledFlagModificationEnabled));
                UpdateGlobalModificationFlag(tc => tc.ShouldModifyEnabledFlag = value);
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
                UpdateBatchConfigTrackProperty(tc => tc.Forced = value, ftv => ftv.Forced = value);
            }
        }
    }

    protected bool _isForcedFlagModificationEnabled = false;

    public bool IsForcedFlagModificationEnabled
    {
        get => _isForcedFlagModificationEnabled;
        set
        {
            if (_isForcedFlagModificationEnabled != value)
            {
                _isForcedFlagModificationEnabled = value;
                OnPropertyChanged(nameof(IsForcedFlagModificationEnabled));
                UpdateGlobalModificationFlag(tc => tc.ShouldModifyForcedFlag = value);
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
                UpdateBatchConfigTrackProperty(tc => tc.Name = value, ftv => ftv.Name = value);
            }
        }
    }

    protected bool _isTrackNameModificationEnabled = false;

    public bool IsTrackNameModificationEnabled
    {
        get => _isTrackNameModificationEnabled;
        set
        {
            if (_isTrackNameModificationEnabled != value)
            {
                _isTrackNameModificationEnabled = value;
                OnPropertyChanged(nameof(IsTrackNameModificationEnabled));
                UpdateGlobalModificationFlag(tc => tc.ShouldModifyName = value);
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

                // Only warn when null comes from outside the internal sync path (e.g., ComboBox clearing its selection).
                // During internal resets (ApplyTrackProperties), _suppressBatchConfigUpdate is true and null is expected.
                if (value is null && !_suppressBatchConfigUpdate)
                {
                    LogSelectedLanguageReceivedNull();
                }

                MatroskaLanguageOption language = value ?? MatroskaLanguageOption.Undetermined;
                UpdateBatchConfigTrackProperty(tc => tc.Language = language, ftv => ftv.Language = language);
            }
        }
    }

    protected bool _isSelectedLanguageModificationEnabled = false;

    public bool IsSelectedLanguageModificationEnabled
    {
        get => _isSelectedLanguageModificationEnabled;
        set
        {
            if (_isSelectedLanguageModificationEnabled != value)
            {
                _isSelectedLanguageModificationEnabled = value;
                OnPropertyChanged(nameof(IsSelectedLanguageModificationEnabled));
                UpdateGlobalModificationFlag(tc => tc.ShouldModifyLanguage = value);
            }
        }
    }

    public bool IsTrackSelected => SelectedTrack is not null && GetTracks().Count > 0;

    protected readonly IBatchConfiguration _batchConfiguration;
    private readonly IUIPreferencesService _uiPreferences;

    public bool ShowTrackAvailabilityText => _uiPreferences.ShowTrackAvailabilityText;

    protected TrackViewModelBase(ILogger logger, ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration, IUIPreferencesService uiPreferences)
    {
        _logger = logger;
        _batchConfiguration = batchConfiguration;
        _uiPreferences = uiPreferences;
        _languages = languageProvider.Languages;

        // Subscribe to property changes on the service
        _uiPreferences.PropertyChanged += OnUIPreferencesChanged;
    }

    private void OnUIPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IUIPreferencesService.ShowTrackAvailabilityText))
        {
            OnPropertyChanged(nameof(ShowTrackAvailabilityText));
        }
    }

    /// <summary>
    /// Gets the track type for this ViewModel (Audio, Video, or Subtitle).
    /// </summary>
    protected abstract TrackType GetTrackType();

    /// <summary>
    /// Calculates how many files in the batch have this specific track index.
    /// </summary>
    /// <param name="trackIndex">Zero-based track index.</param>
    /// <returns>Number of files that have this track.</returns>
    public int GetTrackAvailabilityCount(int trackIndex)
    {
        if (_batchConfiguration.FileList.Count == 0)
            return 0;

        var trackType = GetTrackType();
        int count = 0;

        foreach (var file in _batchConfiguration.FileList)
        {
            if (file.HasTrack(trackType, trackIndex))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Gets the total number of files in the batch.
    /// </summary>
    public int TotalFileCount => _batchConfiguration.FileList.Count;

    /// <summary>
    /// Gets a formatted string showing track availability (e.g., "3/5 files").
    /// </summary>
    /// <param name="trackIndex">Zero-based track index.</param>
    /// <returns>Formatted availability string.</returns>
    public string GetTrackAvailabilityText(int trackIndex)
    {
        int available = GetTrackAvailabilityCount(trackIndex);
        int total = TotalFileCount;

        if (total == 0)
            return "0/0";

        return $"{available}/{total}";
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
        if (SelectedTrack is null)
            return;

        // Only respond to property changes from the currently selected track
        if (!ReferenceEquals(sender, SelectedTrack))
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
            case nameof(TrackConfiguration.Enabled):
                IsEnabledTrack = SelectedTrack.Enabled;
                break;
            case nameof(TrackConfiguration.Language):
                SelectedLanguage = SelectedTrack.Language;
                break;
            case nameof(TrackConfiguration.ShouldModifyDefaultFlag):
                IsDefaultFlagModificationEnabled = SelectedTrack.ShouldModifyDefaultFlag;
                break;
            case nameof(TrackConfiguration.ShouldModifyEnabledFlag):
                IsEnabledFlagModificationEnabled = SelectedTrack.ShouldModifyEnabledFlag;
                break;
            case nameof(TrackConfiguration.ShouldModifyForcedFlag):
                IsForcedFlagModificationEnabled = SelectedTrack.ShouldModifyForcedFlag;
                break;
            case nameof(TrackConfiguration.ShouldModifyName):
                IsTrackNameModificationEnabled = SelectedTrack.ShouldModifyName;
                break;
            case nameof(TrackConfiguration.ShouldModifyLanguage):
                IsSelectedLanguageModificationEnabled = SelectedTrack.ShouldModifyLanguage;
                break;
        }

        _suppressBatchConfigUpdate = false;
    }

    /// <summary>
    /// Updates the properties of the currently selected track in the batch configuration using the provided update action.
    /// </summary>
    /// <remarks>
    /// This method applies the provided update actions to both:
    /// <list type="bullet">
    /// <item>The global track collection (used for UI display) - triggers PropertyChanged which fires StateChanged</item>
    /// <item>All per-file track value collections (used for command generation) - updated silently</item>
    /// </list>
    /// The global track update will trigger the StateChanged event through its PropertyChanged handler,
    /// ensuring the UI and command generation stay synchronized.
    /// If updates are suppressed or no track is selected, the method performs no operation.
    /// </remarks>
    /// <param name="globalUpdateAction">An <see cref="Action{TrackConfiguration}"/> delegate that defines the update to apply to the global track configuration.</param>
    /// <param name="perFileUpdateAction">An <see cref="Action{FileTrackValues}"/> delegate that defines the update to apply to each per-file track value.</param>
    protected virtual void UpdateBatchConfigTrackProperty(
        Action<TrackConfiguration> globalUpdateAction,
        Action<FileTrackValues> perFileUpdateAction)
    {
        // If suppressing updates, do nothing to avoid (potential) recursion.
        if (_suppressBatchConfigUpdate)
            return;
        if (SelectedTrack == null || GetTracks() == null)
            return;

        int index = SelectedTrack.Index;
        var tracks = GetTracks();
        if (index < 0 || index >= tracks.Count)
            return;

        // First, update all per-file value collections silently (without triggering events)
        // This keeps per-file configurations in sync with the global track settings used for command generation
        var trackType = SelectedTrack.Type;
        foreach (var kvp in _batchConfiguration.FileConfigurations)
        {
            var fileConfig = kvp.Value;
            var fileTracks = fileConfig.GetTrackListForType(trackType);

            // Only update if this file actually has this track
            if (index >= 0 && index < fileTracks.Count)
            {
                perFileUpdateAction(fileTracks[index]);
            }
        }

        // Finally, apply the update action to the global track configuration
        // This will trigger PropertyChanged -> TrackConfiguration_PropertyChanged -> StateChanged
        // which updates CanProcessBatch and regenerates commands
        globalUpdateAction(tracks[index]);
    }

    /// <summary>
    /// Updates only the global <see cref="TrackConfiguration"/> for a modification flag (e.g. <c>ShouldModify*</c>).
    /// Per-file track values do not carry modification flags, so only the global track is updated.
    /// </summary>
    /// <remarks>
    /// If updates are suppressed or no track is selected, the method performs no operation.
    /// </remarks>
    /// <param name="updateAction">An <see cref="Action{TrackConfiguration}"/> delegate that sets the modification flag on the global track configuration.</param>
    protected virtual void UpdateGlobalModificationFlag(Action<TrackConfiguration> updateAction)
    {
        // If suppressing updates, do nothing to avoid (potential) recursion.
        if (_suppressBatchConfigUpdate)
            return;
        if (SelectedTrack == null || GetTracks() == null)
            return;

        int index = SelectedTrack.Index;
        var tracks = GetTracks();
        if (index < 0 || index >= tracks.Count)
            return;

        // Apply the update action to the global track configuration only
        // This will trigger PropertyChanged -> TrackConfiguration_PropertyChanged -> StateChanged
        updateAction(tracks[index]);
    }

    /// <summary>
    /// Updates the bound view properties when the selected track changes to reflect the state of the newly selected track.
    /// </summary>
    /// <param name="newSelectedTrack">The newly selected <see cref="TrackConfiguration"/>, or <see langword="null"/> if no track is currently selected.</param>
    protected virtual void OnSelectedTrackChanged(TrackConfiguration? newSelectedTrack)
    {
        // Raise event to re-calculate IsTrackSelected
        OnPropertyChanged(nameof(IsTrackSelected));

        ApplyTrackProperties(newSelectedTrack);
    }

    /// <summary>
    /// Updates the track-related properties based on the specified <see cref="TrackConfiguration"/> instance.
    /// </summary>
    /// <param name="track">The <see cref="TrackConfiguration"/> instance containing the track properties to apply. If <paramref
    /// name="track"/> is <see langword="null"/>, all track-related properties are reset to default values.</param>
    private void ApplyTrackProperties(TrackConfiguration? track)
    {
        // Suppress batch config updates while synchronizing properties to avoid (potential) recursion.
        _suppressBatchConfigUpdate = true;

        try
        {
            // TODO: Need a better way to reset properties when no track is provided.
            if (track is null)
            {
                IsDefaultTrack = false;
                IsEnabledTrack = true;
                IsForcedTrack = false;
                TrackName = string.Empty;
                SelectedLanguage = null;
                IsDefaultFlagModificationEnabled = false;
                IsEnabledFlagModificationEnabled = false;
                IsForcedFlagModificationEnabled = false;
                IsTrackNameModificationEnabled = false;
                IsSelectedLanguageModificationEnabled = false;

                return;
            }

            // Synchronize properties with the selected track.
            IsDefaultTrack = track.Default;
            IsEnabledTrack = track.Enabled;
            IsForcedTrack = track.Forced;
            TrackName = track.Name;
            SelectedLanguage = track.Language;
            IsDefaultFlagModificationEnabled = track.ShouldModifyDefaultFlag;
            IsEnabledFlagModificationEnabled = track.ShouldModifyEnabledFlag;
            IsForcedFlagModificationEnabled = track.ShouldModifyForcedFlag;
            IsTrackNameModificationEnabled = track.ShouldModifyName;
            IsSelectedLanguageModificationEnabled = track.ShouldModifyLanguage;
        }
        finally
        {
            _suppressBatchConfigUpdate = false;
        }
    }
}
