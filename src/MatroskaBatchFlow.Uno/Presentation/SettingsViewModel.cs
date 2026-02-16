using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Enums;
using MatroskaBatchFlow.Uno.Utilities;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationSettingsService _validationSettingsService;
    private readonly IValidationStateService _validationStateService;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ILogLevelService _logLevelService;
    private readonly LoggingOptions _loggingOptions;
    private bool _suppressValidationUpdates;

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

    [ObservableProperty]
    private int selectedThemeIndex;

    [ObservableProperty]
    private int selectedLogLevelIndex;

    [ObservableProperty]
    private bool isLogLevelControlEnabled;

    public string LogLevelDescription => IsLogLevelControlEnabled
        ? "Set the minimum logging level. Lower levels produce more detailed logs."
        : $"Currently set by appsettings.json to '{_loggingOptions.MinimumLevel}' and cannot be changed here.";

    public bool ShowTrackAvailabilityText
    {
        get => _uiPreferences.ShowTrackAvailabilityText;
        set => _uiPreferences.ShowTrackAvailabilityText = value;
    }

    public bool EnableLoggingView
    {
        get => _uiPreferences.EnableLoggingView;
        set => _uiPreferences.EnableLoggingView = value;
    }

    public bool IsCustomMode => SelectedStrictnessModeIndex == (int)StrictnessMode.Custom;
    public bool IsCustomStrictnessEnabled => IsCustomMode;

    public SettingsViewModel(
        IWritableSettings<UserSettings> userSettings,
        IValidationSettingsService validationSettingsService,
        IValidationStateService validationStateService,
        IUIPreferencesService uiPreferences,
        ILogLevelService logLevelService,
        IOptions<LoggingOptions> loggingOptions,
        ILogger<SettingsViewModel> logger)
    {
        _userSettings = userSettings;
        _validationSettingsService = validationSettingsService;
        _validationStateService = validationStateService;
        _uiPreferences = uiPreferences;
        _logLevelService = logLevelService;
        _loggingOptions = loggingOptions.Value;
        _logger = logger;

        customMkvPropeditPath = ExecutableLocator.FindExecutable(_userSettings.Value.MkvPropedit.CustomPath ?? string.Empty) ?? string.Empty;
        // If Skia is being used, we force the custom path to be enabled since the bundled mkvpropedit executable is only for Windows.
        isCustomMkvPropeditPathEnabled = PlatformInfo.IsSkia || _userSettings.Value.MkvPropedit.IsCustomPathEnabled;

        // Load theme setting
        selectedThemeIndex = (int)_uiPreferences.AppTheme;

        // Determine if log level control should be enabled
        // If appsettings.json has a non-empty log level configured, disable user control
        isLogLevelControlEnabled = string.IsNullOrWhiteSpace(_loggingOptions.MinimumLevel);

        // Load log level - show appsettings value if configured, otherwise user setting
        var effectiveLogLevel = !string.IsNullOrWhiteSpace(_loggingOptions.MinimumLevel)
            ? _loggingOptions.MinimumLevel
            : _userSettings.Value.UI.LogLevel;
        selectedLogLevelIndex = ConvertLogLevelToIndex(effectiveLogLevel);

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
            LogSaveMkvPropeditPathEnabledFailed(ex);
            // TODO: add weakreference messenger dialog message here
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
            LogSaveCustomPathFailed(ex);
        }
    }

    /// <summary>
    /// Handles changes to the selected strictness mode.
    /// </summary>
    /// <param name="value">The new index value.</param>
    async partial void OnSelectedStrictnessModeIndexChanged(int value)
    {
        if (_suppressValidationUpdates)
        {
            return;
        }

        var previousMode = _userSettings.Value.BatchValidation.Mode;
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

            if (previousMode != mode)
            {
                LogValidationStrictnessChanged(previousMode, mode);
            }

            // Trigger re-validation of the current batch
            _validationStateService.Revalidate();

            // Notify UI of all changes
            NotifyAllValidationPropertiesChanged();
            OnPropertyChanged(nameof(IsCustomMode));
            OnPropertyChanged(nameof(IsCustomStrictnessEnabled));
        }
        catch (Exception ex)
        {
            LogSaveStrictnessFailed(mode, ex);
        }
    }

    /// <summary>
    /// Handles changes to validation severity settings in custom mode.
    /// </summary>
    async partial void OnTrackCountParitySeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("TrackCountParity", () => _userSettings.Value.BatchValidation.CustomSettings.TrackCountParity, value);

    async partial void OnAudioLanguageSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("AudioLanguage", () => _userSettings.Value.BatchValidation.CustomSettings.AudioTrackValidation.Language, value);

    async partial void OnAudioDefaultFlagSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("AudioDefaultFlag", () => _userSettings.Value.BatchValidation.CustomSettings.AudioTrackValidation.DefaultFlag, value);

    async partial void OnAudioForcedFlagSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("AudioForcedFlag", () => _userSettings.Value.BatchValidation.CustomSettings.AudioTrackValidation.ForcedFlag, value);

    async partial void OnVideoLanguageSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("VideoLanguage", () => _userSettings.Value.BatchValidation.CustomSettings.VideoTrackValidation.Language, value);

    async partial void OnVideoDefaultFlagSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("VideoDefaultFlag", () => _userSettings.Value.BatchValidation.CustomSettings.VideoTrackValidation.DefaultFlag, value);

    async partial void OnSubtitleLanguageSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("SubtitleLanguage", () => _userSettings.Value.BatchValidation.CustomSettings.SubtitleTrackValidation.Language, value);

    async partial void OnSubtitleForcedFlagSeverityIndexChanged(int value)
        => await HandleSeverityChangedAsync("SubtitleForcedFlag", () => _userSettings.Value.BatchValidation.CustomSettings.SubtitleTrackValidation.ForcedFlag, value);

    /// <summary>
    /// Common handler for all validation severity changes. Captures the previous value,
    /// saves settings, and logs if the value actually changed.
    /// </summary>
    private async Task HandleSeverityChangedAsync(string settingName, Func<ValidationSeverity> getPrevious, int newValue)
    {
        var previous = getPrevious();
        await SaveValidationSeverityAsync();
        LogSeverityChangeIfDifferent(settingName, previous, (ValidationSeverity)newValue);
    }

    private void LogSeverityChangeIfDifferent(string settingName, ValidationSeverity previous, ValidationSeverity current)
    {
        if (previous != current)
        {
            LogValidationSeverityChanged(settingName, previous, current);
        }
    }

    /// <summary>
    /// Handles changes to the selected theme.
    /// </summary>
    /// <param name="value">The new theme index value.</param>
    partial void OnSelectedThemeIndexChanged(int value)
    {
        _uiPreferences.AppTheme = (AppThemePreference)value;
    }

    /// <summary>
    /// Handles changes to the selected log level.
    /// </summary>
    /// <param name="value">The new log level index value.</param>
    async partial void OnSelectedLogLevelIndexChanged(int value)
    {
        // Only allow changes if the control is enabled (not overridden by appsettings.json)
        if (!IsLogLevelControlEnabled)
        {
            return;
        }

        var logLevelName = ConvertIndexToLogLevel(value);

        try
        {
            // Update user settings
            await _userSettings.UpdateAsync(settings =>
            {
                settings.UI.LogLevel = logLevelName;
            });

            // Apply the new log level immediately
            if (Enum.TryParse<Serilog.Events.LogEventLevel>(logLevelName, out var level))
            {
                _logLevelService.MinimumLevel = level;
                LogLogLevelChanged(logLevelName);
            }
        }
        catch (Exception ex)
        {
            LogSaveLogLevelFailed(ex);
        }
    }

    /// <summary>
    /// Converts a log level string to a combo box index.
    /// </summary>
    /// <param name="logLevel">The log level string.</param>
    private static int ConvertLogLevelToIndex(string logLevel)
    {
        return logLevel switch
        {
            "Verbose" => 0,
            "Debug" => 1,
            "Information" => 2,
            "Warning" => 3,
            "Error" => 4,
            "Fatal" => 5,
            _ => 2 // Default to Information
        };
    }

    /// <summary>
    /// Converts a combo box index to a log level string.
    /// </summary>
    /// <param name="index">The combo box index.</param>
    private static string ConvertIndexToLogLevel(int index)
    {
        return index switch
        {
            0 => "Verbose",
            1 => "Debug",
            2 => "Information",
            3 => "Warning",
            4 => "Error",
            5 => "Fatal",
            _ => "Information"
        };
    }

    /// <summary>
    /// Loads validation settings from user settings into ViewModel properties.
    /// </summary>
    private void LoadValidationSettings()
    {
        _suppressValidationUpdates = true;

        try
        {
            var settings = _userSettings.Value.BatchValidation;
            SelectedStrictnessModeIndex = (int)settings.Mode;

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
        finally
        {
            _suppressValidationUpdates = false;
        }
    }

    /// <summary>
    /// Updates all validation severity properties from a ValidationSeveritySettings object.
    /// </summary>
    private void UpdateViewModelPropertiesFromSettings(ValidationSeveritySettings settings)
    {
        TrackCountParitySeverityIndex = (int)settings.TrackCountParity;
        AudioLanguageSeverityIndex = (int)settings.AudioTrackValidation.Language;
        AudioDefaultFlagSeverityIndex = (int)settings.AudioTrackValidation.DefaultFlag;
        AudioForcedFlagSeverityIndex = (int)settings.AudioTrackValidation.ForcedFlag;
        VideoLanguageSeverityIndex = (int)settings.VideoTrackValidation.Language;
        VideoDefaultFlagSeverityIndex = (int)settings.VideoTrackValidation.DefaultFlag;
        SubtitleLanguageSeverityIndex = (int)settings.SubtitleTrackValidation.Language;
        SubtitleForcedFlagSeverityIndex = (int)settings.SubtitleTrackValidation.ForcedFlag;
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
        if (_suppressValidationUpdates)
        {
            return;
        }

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

            // Trigger re-validation of the current batch
            _validationStateService.Revalidate();
        }
        catch (Exception ex)
        {
            LogSaveValidationSeverityFailed(ex);
        }
    }
}
