using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

/// <summary>
/// Contains unit tests for the BatchValidationSettings model.
/// </summary>
public class BatchValidationSettingsTests
{
    [Fact]
    public void BatchValidationSettings_HasCorrectDefaultValues()
    {
        // Act
        var settings = new BatchValidationSettings();

        // Assert
        Assert.Equal(StrictnessMode.Strict, settings.Mode);
        Assert.NotNull(settings.CustomSettings);
        Assert.Equal(ValidationSeverity.Error, settings.CustomSettings.TrackCountParity);
        Assert.NotNull(settings.CustomSettings.AudioTrackValidation);
        Assert.NotNull(settings.CustomSettings.VideoTrackValidation);
        Assert.NotNull(settings.CustomSettings.SubtitleTrackValidation);
    }

    [Fact]
    public void BatchValidationSettings_ModeCanBeSet()
    {
        // Arrange
        var settings = new BatchValidationSettings();

        // Act
        settings.Mode = StrictnessMode.Lenient;

        // Assert
        Assert.Equal(StrictnessMode.Lenient, settings.Mode);
    }

    [Fact]
    public void BatchValidationSettings_TrackCountParityCanBeSet()
    {
        // Arrange
        var settings = new BatchValidationSettings();

        // Act
        settings.CustomSettings.TrackCountParity = ValidationSeverity.Warning;

        // Assert
        Assert.Equal(ValidationSeverity.Warning, settings.CustomSettings.TrackCountParity);
    }

    [Fact]
    public void BatchValidationSettings_TrackValidationSettingsCanBeModified()
    {
        // Arrange
        var settings = new BatchValidationSettings();

        // Act
        settings.CustomSettings.AudioTrackValidation.Language = ValidationSeverity.Warning;
        settings.CustomSettings.VideoTrackValidation.DefaultFlag = ValidationSeverity.Info;
        settings.CustomSettings.SubtitleTrackValidation.ForcedFlag = ValidationSeverity.Off;

        // Assert
        Assert.Equal(ValidationSeverity.Warning, settings.CustomSettings.AudioTrackValidation.Language);
        Assert.Equal(ValidationSeverity.Info, settings.CustomSettings.VideoTrackValidation.DefaultFlag);
        Assert.Equal(ValidationSeverity.Off, settings.CustomSettings.SubtitleTrackValidation.ForcedFlag);
    }
}
