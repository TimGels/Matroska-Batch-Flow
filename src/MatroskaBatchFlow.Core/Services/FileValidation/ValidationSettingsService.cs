using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Service for managing validation settings mode changes and preserving custom settings.
/// </summary>
public sealed class ValidationSettingsService : IValidationSettingsService
{
    /// <summary>
    /// In-memory backup of custom settings when switching to Strict/Lenient mode.
    /// Not persisted to JSON.
    /// </summary>
    private BatchValidationSettings? _customSettingsBackup;

    /// <summary>
    /// Gets the effective validation settings based on the current mode.
    /// For Custom mode, returns the saved custom values.
    /// For Strict/Lenient mode, applies preset in-memory and returns it.
    /// </summary>
    /// <param name="userSettings">The user settings containing the current mode and custom values.</param>
    /// <returns>Effective validation settings ready to use.</returns>
    public BatchValidationSettings GetEffectiveSettings(UserSettings userSettings)
    {
        var mode = userSettings.BatchValidation.Mode;

        // For Custom mode, return a settings object with CustomSettings
        if (mode == StrictnessMode.Custom)
        {
            return userSettings.BatchValidation;
        }

        // For Strict/Lenient mode, apply preset in-memory
        var effectiveSettings = new BatchValidationSettings { Mode = mode };
        ApplyPreset(effectiveSettings.CustomSettings, mode);
        return effectiveSettings;
    }

    /// <summary>
    /// Handles mode switching, backing up or restoring custom settings as needed.
    /// Only the Mode is written to JSON. Custom values remain in JSON, presets are applied in-memory.
    /// </summary>
    /// <param name="userSettings">The user settings to modify.</param>
    /// <param name="newMode">The new strictness mode.</param>
    public void SwitchMode(UserSettings userSettings, StrictnessMode newMode)
    {
        var previousMode = userSettings.BatchValidation.Mode;
        userSettings.BatchValidation.Mode = newMode;

        // If switching from Custom to Strict/Lenient, backup custom values in-memory
        if (previousMode == StrictnessMode.Custom && newMode != StrictnessMode.Custom)
        {
            BackupCustomSettingsToMemory(userSettings.BatchValidation);
            // Don't apply preset to JSON - it will be applied in-memory when displaying
        }
        // If switching to Custom mode, restore custom values if available
        else if (newMode == StrictnessMode.Custom && _customSettingsBackup != null)
        {
            RestoreCustomSettingsFromMemory(userSettings.BatchValidation);
        }
        // If switching between Strict and Lenient, no action needed - presets applied in-memory only
    }

    /// <summary>
    /// Applies a preset (Strict or Lenient) to the validation severity settings.
    /// </summary>
    /// <param name="settings">The validation severity settings to modify.</param>
    /// <param name="mode">The preset mode to apply.</param>
    public void ApplyPreset(ValidationSeveritySettings settings, StrictnessMode mode)
    {
        switch (mode)
        {
            case StrictnessMode.Strict:
                settings.TrackCountParity = ValidationSeverity.Error;
                settings.AudioTrackValidation.Language = ValidationSeverity.Error;
                settings.VideoTrackValidation.Language = ValidationSeverity.Error;
                settings.SubtitleTrackValidation.Language = ValidationSeverity.Error;
                settings.AudioTrackValidation.DefaultFlag = ValidationSeverity.Warning;
                settings.AudioTrackValidation.ForcedFlag = ValidationSeverity.Warning;
                settings.VideoTrackValidation.DefaultFlag = ValidationSeverity.Warning;
                settings.SubtitleTrackValidation.ForcedFlag = ValidationSeverity.Warning;
                break;

            case StrictnessMode.Lenient:
                settings.TrackCountParity = ValidationSeverity.Info;
                settings.AudioTrackValidation.Language = ValidationSeverity.Info;
                settings.VideoTrackValidation.Language = ValidationSeverity.Info;
                settings.SubtitleTrackValidation.Language = ValidationSeverity.Info;
                settings.AudioTrackValidation.DefaultFlag = ValidationSeverity.Info;
                settings.AudioTrackValidation.ForcedFlag = ValidationSeverity.Info;
                settings.VideoTrackValidation.DefaultFlag = ValidationSeverity.Info;
                settings.SubtitleTrackValidation.ForcedFlag = ValidationSeverity.Info;
                break;

            default:
                throw new ArgumentException($"Cannot apply preset for mode: {mode}", nameof(mode));
        }
    }

    /// <summary>
    /// Backs up the current custom settings to in-memory storage before switching to a preset mode.
    /// </summary>
    /// <param name="current">The current validation settings to back up.</param>
    private void BackupCustomSettingsToMemory(BatchValidationSettings current)
    {
        var custom = current.CustomSettings;
        _customSettingsBackup = new BatchValidationSettings
        {
            Mode = StrictnessMode.Custom,
            CustomSettings = new ValidationSeveritySettings
            {
                TrackCountParity = custom.TrackCountParity,
                AudioTrackValidation = new TrackPropertyValidationSettings
                {
                    Language = custom.AudioTrackValidation.Language,
                    DefaultFlag = custom.AudioTrackValidation.DefaultFlag,
                    ForcedFlag = custom.AudioTrackValidation.ForcedFlag
                },
                VideoTrackValidation = new TrackPropertyValidationSettings
                {
                    Language = custom.VideoTrackValidation.Language,
                    DefaultFlag = custom.VideoTrackValidation.DefaultFlag
                },
                SubtitleTrackValidation = new TrackPropertyValidationSettings
                {
                    Language = custom.SubtitleTrackValidation.Language,
                    ForcedFlag = custom.SubtitleTrackValidation.ForcedFlag
                }
            }
        };
    }

    /// <summary>
    /// Restores previously backed up custom settings from in-memory storage.
    /// </summary>
    /// <param name="current">The validation settings to modify.</param>
    private void RestoreCustomSettingsFromMemory(BatchValidationSettings current)
    {
        if (_customSettingsBackup == null)
        {
            return;
        }

        var backup = _customSettingsBackup.CustomSettings;
        var custom = current.CustomSettings;

        custom.TrackCountParity = backup.TrackCountParity;
        custom.AudioTrackValidation.Language = backup.AudioTrackValidation.Language;
        custom.AudioTrackValidation.DefaultFlag = backup.AudioTrackValidation.DefaultFlag;
        custom.AudioTrackValidation.ForcedFlag = backup.AudioTrackValidation.ForcedFlag;
        custom.VideoTrackValidation.Language = backup.VideoTrackValidation.Language;
        custom.VideoTrackValidation.DefaultFlag = backup.VideoTrackValidation.DefaultFlag;
        custom.SubtitleTrackValidation.Language = backup.SubtitleTrackValidation.Language;
        custom.SubtitleTrackValidation.ForcedFlag = backup.SubtitleTrackValidation.ForcedFlag;
    }
}
