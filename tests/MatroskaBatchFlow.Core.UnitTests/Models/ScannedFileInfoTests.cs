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

    [Fact]
    public void Constructor_PathWithoutExtension_IsUnsupportedWithMissingExtension()
    {
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddMatroskaGeneralTrack()
            .Build();

        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file");

        Assert.Equal(new UnsupportedFormatIssue(UnsupportedFormatKind.MissingExtension), scannedFile.UnsupportedFormatReason);
        Assert.False(scannedFile.IsSupported);
    }

    [Theory]
    [InlineData("Matroska", "MPEG-4")]
    [InlineData("MPEG-4", "Matroska")]
    public void Constructor_ConflictingGeneralTrackFormats_IsUnsupportedWithNonMatroskaContainer(string firstFormat, string secondFormat)
    {
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrack(new TrackInfoBuilder().WithType(TrackType.General).WithFormat(firstFormat).Build())
            .AddTrack(new TrackInfoBuilder().WithType(TrackType.General).WithFormat(secondFormat).Build())
            .Build();

        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");

        Assert.Equal(new UnsupportedFormatIssue(UnsupportedFormatKind.WrongContainer, "MPEG-4"), scannedFile.UnsupportedFormatReason);
        Assert.False(scannedFile.IsSupported);
    }
}
