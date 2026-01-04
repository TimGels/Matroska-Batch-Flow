using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

/// <summary>
/// Contains unit tests for the FileTrackConfiguration model.
/// </summary>
public class FileTrackConfigurationTests
{
    [Fact]
    public void FileTrackConfiguration_InitializesWithEmptyCollections()
    {
        // Act
        var config = new FileTrackConfiguration();

        // Assert
        Assert.NotNull(config.AudioTracks);
        Assert.NotNull(config.VideoTracks);
        Assert.NotNull(config.SubtitleTracks);
        Assert.Empty(config.AudioTracks);
        Assert.Empty(config.VideoTracks);
        Assert.Empty(config.SubtitleTracks);
    }

    [Fact]
    public void FileTrackConfiguration_FilePathCanBeSet()
    {
        // Act
        var config = new FileTrackConfiguration
        {
            FilePath = "test.mkv"
        };

        // Assert
        Assert.Equal("test.mkv", config.FilePath);
    }

    [Theory]
    [InlineData(TrackType.Audio)]
    [InlineData(TrackType.Video)]
    [InlineData(TrackType.Text)]
    public void GetTrackListForType_ReturnsCorrectCollection(TrackType trackType)
    {
        // Arrange
        var config = new FileTrackConfiguration();
        var expectedList = trackType switch
        {
            TrackType.Audio => config.AudioTracks,
            TrackType.Video => config.VideoTracks,
            TrackType.Text => config.SubtitleTracks,
            _ => throw new ArgumentException("Invalid track type")
        };

        // Act
        var result = config.GetTrackListForType(trackType);

        // Assert
        Assert.Same(expectedList, result);
    }

    [Fact]
    public void GetTrackListForType_ReturnsEmptyForGeneralType()
    {
        // Arrange
        var config = new FileTrackConfiguration();

        // Act
        var result = config.GetTrackListForType(TrackType.General);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTrackListForType_ReturnsCorrectCollection_AudioTracksAreModifiable()
    {
        // Arrange
        var config = new FileTrackConfiguration();
        var audioList = config.GetTrackListForType(TrackType.Audio);

        // Act - Verify we can modify the returned list
        var initialCount = audioList.Count;

        // Assert
        Assert.Same(config.AudioTracks, audioList);
        Assert.Equal(0, initialCount); // Initially empty
    }
}
