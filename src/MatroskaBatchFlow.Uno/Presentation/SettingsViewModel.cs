using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Utilities;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationSettingsService _validationSettingsService;

    [ObservableProperty]
    private string customMkvPropeditPath;

    [ObservableProperty]
    private bool isCustomMkvPropeditPathEnabled;

    // Batch Validation Settings
    [ObservableProperty]
    private int selectedStrictnessModeIndex;

    [ObservableProperty]
    private int trackCountParitySeverityIndex;

    [ObservableProperty]
    private int audioLanguageSeverityIndex;

    [ObservableProperty]
    private int audioDefaultFlagSeverityIndex;

    [ObservableProperty]
    private int audioForcedFlagSeverityIndex;

    [ObservableProperty]
    private int videoLanguageSeverityIndex;

    [ObservableProperty]
    private int videoDefaultFlagSeverityIndex;

    [ObservableProperty]
    private int subtitleLanguageSeverityIndex;

    [ObservableProperty]
    private int subtitleForcedFlagSeverityIndex;

    public bool IsCustomMode => SelectedStrictnessModeIndex == (int)StrictnessMode.Custom;
    public bool IsCustomStrictnessEnabled => IsCustomMode;

    public SettingsViewModel(IWritableSettings<UserSettings> userSettings, IValidationSettingsService validationSettingsService)
    {
        _userSettings = userSettings;
        _validationSettingsService = validationSettingsService;

        customMkvPropeditPath = ExecutableLocator.FindExecutable(_userSettings.Value.MkvPropedit.CustomPath ?? string.Empty) ?? string.Empty;
        // If Skia is being used, we force the custom path to be enabled since the bundled mkvpropedit executable is only for Windows.
        isCustomMkvPropeditPathEnabled = PlatformInfo.IsSkia || _userSettings.Value.MkvPropedit.IsCustomPathEnabled;

        // Load batch validation settings
        LoadValidationSettings();
    }

    private async Task SaveSettingsAsync()
    {
        await _userSettings.UpdateAsync(settings =>
        {
            settings.MkvPropedit.CustomPath = ExecutableLocator.FindExecutable(CustomMkvPropeditPath);
            settings.MkvPropedit.IsCustomPathEnabled = IsCustomMkvPropeditPathEnabled;
        });
    }

    /// <summary>
    /// Handles changes to the IsCustomMkvPropeditPathEnabled property.
    /// </summary>
    /// <param name="value">The new value of the property.</param>
    async partial void OnIsCustomMkvPropeditPathEnabledChanged(bool value)
    {
        try
        {
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            // TODO: Handle exception properly (e.g., log it, show a message to the user, etc.)
        }
    }

    /// <summary>
    /// Handles changes to the CustomMkvPropeditPath property.
    /// </summary>
    /// <param name="value">The new value of the property.</param>
    async partial void OnCustomMkvPropeditPathChanged(string value)
    {
        try
        {
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            // TODO: Handle exception properly (e.g., log it, show a message to the user, etc.)
        }
    }

    /// <summary>
    /// Handles changes to the selected strictness mode.
    /// </summary>
    /// <param name="value">The new index value.</param>
    async partial void OnSelectedStrictnessModeIndexChanged(int value)
    {
        var mode = (StrictnessMode)value;
        
        try
        {
            // Delegate mode switching logic to the service
            await _userSettings.UpdateAsync(settings =>
            {
                _validationSettingsService.SwitchMode(settings, mode);
            });

            // Reload from file to update UI
            LoadValidationSettings();
            
            // Notify UI of all changes
            NotifyAllValidationPropertiesChanged();
            OnPropertyChanged(nameof(IsCustomMode));
            OnPropertyChanged(nameof(IsCustomStrictnessEnabled));
        }
        catch (Exception ex)
        {
            // TODO: Log exception and show error to user
        }
    }

    /// <summary>
    /// Handles changes to validation severity settings in custom mode.
    /// </summary>
    async partial void OnTrackCountParitySeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnAudioLanguageSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnAudioDefaultFlagSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnAudioForcedFlagSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnVideoLanguageSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnVideoDefaultFlagSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnSubtitleLanguageSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    async partial void OnSubtitleForcedFlagSeverityIndexChanged(int value)
    {
        await SaveValidationSeverityAsync();
    }

    /// <summary>
    /// Loads validation settings from user settings into ViewModel properties.
    /// </summary>
    private void LoadValidationSettings()
    {
        var settings = _userSettings.Value.BatchValidation;
        selectedStrictnessModeIndex = (int)settings.Mode;
        
        // If mode is Strict or Lenient, display preset values (not saved custom values)
        if (settings.Mode != StrictnessMode.Custom)
        {
            var tempSettings = new ValidationSeveritySettings();
            _validationSettingsService.ApplyPreset(tempSettings, settings.Mode);
            UpdateViewModelPropertiesFromSettings(tempSettings);
        }
        else
        {
            // Custom mode - load actual saved custom values
            UpdateViewModelPropertiesFromSettings(settings.CustomSettings);
        }
    }
    
    /// <summary>
    /// Updates all validation severity properties from a ValidationSeveritySettings object.
    /// </summary>
    private void UpdateViewModelPropertiesFromSettings(ValidationSeveritySettings settings)
    {
        trackCountParitySeverityIndex = (int)settings.TrackCountParity;
        audioLanguageSeverityIndex = (int)settings.AudioTrackValidation.Language;
        audioDefaultFlagSeverityIndex = (int)settings.AudioTrackValidation.DefaultFlag;
        audioForcedFlagSeverityIndex = (int)settings.AudioTrackValidation.ForcedFlag;
        videoLanguageSeverityIndex = (int)settings.VideoTrackValidation.Language;
        videoDefaultFlagSeverityIndex = (int)settings.VideoTrackValidation.DefaultFlag;
        subtitleLanguageSeverityIndex = (int)settings.SubtitleTrackValidation.Language;
        subtitleForcedFlagSeverityIndex = (int)settings.SubtitleTrackValidation.ForcedFlag;
    }
    
    /// <summary>
    /// Notifies the UI that all validation severity properties have changed.
    /// </summary>
    private void NotifyAllValidationPropertiesChanged()
    {
        OnPropertyChanged(nameof(TrackCountParitySeverityIndex));
        OnPropertyChanged(nameof(AudioLanguageSeverityIndex));
        OnPropertyChanged(nameof(AudioDefaultFlagSeverityIndex));
        OnPropertyChanged(nameof(AudioForcedFlagSeverityIndex));
        OnPropertyChanged(nameof(VideoLanguageSeverityIndex));
        OnPropertyChanged(nameof(VideoDefaultFlagSeverityIndex));
        OnPropertyChanged(nameof(SubtitleLanguageSeverityIndex));
        OnPropertyChanged(nameof(SubtitleForcedFlagSeverityIndex));
    }

    /// <summary>
    /// Saves validation severity settings (for custom mode).
    /// </summary>
    private async Task SaveValidationSeverityAsync()
    {
        // Only save custom settings when in Custom mode
        if (SelectedStrictnessModeIndex != (int)StrictnessMode.Custom)
        {
            return;
        }

        try
        {
            await _userSettings.UpdateAsync(settings =>
            {
                var custom = settings.BatchValidation.CustomSettings;
                custom.TrackCountParity = (ValidationSeverity)TrackCountParitySeverityIndex;
                custom.AudioTrackValidation.Language = (ValidationSeverity)AudioLanguageSeverityIndex;
                custom.AudioTrackValidation.DefaultFlag = (ValidationSeverity)AudioDefaultFlagSeverityIndex;
                custom.AudioTrackValidation.ForcedFlag = (ValidationSeverity)AudioForcedFlagSeverityIndex;
                custom.VideoTrackValidation.Language = (ValidationSeverity)VideoLanguageSeverityIndex;
                custom.VideoTrackValidation.DefaultFlag = (ValidationSeverity)VideoDefaultFlagSeverityIndex;
                custom.SubtitleTrackValidation.Language = (ValidationSeverity)SubtitleLanguageSeverityIndex;
                custom.SubtitleTrackValidation.ForcedFlag = (ValidationSeverity)SubtitleForcedFlagSeverityIndex;
            });
        }
        catch (Exception ex)
        {
            // TODO: Log exception and show error to user
        }
    }
}
