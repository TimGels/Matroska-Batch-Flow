using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Tests.Builders;
using Moq;

namespace MatroskaBatchFlow.Core.Tests.Services;

public class BatchConfigurationTrackInitializerTests
{
    [Fact]
    public void EnsureTrackCount_ReferenceHasMoreTracks_AddsTracksToBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>();
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Build reference file with 3 audio tracks
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        for (int i = 0; i < 3; i++)
            builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(i).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Equal(3, audioTracks.Count);
        Assert.All(audioTracks, t => Assert.Equal(TrackType.Audio, t.TrackType));
    }

    [Fact]
    public void EnsureTrackCount_ReferenceHasFewerTracks_RemovesTracksFromBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>
        {
            new() { TrackType = TrackType.Audio },
            new() { TrackType = TrackType.Audio },
            new() { TrackType = TrackType.Audio }
        };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Build reference file with 1 audio track
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        Assert.Equal(TrackType.Audio, audioTracks[0].TrackType);
    }

    [Fact]
    public void EnsureTrackCount_ReferenceAndBatchConfigHaveSameCount_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>
        {
            new() { TrackType = TrackType.Audio },
            new() { TrackType = TrackType.Audio }
        };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Build reference file with 2 audio tracks
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        for (int i = 0; i < 2; i++)
            builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(i).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Equal(2, audioTracks.Count);
    }

    [Fact]
    public void EnsureTrackCount_ReferenceFileIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Act
        initializer.EnsureTrackCount(null!, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
    }

    [Fact]
    public void EnsureTrackCount_NoTrackTypesSpecified_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
    }

    [Fact]
    public void EnsureTrackCount_MultipleTrackTypes_SynchronizesEachTypeIndependently()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        var videoTracks = new List<TrackConfiguration>();
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Reference file: 2 audio, 1 video
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());
        builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(1).Build());
        builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Video).WithStreamKindID(0).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

        // Assert
        Assert.Equal(2, audioTracks.Count);
        Assert.Single(videoTracks);
        Assert.All(audioTracks, t => Assert.Equal(TrackType.Audio, t.TrackType));
        Assert.All(videoTracks, t => Assert.Equal(TrackType.Video, t.TrackType));
    }

    [Fact]
    public void EnsureTrackCount_ReferenceFileHasZeroTracks_ClearsBatchConfigList()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>
        {
            new() { TrackType = TrackType.Audio },
            new() { TrackType = TrackType.Audio }
        };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        // Reference file: 0 audio tracks
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Empty(audioTracks);
    }

    [Theory]
    [InlineData(0, 2, 2)] // 0 in config, 2 in reference, expect 2 after
    [InlineData(3, 1, 1)] // 3 in config, 1 in reference, expect 1 after
    [InlineData(2, 2, 2)] // 2 in config, 2 in reference, expect 2 after
    public void EnsureTrackCount_VariousAudioCounts_ResultsAsExpected(int initialCount, int referenceCount, int expectedCount)
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = Enumerable.Range(0, initialCount)
            .Select(_ => new TrackConfiguration { TrackType = TrackType.Audio }).ToList();
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        for (int i = 0; i < referenceCount; i++)
            builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(i).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Equal(expectedCount, audioTracks.Count);
    }

    [Fact]
    public void EnsureTrackCount_ReferenceFileResultIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = null! };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
    }

    [Fact]
    public void EnsureTrackCount_ReferenceFileMediaIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        var result = new MediaInfoResult(
            new MediaInfoResult.CreatingLibraryInfo("", "", ""),
            null! // Media is null
        );
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = result };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
    }

    [Fact]
    public void EnsureTrackCount_ReferenceFileMediaTrackIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        var media = new MediaInfoResult.MediaInfo("ref", null!); // Track is null
        var result = new MediaInfoResult(
            new MediaInfoResult.CreatingLibraryInfo("", "", ""),
            media
        );
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = result };

        // Act
        initializer.EnsureTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
    }

    [Fact]
    public void EnsureTrackCount_TrackTypesIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = new Mock<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration> { new() { TrackType = TrackType.Audio } };
        mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);

        var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = builder.Build() };

        // Act
        initializer.EnsureTrackCount(referenceFile, null!);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
    }
}
