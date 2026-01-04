using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Service for managing validation settings mode changes and preserving custom settings.
/// </summary>
public interface IValidationSettingsService
{
    /// <summary>
    /// Gets the effective validation settings based on the current mode.
    /// For Custom mode, returns the saved custom values.
    /// For Strict/Lenient mode, applies preset in-memory and returns it.
    /// </summary>
    /// <param name="userSettings">The user settings containing the current mode and custom values.</param>
    /// <returns>Effective validation settings ready to use.</returns>
    BatchValidationSettings GetEffectiveSettings(UserSettings userSettings);

    /// <summary>
    /// Handles mode switching, backing up or restoring custom settings as needed.
    /// </summary>
    /// <param name="userSettings">The user settings to modify.</param>
    /// <param name="newMode">The new strictness mode.</param>
    void SwitchMode(UserSettings userSettings, StrictnessMode newMode);

    /// <summary>
    /// Applies a preset (Strict or Lenient) to the validation settings.
    /// </summary>
    /// <param name="settings">The validation severity settings to modify.</param>
    /// <param name="mode">The preset mode to apply.</param>
    void ApplyPreset(ValidationSeveritySettings settings, StrictnessMode mode);
}
