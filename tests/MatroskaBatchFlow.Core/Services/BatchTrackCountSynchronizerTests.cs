using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Tests.Builders;
using NSubstitute;

namespace MatroskaBatchFlow.Core.Tests.Services;

/// <summary>
/// Contains unit tests for the BatchTrackCountSynchronizer class, verifying correct synchronization of track counts
/// between batch configuration and reference media files.
/// </summary>
/// <remarks>These tests cover scenarios such as adding or removing tracks to match a reference file, handling
/// multiple track types, and ensuring no changes occur when reference data is missing or invalid. The tests help ensure
/// that BatchTrackCountSynchronizer behaves as expected under various conditions.</remarks>
public class BatchTrackCountSynchronizerTests
{
    [Fact]
    public void SynchronizeTrackCount_AddsTracksWhenBatchConfigHasFewer()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>();
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Equal(3, audioTracks.Count);
        Assert.All(audioTracks, track => Assert.Equal(TrackType.Audio, track.Type));
    }

    [Fact]
    public void SynchronizeTrackCount_RemovesTracksWhenBatchConfigHasMore()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Video);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Video);
        
        var videoTracks = new List<TrackConfiguration>
        {
            new(trackInfo) { Type = TrackType.Video },
            new(trackInfo) { Type = TrackType.Video },
            new(trackInfo) { Type = TrackType.Video }
        };
        mockConfig.GetTrackListForType(TrackType.Video).Returns(videoTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Video);

        // Assert
        Assert.Single(videoTracks);
    }

    [Fact]
    public void SynchronizeTrackCount_HandlesMultipleTrackTypes()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var audioTracks = new List<TrackConfiguration>();
        var videoTracks = new List<TrackConfiguration>();
        
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);
        mockConfig.GetTrackListForType(TrackType.Video).Returns(videoTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Video)
            .Build();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

        // Assert
        Assert.Equal(2, audioTracks.Count);
        Assert.Single(videoTracks);
    }

    [Fact]
    public void SynchronizeTrackCount_MaintainsCorrectCountWhenMatching()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Text);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Text);
        
        var textTracks = new List<TrackConfiguration>
        {
            new(trackInfo) { Type = TrackType.Text },
            new(trackInfo) { Type = TrackType.Text }
        };
        mockConfig.GetTrackListForType(TrackType.Text).Returns(textTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .AddTrackOfType(TrackType.Text)
            .Build();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Text);

        // Assert
        Assert.Equal(2, textTracks.Count);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(3, 2)]
    public void SynchronizeTrackCount_AdjustsToCorrectCount(int initialCount, int referenceCount)
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Audio);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Audio);
        
        var audioTracks = new List<TrackConfiguration>();
        for (int i = 0; i < initialCount; i++)
            audioTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Audio });
        
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var resultBuilder = new MediaInfoResultBuilder().WithCreatingLibrary();
        for (int i = 0; i < referenceCount; i++)
            resultBuilder.AddTrackOfType(TrackType.Audio);
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = resultBuilder.Build() };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Equal(referenceCount, audioTracks.Count);
    }

    [Fact]
    public void SynchronizeTrackCount_ReferenceFileResultIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Audio);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Audio);
        
        var audioTracks = new List<TrackConfiguration> { new(trackInfo) { Type = TrackType.Audio } };
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = null! };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.DidNotReceive().GetTrackListForType(Arg.Any<TrackType>());
    }

    [Fact]
    public void SynchronizeTrackCount_ReferenceFileMediaIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Audio);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Audio);
        
        var audioTracks = new List<TrackConfiguration> { new(trackInfo) { Type = TrackType.Audio } };
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var result = new MediaInfoResult(
            new MediaInfoResult.CreatingLibraryInfo("", "", ""),
            null!
        );
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = result };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.DidNotReceive().GetTrackListForType(Arg.Any<TrackType>());
    }

    [Fact]
    public void SynchronizeTrackCount_ReferenceFileMediaTrackIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Audio);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Audio);
        
        var audioTracks = new List<TrackConfiguration> { new(trackInfo) { Type = TrackType.Audio } };
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var media = new MediaInfoResult.MediaInfo("ref", null!);
        var result = new MediaInfoResult(
            new MediaInfoResult.CreatingLibraryInfo("", "", ""),
            media
        );
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = result };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, TrackType.Audio);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.DidNotReceive().GetTrackListForType(Arg.Any<TrackType>());
    }

    [Fact]
    public void SynchronizeTrackCount_TrackTypesIsNull_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Audio);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Audio);
        
        var audioTracks = new List<TrackConfiguration> { new(trackInfo) { Type = TrackType.Audio } };
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile, null!);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.DidNotReceive().GetTrackListForType(Arg.Any<TrackType>());
    }

    [Fact]
    public void SynchronizeTrackCount_EmptyTrackTypesArray_DoesNotModifyBatchConfig()
    {
        // Arrange
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary().AddTrackOfType(TrackType.Audio);
        var trackInfo = builder.Build().Media.Track.First(t => t.Type == TrackType.Audio);
        
        var audioTracks = new List<TrackConfiguration> { new(trackInfo) { Type = TrackType.Audio } };
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);

        var synchronizer = new BatchTrackCountSynchronizer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var referenceFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        synchronizer.SynchronizeTrackCount(referenceFile);

        // Assert
        Assert.Single(audioTracks);
        mockConfig.DidNotReceive().GetTrackListForType(Arg.Any<TrackType>());
    }
}
