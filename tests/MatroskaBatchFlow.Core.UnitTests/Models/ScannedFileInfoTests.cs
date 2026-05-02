using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

public class ScannedFileInfoTests
{
    [Fact]
    public void GetTracks_ReturnsTracksOrderedByStreamKindId()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(2).Build())
            .AddTrack(new TrackInfoBuilder().WithType(TrackType.Video).WithStreamKindID(0).Build())
            .AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build())
            .AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(1).Build())
            .Build();

        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");

        // Act
        var audioTracks = scannedFile.GetTracks(TrackType.Audio);

        // Assert
        Assert.Collection(
            audioTracks,
            track => Assert.Equal(0, track.StreamKindID),
            track => Assert.Equal(1, track.StreamKindID),
            track => Assert.Equal(2, track.StreamKindID));
    }
}
