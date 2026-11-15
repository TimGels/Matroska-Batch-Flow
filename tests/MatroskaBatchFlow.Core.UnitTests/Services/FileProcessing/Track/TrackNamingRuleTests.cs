using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileProcessing.Track;

public class TrackNamingRuleTests
{
    [Theory]
    [InlineData(TrackType.Audio, "")]
    [InlineData(TrackType.Audio, "   ")]
    [InlineData(TrackType.Audio, "Original")]
    [InlineData(TrackType.Video, "")]
    [InlineData(TrackType.Video, "   ")]
    [InlineData(TrackType.Video, "Original")]
    [InlineData(TrackType.Text, "")]
    [InlineData(TrackType.Text, "   ")]
    [InlineData(TrackType.Text, "Original")]
    public void Apply_AfterResettingUserSetName_RestoresDetectedName(TrackType type, string scannedName)
    {
        var trackInfo = new TrackInfoBuilder()
            .WithType(type)
            .WithStreamKindID(0)
            .WithTitle(scannedName)
            .Build();
        var result = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrack(trackInfo)
            .Build();
        var file = new ScannedFileInfo(result, "file.mkv");
        var config = new BatchConfiguration(
            Substitute.For<IScannedFileInfoPathComparer>(),
            Substitute.For<ILogger<BatchConfiguration>>());
        config.FileList.Add(file);
        var intent = new TrackIntent(trackInfo)
        {
            Type = type,
            Index = 0,
            Name = "Edited",
            ShouldModifyName = true
        };
        config.GetTrackListForType(type).Add(intent);
        intent.MarkUserSet(TrackIntentProperty.Name);
        intent.MarkUserSet(TrackIntentProperty.Default);
        IFileProcessingRule rule = type switch
        {
            TrackType.Audio => new AudioTrackNamingRule(),
            TrackType.Video => new VideoTrackNamingRule(),
            TrackType.Text => new SubtitleTrackNamingRule(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        rule.Apply(file, config);
        Assert.Equal("Edited", intent.Name);

        intent.ResetUserSet(TrackIntentProperty.Name);
        rule.Apply(file, config);

        Assert.Equal(string.IsNullOrWhiteSpace(scannedName) ? string.Empty : scannedName, intent.Name);
        Assert.False(intent.IsNameUserSet);
        Assert.True(intent.IsDefaultUserSet);
        Assert.True(intent.ShouldModifyName);
    }
}
