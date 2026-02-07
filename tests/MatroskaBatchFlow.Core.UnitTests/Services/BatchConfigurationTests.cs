using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

/// <summary>
/// Contains unit tests for the BatchConfiguration class, verifying its property reset behavior and track collection
/// management.
/// </summary>
/// <remarks>These tests ensure that BatchConfiguration correctly resets its properties and track collections when
/// Clear is called, and that it returns the appropriate track lists for known and unknown track types. The tests are
/// intended to validate the public API and expected usage scenarios of BatchConfiguration.</remarks>
public class BatchConfigurationTests
{
    private readonly IScannedFileInfoPathComparer _comparer = Substitute.For<IScannedFileInfoPathComparer>();
    private readonly ILogger<BatchConfiguration> _logger = Substitute.For<ILogger<BatchConfiguration>>();

    [Fact]
    public void Clear_WhenCalled_ResetsAllPropertiesAndTrackCollections()
    {
        // Arrange
        var audioTrackInfo = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build()
            .Media.Track.First(t => t.Type == TrackType.Audio);
            
        var videoTrackInfo = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build()
            .Media.Track.First(t => t.Type == TrackType.Video);
            
        var subtitleTrackInfo = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build()
            .Media.Track.First(t => t.Type == TrackType.Text);
        
        var config = new BatchConfiguration(_comparer, _logger)
        {
            DirectoryPath = "C:\\media",
            Title = "TestTitle",
            AudioTracks = [new(audioTrackInfo) { Name = "A" }],
            VideoTracks = [new(videoTrackInfo) { Name = "V" }],
            SubtitleTracks = [new(subtitleTrackInfo) { Name = "S" }]
        };

        // Act
        config.Clear();

        // Assert
        Assert.Empty(config.DirectoryPath);
        Assert.Empty(config.Title);
        Assert.Empty(config.AudioTracks);
        Assert.Empty(config.VideoTracks);
        Assert.Empty(config.SubtitleTracks);
    }

    [Fact]
    public void Clear_WhenCalledOnDefaultInstance_KeepsPropertiesAndCollectionsEmpty()
    {
        // Arrange
        var config = new BatchConfiguration(_comparer, _logger);

        // Act
        config.Clear();

        // Assert
        Assert.Empty(config.DirectoryPath);
        Assert.Empty(config.Title);
        Assert.Empty(config.AudioTracks);
        Assert.Empty(config.VideoTracks);
        Assert.Empty(config.SubtitleTracks);
    }

    [Theory]
    [InlineData(TrackType.Audio)]
    [InlineData(TrackType.Video)]
    [InlineData(TrackType.Text)]
    public void GetTrackListForType_WhenCalledWithKnownType_ReturnsCorrectCollection(TrackType type)
    {
        // Arrange
        var config = new BatchConfiguration(_comparer, _logger);
        var audio = config.AudioTracks;
        var video = config.VideoTracks;
        var sub = config.SubtitleTracks;

        // Act
        var result = config.GetTrackListForType(type);

        // Assert
        if (type == TrackType.Audio)
            Assert.Same(audio, result);
        else if (type == TrackType.Video)
            Assert.Same(video, result);
        else if (type == TrackType.Text)
            Assert.Same(sub, result);
    }

    [Fact]
    public void GetTrackListForType_WhenCalledWithUnknownType_ReturnsEmptyList()
    {
        // Arrange
        var config = new BatchConfiguration(_comparer, _logger);
        var unknownType = (TrackType)999;

        // Act
        var result = config.GetTrackListForType(unknownType);

        // Assert
        Assert.Empty(result);
    }
}
