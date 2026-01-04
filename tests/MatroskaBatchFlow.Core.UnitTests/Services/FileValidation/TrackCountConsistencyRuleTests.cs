using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

/// <summary>
/// Contains unit tests for the TrackCountConsistencyRule validation rule.
/// </summary>
public class TrackCountConsistencyRuleTests
{
    private readonly TrackCountConsistencyRule _rule = new();

    [Fact]
    public void Validate_ReturnsNoErrors_WhenValidationIsOff()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                TrackCountParity = ValidationSeverity.Off
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithTracks("file1.mkv", audioCount: 2, videoCount: 1, textCount: 3),
            CreateFileWithTracks("file2.mkv", audioCount: 1, videoCount: 1, textCount: 1)  // Mismatch!
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // Should skip validation when Off
    }

    [Fact]
    public void Validate_ReturnsNoErrors_WhenLessThanTwoFiles()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                TrackCountParity = ValidationSeverity.Error
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithTracks("file1.mkv", audioCount: 2, videoCount: 1, textCount: 3)
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);  // No comparison possible with < 2 files
    }

    [Fact]
    public void Validate_ReturnsNoErrors_WhenAllFilesHaveMatchingTrackCounts()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                TrackCountParity = ValidationSeverity.Error
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithTracks("file1.mkv", audioCount: 2, videoCount: 1, textCount: 3),
            CreateFileWithTracks("file2.mkv", audioCount: 2, videoCount: 1, textCount: 3),
            CreateFileWithTracks("file3.mkv", audioCount: 2, videoCount: 1, textCount: 3)
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(ValidationSeverity.Error)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Info)]
    public void Validate_ReturnsErrorWithCorrectSeverity_WhenTrackCountsMismatch(ValidationSeverity severity)
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                TrackCountParity = severity
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithTracks("file1.mkv", audioCount: 2, videoCount: 1, textCount: 3),
            CreateFileWithTracks("file2.mkv", audioCount: 1, videoCount: 1, textCount: 2)  // Audio and Text mismatch
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(severity, results[0].Severity);
        Assert.Equal("file2.mkv", results[0].ValidatedFilePath);
        Assert.Contains("Track count mismatch", results[0].Message);
        Assert.Contains("Audio: 1 (expected 2)", results[0].Message);
        Assert.Contains("Text: 2 (expected 3)", results[0].Message);
    }

    [Fact]
    public void Validate_ReportsMultipleFilesWithMismatches()
    {
        // Arrange
        var settings = new BatchValidationSettings
        {
            CustomSettings = new ValidationSeveritySettings
            {
                TrackCountParity = ValidationSeverity.Error
            }
        };

        var files = new List<ScannedFileInfo>
        {
            CreateFileWithTracks("file1.mkv", audioCount: 2, videoCount: 1, textCount: 3),
            CreateFileWithTracks("file2.mkv", audioCount: 1, videoCount: 1, textCount: 3),  // Audio mismatch
            CreateFileWithTracks("file3.mkv", audioCount: 2, videoCount: 2, textCount: 3),  // Video mismatch
            CreateFileWithTracks("file4.mkv", audioCount: 2, videoCount: 1, textCount: 3)   // OK
        };

        // Act
        var results = _rule.Validate(files, settings).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.ValidatedFilePath == "file2.mkv" && r.Message.Contains("Audio: 1 (expected 2)"));
        Assert.Contains(results, r => r.ValidatedFilePath == "file3.mkv" && r.Message.Contains("Video: 2 (expected 1)"));
    }

    private static ScannedFileInfo CreateFileWithTracks(string path, int audioCount, int videoCount, int textCount)
    {
        var builder = new MediaInfoResultBuilder();

        // Add audio tracks
        for (int i = 0; i < audioCount; i++)
        {
            builder.AddTrackOfType(TrackType.Audio);
        }

        // Add video tracks
        for (int i = 0; i < videoCount; i++)
        {
            builder.AddTrackOfType(TrackType.Video);
        }

        // Add text/subtitle tracks
        for (int i = 0; i < textCount; i++)
        {
            builder.AddTrackOfType(TrackType.Text);
        }

        return new ScannedFileInfo
        {
            Path = path,
            Result = builder.Build()
        };
    }
}
