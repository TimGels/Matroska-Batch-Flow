using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

public class MkvPropeditArgumentsGeneratorTests
{
    [Fact]
    public void BuildBatchArguments_WhenNoPropertiesAreEnabled_ReturnsEmpty()
    {
        // Arrange
        var batchConfig = CreateBatchConfiguration();
        var file = CreateScannedFile(
            "C:\\media\\episode-01.mkv",
            new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());

        AddAndInitializeTracks(batchConfig, file);

        var sut = new MkvPropeditArgumentsGenerator(Substitute.For<ILogger<MkvPropeditArgumentsGenerator>>());

        // Act
        var commands = sut.BuildBatchArguments(batchConfig);

        // Assert
        Assert.Empty(commands);
    }

    [Fact]
    public void BuildBatchArguments_WhenSubtitleNameIsEnabled_GeneratesCommandOnlyForFilesWithThatTrack()
    {
        // Arrange
        var batchConfig = CreateBatchConfiguration();

        var fileWithSubtitle = CreateScannedFile(
            "C:\\media\\with-subtitle.mkv",
            new TrackInfoBuilder().WithType(TrackType.Text).WithStreamKindID(0).WithTitle("Original").Build());

        var fileWithoutSubtitle = CreateScannedFile(
            "C:\\media\\without-subtitle.mkv",
            new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());

        AddAndInitializeTracks(batchConfig, fileWithSubtitle, fileWithoutSubtitle);

        batchConfig.SubtitleTracks[0].ShouldModifyName = true;
        batchConfig.SubtitleTracks[0].Name = "Renamed Subtitle";

        var sut = new MkvPropeditArgumentsGenerator(Substitute.For<ILogger<MkvPropeditArgumentsGenerator>>());

        // Act
        var commands = sut.BuildBatchArguments(batchConfig);

        // Assert
        Assert.Single(commands);
        Assert.Contains("with-subtitle.mkv", commands[0], StringComparison.Ordinal);
        Assert.Contains("--edit track:s1", commands[0], StringComparison.Ordinal);
        Assert.Contains("name=\"Renamed Subtitle\"", commands[0], StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBatchArguments_WhenContainerAndTrackSettingsEnabled_IncludesAllExpectedDirectArguments()
    {
        // Arrange
        var batchConfig = CreateBatchConfiguration();

        var file = CreateScannedFile(
            "C:\\media\\movie.mkv",
            new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build());

        AddAndInitializeTracks(batchConfig, file);

        batchConfig.ShouldModifyTitle = true;
        batchConfig.Title = "New Container Title";
        batchConfig.ShouldModifyTrackStatisticsTags = true;
        batchConfig.AddTrackStatisticsTags = true;
        batchConfig.DeleteTrackStatisticsTags = true;

        batchConfig.AudioTracks[0].ShouldModifyDefaultFlag = true;
        batchConfig.AudioTracks[0].Default = true;

        var sut = new MkvPropeditArgumentsGenerator(Substitute.For<ILogger<MkvPropeditArgumentsGenerator>>());

        // Act
        var commands = sut.BuildBatchArguments(batchConfig);

        // Assert
        Assert.Single(commands);
        Assert.Contains("--edit info", commands[0], StringComparison.Ordinal);
        Assert.Contains("title=\"New Container Title\"", commands[0], StringComparison.Ordinal);
        Assert.Contains("--add-track-statistics-tags", commands[0], StringComparison.Ordinal);
        Assert.Contains("--delete-track-statistics-tags", commands[0], StringComparison.Ordinal);
        Assert.Contains("--edit track:a1", commands[0], StringComparison.Ordinal);
        Assert.Contains("flag-default=1", commands[0], StringComparison.Ordinal);
    }

    private static BatchConfiguration CreateBatchConfiguration()
    {
        var platformService = Substitute.For<IPlatformService>();
        platformService.IsWindows().Returns(true);

        var comparer = new ScannedFileInfoPathComparer(platformService);
        var logger = Substitute.For<ILogger<BatchConfiguration>>();

        return new BatchConfiguration(comparer, logger);
    }

    private static void AddAndInitializeTracks(BatchConfiguration batchConfig, params ScannedFileInfo[] files)
    {
        var languageProvider = Substitute.For<ILanguageProvider>();
        languageProvider.Languages.Returns(ImmutableList<MatroskaLanguageOption>.Empty);
        languageProvider.Resolve(Arg.Any<string?>()).Returns(MatroskaLanguageOption.Undetermined);

        var initializer = new BatchTrackConfigurationInitializer(
            batchConfig,
            new TrackIntentFactory(languageProvider));

        foreach (var file in files)
        {
            batchConfig.FileList.Add(file);
            initializer.Initialize(file, TrackType.Audio, TrackType.Video, TrackType.Text);
        }
    }

    private static ScannedFileInfo CreateScannedFile(string path, params MediaInfoResult.MediaInfo.TrackInfo[] tracks)
    {
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        foreach (var track in tracks)
        {
            builder.AddTrack(track);
        }

        return new ScannedFileInfo(builder.Build(), path);
    }
}
