using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Core.Tests.Services;

public class BatchConfigurationTests
{
    [Fact]
    public void Clear_WhenCalled_ResetsAllPropertiesAndTrackCollections()
    {
        // Arrange
        var config = new BatchConfiguration
        {
            DirectoryPath = "C:\\media",
            Title = "TestTitle",
            AudioTracks = [new() { Name = "A" }],
            VideoTracks = [new() { Name = "V" }],
            SubtitleTracks = [new() { Name = "S" }]
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
        var config = new BatchConfiguration();

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
        var config = new BatchConfiguration();
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
        var config = new BatchConfiguration();
        var unknownType = (TrackType)999;

        // Act
        var result = config.GetTrackListForType(unknownType);

        // Assert
        Assert.Empty(result);
    }
}
