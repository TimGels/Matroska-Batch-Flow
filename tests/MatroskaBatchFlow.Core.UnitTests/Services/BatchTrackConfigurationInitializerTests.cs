using System.Collections.Immutable;
using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

/// <summary>
/// Contains unit tests for the BatchTrackConfigurationInitializer class, verifying correct expansion of
/// global track intent collections based on scanned file information.
/// </summary>
public class BatchTrackConfigurationInitializerTests
{
    private static ILanguageProvider CreateMockLanguageProvider()
    {
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        mockLanguageProvider.Languages.Returns(ImmutableList<MatroskaLanguageOption>.Empty);
        mockLanguageProvider.Resolve(Arg.Any<string?>()).Returns(MatroskaLanguageOption.Undetermined);
        return mockLanguageProvider;
    }

    private static (IBatchConfiguration mockConfig,
                    ObservableCollection<TrackIntent> audioTracks,
                    ObservableCollection<TrackIntent> videoTracks,
                    ObservableCollection<TrackIntent> subtitleTracks) CreateMockConfig()
    {
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var audioTracks = new ObservableCollection<TrackIntent>();
        var videoTracks = new ObservableCollection<TrackIntent>();
        var subtitleTracks = new ObservableCollection<TrackIntent>();
        var mockComparer = Substitute.For<IScannedFileInfoPathComparer>();
        var fileList = new UniqueObservableCollection<ScannedFileInfo>(mockComparer);

        mockConfig.AudioTracks.Returns(audioTracks);
        mockConfig.VideoTracks.Returns(videoTracks);
        mockConfig.SubtitleTracks.Returns(subtitleTracks);
        mockConfig.FileList.Returns(fileList);
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);
        mockConfig.GetTrackListForType(TrackType.Video).Returns(videoTracks);
        mockConfig.GetTrackListForType(TrackType.Text).Returns(subtitleTracks);

        return (mockConfig, audioTracks, videoTracks, subtitleTracks);
    }

    private static BatchTrackConfigurationInitializer CreateInitializer(
        IBatchConfiguration batchConfig,
        ILanguageProvider? languageProvider = null)
    {
        languageProvider ??= CreateMockLanguageProvider();
        var trackIntentFactory = new TrackIntentFactory(languageProvider);
        return new BatchTrackConfigurationInitializer(batchConfig, trackIntentFactory);
    }

    [Fact]
    public void Initialize_PopulatesGlobalTracks()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");
        mockConfig.FileList.Add(scannedFile);

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - Global tracks should be populated
        Assert.Equal(3, audioTracks.Count);
    }

    [Fact]
    public void Initialize_PopulatesMultipleTrackTypes()
    {
        // Arrange
        var (mockConfig, audioTracks, videoTracks, subtitleTracks) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Text)
            .AddTrackOfType(TrackType.Text)
            .Build();
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");
        mockConfig.FileList.Add(scannedFile);

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio, TrackType.Video, TrackType.Text);

        // Assert - Verify ScannedFileInfo has correct track counts
        Assert.Equal(2, scannedFile.AudioTrackCount);
        Assert.Equal(1, scannedFile.VideoTrackCount);
        Assert.Equal(2, scannedFile.SubtitleTrackCount);

        // Verify global tracks populated
        Assert.Equal(2, audioTracks.Count);
        Assert.Single(videoTracks);
        Assert.Equal(2, subtitleTracks.Count);
    }

    [Fact]
    public void Initialize_UpdatesGlobalTracksToMaximumCount()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // First file - 2 audio tracks
        var firstFile = new ScannedFileInfo(
            new MediaInfoResultBuilder()
                .WithCreatingLibrary()
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .Build(),
            "file1.mkv"
        );
        mockConfig.FileList.Add(firstFile);
        initializer.Initialize(firstFile, TrackType.Audio);

        // Second file - 3 audio tracks
        var secondFile = new ScannedFileInfo(
            new MediaInfoResultBuilder()
                .WithCreatingLibrary()
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .Build(),
            "file2.mkv"
        );
        mockConfig.FileList.Add(secondFile);
        initializer.Initialize(secondFile, TrackType.Audio);

        // Assert - Global tracks should now be 3 (maximum across all files)
        Assert.Equal(3, audioTracks.Count);
    }

    [Fact]
    public void Initialize_HandlesEmptyTrackTypes()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");

        // Act - Call with no track types (empty params array)
        initializer.Initialize(scannedFile);

        // Assert - Nothing should be created (method returns early when trackTypes is empty)
        Assert.Empty(audioTracks);
    }

    [Fact]
    public void Initialize_NullScannedFile_DoesNothing()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // Act
        initializer.Initialize(null!, TrackType.Audio);

        // Assert - No tracks created
        Assert.Empty(audioTracks);
    }

    [Fact]
    public void Initialize_NullResult_DoesNothing()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var scannedFile = new ScannedFileInfo(null!, "file.mkv");

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - No tracks created
        Assert.Empty(audioTracks);
    }

    [Fact]
    public void Initialize_EmptyMediaInfo_DoesNotAddTracks()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // Create a MediaInfoResult with creating library but no tracks
        var emptyResult = new MediaInfoResultBuilder().WithCreatingLibrary().Build();
        var scannedFile = new ScannedFileInfo(emptyResult, "file.mkv");

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - ScannedFileInfo should have 0 audio tracks
        Assert.Equal(0, scannedFile.AudioTrackCount);
        Assert.Empty(audioTracks);
    }

    [Fact]
    public void Initialize_GlobalTracksHaveCorrectScannedTrackInfo()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");
        mockConfig.FileList.Add(scannedFile);

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - global tracks have correct properties
        Assert.Equal(2, audioTracks.Count);

        Assert.Equal(TrackType.Audio, audioTracks[0].Type);
        Assert.Equal(0, audioTracks[0].Index);
        Assert.NotNull(audioTracks[0].ScannedTrackInfo);

        Assert.Equal(TrackType.Audio, audioTracks[1].Type);
        Assert.Equal(1, audioTracks[1].Index);
        Assert.NotNull(audioTracks[1].ScannedTrackInfo);
    }

    [Fact]
    public void Initialize_DoesNotShrinkGlobalTracksWhenFewerTracksScanned()
    {
        // Arrange
        var (mockConfig, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // First file - 3 audio tracks
        var firstFile = new ScannedFileInfo(
            new MediaInfoResultBuilder()
                .WithCreatingLibrary()
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .Build(),
            "file1.mkv"
        );
        mockConfig.FileList.Add(firstFile);
        initializer.Initialize(firstFile, TrackType.Audio);
        Assert.Equal(3, audioTracks.Count);

        // Second file - only 1 audio track
        var secondFile = new ScannedFileInfo(
            new MediaInfoResultBuilder()
                .WithCreatingLibrary()
                .AddTrackOfType(TrackType.Audio)
                .Build(),
            "file2.mkv"
        );
        mockConfig.FileList.Add(secondFile);
        initializer.Initialize(secondFile, TrackType.Audio);

        // Assert - Global tracks should still be 3 (never shrinks)
        Assert.Equal(3, audioTracks.Count);
    }
}
