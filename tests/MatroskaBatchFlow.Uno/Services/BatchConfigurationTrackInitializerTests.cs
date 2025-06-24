using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Tests.Builders;
using Moq;

namespace MatroskaBatchFlow.Uno.Tests.Services;

public class BatchConfigurationTrackInitializerTests
{
    [Fact]
    public void EnsureTrackCount_WhenReferenceFileHasMoreTracks_ShouldAddTracks()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>();
        var videoTracks = new List<TrackConfiguration>();

        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Create a reference file with 2 audio tracks and 1 video track
        var referenceFile = CreateTestFile(audioCount: 2, videoCount: 1);

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

        // Assert
        Assert.Equal(2, audioTracks.Count);
        Assert.Single(videoTracks);
        Assert.All(audioTracks, track => Assert.Equal(TrackType.Audio, track.TrackType));
        Assert.All(videoTracks, track => Assert.Equal(TrackType.Video, track.TrackType));
    }

    [Fact]
    public void EnsureTrackCount_WhenReferenceFileHasFewerTracks_ShouldRemoveTracks()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration { TrackType = TrackType.Audio },
            new TrackConfiguration { TrackType = TrackType.Audio },
            new TrackConfiguration { TrackType = TrackType.Audio }
        };
        var videoTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration { TrackType = TrackType.Video },
            new TrackConfiguration { TrackType = TrackType.Video }
        };

        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Create a reference file with 1 audio track and 0 video tracks
        var referenceFile = CreateTestFile(audioCount: 1, videoCount: 0);

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

        // Assert
        Assert.Single(audioTracks);
        Assert.Empty(videoTracks);
    }

    [Fact]
    public void EnsureTrackCount_WhenTrackCountsMatch_ShouldNotModifyTracks()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration { TrackType = TrackType.Audio }
        };
        var videoTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration { TrackType = TrackType.Video },
            new TrackConfiguration { TrackType = TrackType.Video }
        };

        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Create a reference file with matching track counts
        var referenceFile = CreateTestFile(audioCount: 1, videoCount: 2);

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

        // Assert
        Assert.Single(audioTracks);
        Assert.Equal(2, videoTracks.Count);
    }

    [Fact]
    public void EnsureTrackCount_WhenReferenceFileIsNull_ShouldNotModifyTracks()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new TrackConfiguration { TrackType = TrackType.Audio } };

        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Act - Pass null reference file
        initializer.EnsureTrackCount(null, TrackType.Audio);

        // Assert - Tracks should remain unchanged
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never());
    }

    [Fact]
    public void EnsureTrackCount_WhenNoTrackTypesSpecified_ShouldNotModifyTracks()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new TrackConfiguration { TrackType = TrackType.Audio } };

        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);
        var referenceFile = CreateTestFile(audioCount: 2);

        // Act - Pass empty track types array
        initializer.EnsureTrackCount(referenceFile, new TrackType[0]);

        // Assert - Tracks should remain unchanged
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never());
    }

    private ScannedFileInfo CreateTestFile(int audioCount = 0, int videoCount = 0, int subtitleCount = 0)
    {
        var mediaInfoBuilder = new MediaInfoResultBuilder()
            .WithCreatingLibrary("TestLib", "1.0", string.Empty)
            .WithMediaReference("TestRef");

        // Add audio tracks
        for (int i = 0; i < audioCount; i++)
        {
            var trackBuilder = new TrackInfoBuilder()
                .WithType(TrackType.Audio)
                .WithStreamKindID(i);

            mediaInfoBuilder.AddTrack(trackBuilder.Build());
        }

        // Add video tracks
        for (int i = 0; i < videoCount; i++)
        {
            var trackBuilder = new TrackInfoBuilder()
                .WithType(TrackType.Video)
                .WithStreamKindID(i);

            mediaInfoBuilder.AddTrack(trackBuilder.Build());
        }

        // Add subtitle tracks
        for (int i = 0; i < subtitleCount; i++)
        {
            var trackBuilder = new TrackInfoBuilder()
                .WithType(TrackType.Text)
                .WithStreamKindID(i);

            mediaInfoBuilder.AddTrack(trackBuilder.Build());
        }

        return new ScannedFileInfo
        {
            Path = "test_file.mkv",
            Result = mediaInfoBuilder.Build()
        };
    }
}
