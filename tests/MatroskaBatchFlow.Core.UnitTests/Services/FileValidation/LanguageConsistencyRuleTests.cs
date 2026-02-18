using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

/// <summary>
/// Contains unit tests for the LanguageConsistencyRule validation rule.
/// </summary>
public class LanguageConsistencyRuleTests
{
    private readonly LanguageConsistencyRule _rule = new();

    [Fact]
    public void Validate_WhenAllSeveritiesAreOff_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Off },
                VideoTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Off },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Off }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file2.mkv", audioLang: "jpn", videoLang: "fra", subtitleLang: "ger")  // Different!
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
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng")
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
    public void Validate_WhenAudioLanguageDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file2.mkv", audioLang: "jpn", videoLang: "eng", subtitleLang: "eng")  // Audio different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("audio", results[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("language", results[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void Validate_WhenVideoLanguageDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                VideoTrackValidation = new TrackPropertyValidationSettings { Language = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file2.mkv", audioLang: "eng", videoLang: "fra", subtitleLang: "eng")  // Video different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("video", results[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("language", results[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void Validate_WhenSubtitleLanguageDiffers_ReturnsResultWithConfiguredSeverity(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                SubtitleTrackValidation = new TrackPropertyValidationSettings { Language = severity }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file2.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "ger")  // Subtitle different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Contains("text", results[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("language", results[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WhenAllLanguagesMatch_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error },
                VideoTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file2.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file3.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng")
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WhenOnlyAudioLanguageCheckIsEnabled_IgnoresOtherMismatches()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error },
                VideoTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Off },
                SubtitleTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Off }
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithLanguages("file1.mkv", audioLang: "eng", videoLang: "eng", subtitleLang: "eng"),
            CreateFileWithLanguages("file2.mkv", audioLang: "eng", videoLang: "fra", subtitleLang: "ger")  // Only video/subtitle different
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // Audio matches, so no errors
    }

    [Fact]
    public void Validate_WhenTrackCountsDiffer_ValidatesOverlappingPositions()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("eng")
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("jpn")  // Differs from reference at overlapping position
            .Build());
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("fra")
            .Build());

        var files = new List<ScannedFileInfo>
        {
            new(builder1.Build(), "file1.mkv"),  // 1 audio track
            new(builder2.Build(), "file2.mkv")   // 2 audio tracks
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert: Overlapping position validated, mismatch reported
        Assert.Single(results);
        Assert.Contains("position 1", results[0].Message);
    }

    [Fact]
    public void Validate_WhenTrackCountsDifferButOverlappingMatch_ReturnsNoErrors()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                AudioTrackValidation = new TrackPropertyValidationSettings { Language = ValidationSeverity.Error }
            }
        };

        var builder1 = new MediaInfoResultBuilder();
        builder1.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("eng")
            .Build());

        var builder2 = new MediaInfoResultBuilder();
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("eng")  // Matches reference at overlapping position
            .Build());
        builder2.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("jpn")
            .Build());

        var files = new List<ScannedFileInfo>
        {
            new(builder1.Build(), "file1.mkv"),  // 1 audio track
            new(builder2.Build(), "file2.mkv")   // 2 audio tracks
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert: Overlapping position matches, no error
        Assert.Empty(results);
    }

    private static ScannedFileInfo CreateFileWithLanguages(string path, string audioLang, string videoLang, string subtitleLang)
    {
        var builder = new MediaInfoResultBuilder();
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage(audioLang)
            .Build());
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Video)
            .WithLanguage(videoLang)
            .Build());
        
        builder.AddTrack(new TrackInfoBuilder()
            .WithType(TrackType.Text)
            .WithLanguage(subtitleLang)
            .Build());

        return new ScannedFileInfo(builder.Build(), path);
    }
}
