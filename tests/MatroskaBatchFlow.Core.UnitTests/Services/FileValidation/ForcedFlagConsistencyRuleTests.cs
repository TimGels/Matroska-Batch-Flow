using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

/// <summary>
/// Contains unit tests for the ForcedFlagConsistencyRule validation rule.
/// </summary>
public class ForcedFlagConsistencyRuleTests
{
    private readonly ForcedFlagConsistencyRule _rule = new();

    [Fact]
    public void Validate_WhenAllSeveritiesAreOff_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Off },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Off }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithForcedFlags("file1.mkv", audioForced: true, subtitleForced: true),
            CreateFileWithForcedFlags("file2.mkv", audioForced: false, subtitleForced: false)  // Different!
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // Should skip validation when all Off
    }

    [Fact]
    public void Validate_WhenLessThanTwoFiles_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithForcedFlags("file1.mkv", audioForced: true, subtitleForced: true)
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // No comparison possible with < 2 files
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void Validate_WhenAudioForcedFlagDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithForcedFlags("file1.mkv", audioForced: true, subtitleForced: true),
            CreateFileWithForcedFlags("file2.mkv", audioForced: false, subtitleForced: true)  // Audio different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("Forced flag mismatch", results[0].Message);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void Validate_WhenSubtitleForcedFlagDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                SubtitleTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithForcedFlags("file1.mkv", audioForced: true, subtitleForced: true),
            CreateFileWithForcedFlags("file2.mkv", audioForced: true, subtitleForced: false)  // Subtitle different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("Forced flag mismatch", results[0].Message);
    }

    [Fact]
    public void Validate_WhenAllForcedFlagsMatch_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithForcedFlags("file1.mkv", audioForced: true, subtitleForced: false),
            CreateFileWithForcedFlags("file2.mkv", audioForced: true, subtitleForced: false),
            CreateFileWithForcedFlags("file3.mkv", audioForced: true, subtitleForced: false)
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // All flags match
    }

    [Fact]
    public void Validate_WhenOnlyAudioValidationEnabled_IgnoresOtherTrackTypes()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Off }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithForcedFlags("file1.mkv", audioForced: true, subtitleForced: true),
            CreateFileWithForcedFlags("file2.mkv", audioForced: true, subtitleForced: false)  // Only subtitle different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // Audio matches, so no errors
    }

    [Fact]
    public void Validate_WhenMultipleTracksOfSameType_ValidatesAllPositions()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(true)  // First audio track
            .Build());
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(false)  // Second audio track
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(true)  // First audio track matches
            .Build());
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(true)  // Second audio track differs!
            .Build());

        var files = new List<ScannedFileInfo>
        {
            new(builder1.Build(), "file1.mkv"),
            new(builder2.Build(), "file2.mkv")
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains("position 2", results[0].Message);  // Error at second track
    }

    [Fact]
    public void Validate_WhenTrackCountsDiffer_SkipsValidation()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(true)
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(false)
            .Build());
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(true)
            .Build());

        var files = new List<ScannedFileInfo>
        {
            new(builder1.Build(), "file1.mkv"),  // 1 audio track
            new(builder2.Build(), "file2.mkv")   // 2 audio tracks
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // Different track counts, validation skipped
    }

    [Fact]
    public void Validate_DoesNotValidateVideoTrackForcedFlag()
    {
        // Arrange - Video tracks should not have forced flag validation
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                VideoTrackValidation = new TrackPropertyValidationSettings { ForcedFlag = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Video)
            .WithForced(true)
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Video)
            .WithForced(false)  // Different forced flag
            .Build());

        var files = new List<ScannedFileInfo>
        {
            new(builder1.Build(), "file1.mkv"),
            new(builder2.Build(), "file2.mkv")
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // Video tracks are not validated for forced flag
    }

    private static ScannedFileInfo CreateFileWithForcedFlags(string path, bool audioForced, bool subtitleForced)
    {
        var builder = new MediaInfoResultBuilder();
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithForced(audioForced)
            .Build());
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Text)
            .WithForced(subtitleForced)
            .Build());

        return new ScannedFileInfo(builder.Build(), path);
    }
}
