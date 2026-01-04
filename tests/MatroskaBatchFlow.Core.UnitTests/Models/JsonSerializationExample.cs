using System.Text.Json;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

/// <summary>
/// Demonstrates the JSON serialization structure of UserSettings with nested CustomSettings.
/// </summary>
public class JsonSerializationExample
{
    [Fact]
    public void DemonstrateCustomModeJsonStructure()
    {
        // Arrange
        var settings = new UserSettings
        {
            BatchValidation = new BatchValidationSettings
            {
                Mode = StrictnessMode.Custom,
                CustomSettings = new ValidationSeveritySettings
                {
                    TrackCountParity = ValidationSeverity.Warning,
                    AudioTrackValidation = new TrackPropertyValidationSettings
                    {
                        Language = ValidationSeverity.Error,
                        DefaultFlag = ValidationSeverity.Info,
                        ForcedFlag = ValidationSeverity.Warning
                    },
                    VideoTrackValidation = new TrackPropertyValidationSettings
                    {
                        Language = ValidationSeverity.Off,
                        DefaultFlag = ValidationSeverity.Warning
                    },
                    SubtitleTrackValidation = new TrackPropertyValidationSettings
                    {
                        Language = ValidationSeverity.Error,
                        ForcedFlag = ValidationSeverity.Info
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        // Assert - the JSON structure shows CustomSettings nested under BatchValidation
        Assert.Contains("\"Mode\": 2", json); // StrictnessMode.Custom = 2
        Assert.Contains("\"CustomSettings\"", json);
        Assert.Contains("\"TrackCountParity\": 2", json); // ValidationSeverity.Warning = 2
        
        // This demonstrates that the JSON structure is now:
        // {
        //   "BatchValidation": {
        //     "Mode": 2,
        //     "CustomSettings": {
        //       "TrackCountParity": 2,
        //       "AudioTrackValidation": { ... },
        //       ...
        //     }
        //   }
        // }
    }

    [Fact]
    public void DemonstrateStrictModeJsonStructure()
    {
        // Arrange
        var settings = new UserSettings
        {
            BatchValidation = new BatchValidationSettings
            {
                Mode = StrictnessMode.Strict,
                CustomSettings = new ValidationSeveritySettings
                {
                    TrackCountParity = ValidationSeverity.Warning,
                    AudioTrackValidation = new TrackPropertyValidationSettings
                    {
                        Language = ValidationSeverity.Error,
                        DefaultFlag = ValidationSeverity.Info
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        // Assert - CustomSettings are preserved in JSON even when mode is not Custom
        Assert.Contains("\"Mode\": 0", json); // StrictnessMode.Strict = 0
        Assert.Contains("\"CustomSettings\"", json); // CustomSettings preserved in JSON even when not active
        
        // This makes it clear to users that CustomSettings are for Custom mode
        // When in Strict/Lenient mode, the app uses presets defined in ValidationSettingsService
    }
}
