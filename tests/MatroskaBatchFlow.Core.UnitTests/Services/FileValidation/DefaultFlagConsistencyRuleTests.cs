using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

/// <summary>
/// Contains unit tests for the DefaultFlagConsistencyRule validation rule.
/// </summary>
public class DefaultFlagConsistencyRuleTests
{
    private readonly DefaultFlagConsistencyRule _rule = new();

    [Fact]
    public void Validate_WhenAllSeveritiesAreOff_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Off },
                VideoTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Off },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Off }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: true),
            CreateFileWithDefaultFlags("file2.mkv", audioDefault: false, videoDefault: false, subtitleDefault: false)  // Different!
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
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: true)
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
    public void Validate_WhenAudioDefaultFlagDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: true),
            CreateFileWithDefaultFlags("file2.mkv", audioDefault: false, videoDefault: true, subtitleDefault: true)  // Audio different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("Default flag mismatch", results[0].Message);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void Validate_WhenVideoDefaultFlagDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                VideoTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: true),
            CreateFileWithDefaultFlags("file2.mkv", audioDefault: true, videoDefault: false, subtitleDefault: true)  // Video different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("Default flag mismatch", results[0].Message);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void Validate_WhenSubtitleDefaultFlagDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                SubtitleTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: true),
            CreateFileWithDefaultFlags("file2.mkv", audioDefault: true, videoDefault: true, subtitleDefault: false)  // Subtitle different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("Default flag mismatch", results[0].Message);
    }

    [Fact]
    public void Validate_WhenAllDefaultFlagsMatch_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error },
                VideoTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: false),
            CreateFileWithDefaultFlags("file2.mkv", audioDefault: true, videoDefault: true, subtitleDefault: false),
            CreateFileWithDefaultFlags("file3.mkv", audioDefault: true, videoDefault: true, subtitleDefault: false)
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
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error },
                VideoTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Off },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Off }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithDefaultFlags("file1.mkv", audioDefault: true, videoDefault: true, subtitleDefault: true),
            CreateFileWithDefaultFlags("file2.mkv", audioDefault: true, videoDefault: false, subtitleDefault: false)  // Only video/subtitle different
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
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(true)  // First audio track
            .Build());
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(false)  // Second audio track
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(true)  // First audio track matches
            .Build());
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(true)  // Second audio track differs!
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
                AudioTrackValidation = new TrackPropertyValidationSettings { DefaultFlag = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(true)
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(false)
            .Build());
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(true)
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

    private static ScannedFileInfo CreateFileWithDefaultFlags(string path, bool audioDefault, bool videoDefault, bool subtitleDefault)
    {
        var builder = new MediaInfoResultBuilder();
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithDefault(audioDefault)
            .Build());
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Video)
            .WithDefault(videoDefault)
            .Build());
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Text)
            .WithDefault(subtitleDefault)
            .Build());

        return new ScannedFileInfo(builder.Build(), path);
    }
}
