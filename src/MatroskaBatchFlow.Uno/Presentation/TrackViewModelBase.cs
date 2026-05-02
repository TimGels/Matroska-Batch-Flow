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
    protected ObservableCollection<TrackIntent> _tracks = [];

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

    private TrackIntent? _selectedTrack;

    public TrackIntent? SelectedTrack
    {
        get => _selectedTrack;
        set
        {
            if (!ReferenceEquals(_selectedTrack, value))
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
                UpdateTrackIntentProperty(intent => intent.Default = value);
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
                UpdateTrackIntentProperty(intent => intent.ShouldModifyDefaultFlag = value);
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
                UpdateTrackIntentProperty(intent => intent.Enabled = value);
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
                UpdateTrackIntentProperty(intent => intent.ShouldModifyEnabledFlag = value);
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
                UpdateTrackIntentProperty(intent => intent.Forced = value);
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
                UpdateTrackIntentProperty(intent => intent.ShouldModifyForcedFlag = value);
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
                UpdateTrackIntentProperty(intent => intent.Name = value);
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
                UpdateTrackIntentProperty(intent => intent.ShouldModifyName = value);
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
                UpdateTrackIntentProperty(intent => intent.Language = language);
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
                UpdateTrackIntentProperty(intent => intent.ShouldModifyLanguage = value);
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
    /// Retrieves a collection of track intents.
    /// </summary>
    /// <returns>A list of <see cref="TrackIntent"/> objects representing the available tracks. 
    /// If no tracks are available, an empty list is returned.</returns>
    protected abstract IList<TrackIntent> GetTracks();

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
    /// Handles property change notifications from a track intent and re-applies the selected track's properties.
    /// </summary>
    /// <remarks>
    /// The current implementation re-applies all bound properties whenever the selected track changes.
    /// This keeps the view state simple and avoids per-property synchronization code.
    /// </remarks>
    /// <param name="sender">The source of the property change event, typically a <see cref="TrackIntent"/>.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed.</param>
    protected virtual void OnTrackPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (SelectedTrack is null)
            return;

        // Only respond to property changes from the currently selected track
        if (!ReferenceEquals(sender, SelectedTrack))
            return;

        // Re-apply all properties from the selected track.
        // Suppress batch config updates to avoid write-back recursion.
        _suppressBatchConfigUpdate = true;

        try
        {
            ApplyTrackProperties(SelectedTrack);
        }
        finally
        {
            _suppressBatchConfigUpdate = false;
        }
    }

    /// <summary>
    /// Updates a property on the selected track intent, guarded by the suppression flag.
    /// </summary>
    /// <param name="updateAction">An <see cref="Action{TrackIntent}"/> delegate that updates the intent property.</param>
    protected virtual void UpdateTrackIntentProperty(Action<TrackIntent> updateAction)
    {
        if (_suppressBatchConfigUpdate)
            return;
        if (SelectedTrack is null || GetTracks().Count == 0)
            return;

        int index = SelectedTrack.Index;
        var tracks = GetTracks();
        if (index < 0 || index >= tracks.Count)
            return;

        updateAction(tracks[index]);
    }

    /// <summary>
    /// Updates the bound view properties when the selected track changes to reflect the state of the newly selected track.
    /// </summary>
    /// <param name="newSelectedTrack">The newly selected <see cref="TrackIntent"/>, or <see langword="null"/> if no track is currently selected.</param>
    protected virtual void OnSelectedTrackChanged(TrackIntent? newSelectedTrack)
    {
        // Raise event to re-calculate IsTrackSelected
        OnPropertyChanged(nameof(IsTrackSelected));

        ApplyTrackProperties(newSelectedTrack);
    }

    /// <summary>
    /// Updates the track-related properties based on the specified <see cref="TrackIntent"/> instance.
    /// </summary>
    /// <param name="intent">The <see cref="TrackIntent"/> instance containing the track properties to apply. If <paramref
    /// name="intent"/> is <see langword="null"/>, all track-related properties are reset to default values.</param>
    private void ApplyTrackProperties(TrackIntent? intent)
    {
        // Suppress batch config updates while synchronizing properties to avoid recursion.
        _suppressBatchConfigUpdate = true;

        try
        {
            if (intent is null)
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

            // Synchronize properties with the selected track intent.
            IsDefaultTrack = intent.Default;
            IsEnabledTrack = intent.Enabled;
            IsForcedTrack = intent.Forced;
            TrackName = intent.Name;
            SelectedLanguage = intent.Language;
            IsDefaultFlagModificationEnabled = intent.ShouldModifyDefaultFlag;
            IsEnabledFlagModificationEnabled = intent.ShouldModifyEnabledFlag;
            IsForcedFlagModificationEnabled = intent.ShouldModifyForcedFlag;
            IsTrackNameModificationEnabled = intent.ShouldModifyName;
            IsSelectedLanguageModificationEnabled = intent.ShouldModifyLanguage;
        }
        finally
        {
            _suppressBatchConfigUpdate = false;
        }
    }
}
