using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

/// <summary>
/// Contains unit tests for the FileTrackAvailabilityRecorder class, verifying correct recording of
/// track availability information for scanned files.
/// </summary>
public class FileTrackAvailabilityRecorderTests
{
    private static (IBatchConfiguration mockConfig, Dictionary<string, FileTrackAvailability> fileTrackMap) CreateMockConfig()
    {
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var fileTrackMap = new Dictionary<string, FileTrackAvailability>();
        mockConfig.FileTrackMap.Returns(fileTrackMap);
        return (mockConfig, fileTrackMap);
    }

    [Fact]
    public void RecordAvailability_RecordsTrackCounts()
    {
        // Arrange
        var (mockConfig, fileTrackMap) = CreateMockConfig();
        var recorder = new FileTrackAvailabilityRecorder(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Text)
            .AddTrackOfType(TrackType.Text)
            .AddTrackOfType(TrackType.Text)
            .Build();
        var scannedFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        var result = recorder.RecordAvailability(scannedFile);

        // Assert
        Assert.Equal("file.mkv", result.FilePath);
        Assert.Equal(2, result.AudioTrackCount);
        Assert.Equal(1, result.VideoTrackCount);
        Assert.Equal(3, result.SubtitleTrackCount);

        // Verify it was added to the batch config
        Assert.True(fileTrackMap.ContainsKey("file.mkv"));
        Assert.Same(result, fileTrackMap["file.mkv"]);
    }

    [Fact]
    public void RecordAvailability_HandlesNoTracks()
    {
        // Arrange
        var (mockConfig, fileTrackMap) = CreateMockConfig();
        var recorder = new FileTrackAvailabilityRecorder(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .Build();
        var scannedFile = new ScannedFileInfo { Path = "empty.mkv", Result = mediaInfoResult };

        // Act
        var result = recorder.RecordAvailability(scannedFile);

        // Assert
        Assert.Equal("empty.mkv", result.FilePath);
        Assert.Equal(0, result.AudioTrackCount);
        Assert.Equal(0, result.VideoTrackCount);
        Assert.Equal(0, result.SubtitleTrackCount);
    }

    [Fact]
    public void RecordAvailability_HandlesNullResult()
    {
        // Arrange
        var (mockConfig, fileTrackMap) = CreateMockConfig();
        var recorder = new FileTrackAvailabilityRecorder(mockConfig);

        var scannedFile = new ScannedFileInfo { Path = "null-result.mkv", Result = null! };

        // Act
        var result = recorder.RecordAvailability(scannedFile);

        // Assert - Should return availability with zero counts
        Assert.Equal("null-result.mkv", result.FilePath);
        Assert.Equal(0, result.AudioTrackCount);
        Assert.Equal(0, result.VideoTrackCount);
        Assert.Equal(0, result.SubtitleTrackCount);
    }

    [Fact]
    public void RecordAvailability_OverwritesExistingEntry()
    {
        // Arrange
        var (mockConfig, fileTrackMap) = CreateMockConfig();
        var recorder = new FileTrackAvailabilityRecorder(mockConfig);

        var firstResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var firstFile = new ScannedFileInfo { Path = "file.mkv", Result = firstResult };
        recorder.RecordAvailability(firstFile);

        var secondResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var secondFile = new ScannedFileInfo { Path = "file.mkv", Result = secondResult };

        // Act
        var result = recorder.RecordAvailability(secondFile);

        // Assert - Should have updated counts from second scan
        Assert.Equal(3, result.AudioTrackCount);
        Assert.Equal(3, fileTrackMap["file.mkv"].AudioTrackCount);
    }

    [Fact]
    public void RecordAvailability_ThrowsForNullScannedFile()
    {
        // Arrange
        var (mockConfig, _) = CreateMockConfig();
        var recorder = new FileTrackAvailabilityRecorder(mockConfig);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => recorder.RecordAvailability(null!));
    }
}
