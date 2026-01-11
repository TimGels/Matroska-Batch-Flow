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
    private static (IBatchConfiguration mockConfig, Dictionary<Guid, FileTrackAvailability> fileTrackMap) CreateMockConfig()
    {
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var fileTrackMap = new Dictionary<Guid, FileTrackAvailability>();
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
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");

        // Act
        var result = recorder.RecordAvailability(scannedFile);

        // Assert
        Assert.Equal("file.mkv", System.IO.Path.GetFileName(result.FilePath));
        Assert.Equal(2, result.AudioTrackCount);
        Assert.Equal(1, result.VideoTrackCount);
        Assert.Equal(3, result.SubtitleTrackCount);

        // Verify it was added to the batch config
        Assert.True(fileTrackMap.ContainsKey(scannedFile.Id));
        Assert.Same(result, fileTrackMap[scannedFile.Id]);
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
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "empty.mkv");

        // Act
        var result = recorder.RecordAvailability(scannedFile);

        // Assert
        Assert.Equal("empty.mkv", System.IO.Path.GetFileName(result.FilePath));
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

        var scannedFile = new ScannedFileInfo(null!, "null-result.mkv");

        // Act
        var result = recorder.RecordAvailability(scannedFile);

        // Assert - Should return availability with zero counts
        Assert.Equal("null-result.mkv", Path.GetFileName(result.FilePath));
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
        var firstFile = new ScannedFileInfo(firstResult, "file.mkv");
        recorder.RecordAvailability(firstFile);

        var secondResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var secondFile = new ScannedFileInfo(secondResult, "file.mkv");

        // Act
        var result = recorder.RecordAvailability(secondFile);

        // Assert - Should have updated counts from second scan
        Assert.Equal(3, result.AudioTrackCount);
        Assert.Equal(3, fileTrackMap[secondFile.Id].AudioTrackCount);
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
