using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

/// <summary>
/// Contains unit tests for the FileTrackAvailability model.
/// </summary>
public class FileTrackAvailabilityTests
{
    [Fact]
    public void FileTrackAvailability_InitializesWithZeroTracks()
    {
        // Act
        var availability = new FileTrackAvailability();

        // Assert
        Assert.Equal(0, availability.AudioTrackCount);
        Assert.Equal(0, availability.VideoTrackCount);
        Assert.Equal(0, availability.SubtitleTrackCount);
    }

    [Fact]
    public void FileTrackAvailability_TrackCountsCanBeSet()
    {
        // Act
        var availability = new FileTrackAvailability
        {
            FilePath = "test.mkv",
            AudioTrackCount = 2,
            VideoTrackCount = 1,
            SubtitleTrackCount = 3
        };

        // Assert
        Assert.Equal("test.mkv", availability.FilePath);
        Assert.Equal(2, availability.AudioTrackCount);
        Assert.Equal(1, availability.VideoTrackCount);
        Assert.Equal(3, availability.SubtitleTrackCount);
    }

    [Theory]
    [InlineData(TrackType.Audio, 0, true)]   // Valid: 0 < 2
    [InlineData(TrackType.Audio, 1, true)]   // Valid: 1 < 2
    [InlineData(TrackType.Audio, 2, false)]  // Invalid: 2 < 2 is false
    [InlineData(TrackType.Video, 0, true)]   // Valid: 0 < 1
    [InlineData(TrackType.Video, 1, false)]  // Invalid: 1 < 1 is false
    [InlineData(TrackType.Text, 0, true)]    // Valid: 0 < 3
    [InlineData(TrackType.Text, 2, true)]    // Valid: 2 < 3
    [InlineData(TrackType.Text, 3, false)]   // Invalid: 3 < 3 is false
    public void HasTrack_ReturnsCorrectAvailability(TrackType trackType, int index, bool expected)
    {
        // Arrange
        var availability = new FileTrackAvailability
        {
            AudioTrackCount = 2,
            VideoTrackCount = 1,
            SubtitleTrackCount = 3
        };

        // Act
        var result = availability.HasTrack(trackType, index);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HasTrack_NegativeIndexReturnsTrue_DueToComparison()
    {
        // Arrange
        var availability = new FileTrackAvailability
        {
            AudioTrackCount = 2
        };

        // Act - Negative index < count is always true
        var result = availability.HasTrack(TrackType.Audio, -1);

        // Assert - Current implementation doesn't guard against negative indices
        Assert.True(result);
    }

    [Fact]
    public void HasTrack_HandlesGeneralTrackType()
    {
        // Arrange
        var availability = new FileTrackAvailability();

        // Act
        var result = availability.HasTrack(TrackType.General, 0);

        // Assert
        Assert.False(result);
    }
}
