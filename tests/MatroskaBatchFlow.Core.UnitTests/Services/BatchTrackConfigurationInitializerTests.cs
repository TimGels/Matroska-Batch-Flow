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
/// Contains unit tests for the BatchTrackConfigurationInitializer class, verifying correct creation of
/// per-file track configurations based on scanned file information.
/// </summary>
public class BatchTrackConfigurationInitializerTests
{
    private static ILanguageProvider CreateMockLanguageProvider()
    {
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        mockLanguageProvider.Languages.Returns(ImmutableList<MatroskaLanguageOption>.Empty);
        return mockLanguageProvider;
    }

    private static (IBatchConfiguration mockConfig, 
                    Dictionary<Guid, FileTrackConfiguration> fileConfigs,
                    ObservableCollection<TrackConfiguration> audioTracks,
                    ObservableCollection<TrackConfiguration> videoTracks,
                    ObservableCollection<TrackConfiguration> subtitleTracks) CreateMockConfig()
    {
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var fileConfigs = new Dictionary<Guid, FileTrackConfiguration>();
        var audioTracks = new ObservableCollection<TrackConfiguration>();
        var videoTracks = new ObservableCollection<TrackConfiguration>();
        var subtitleTracks = new ObservableCollection<TrackConfiguration>();
        var mockComparer = Substitute.For<IScannedFileInfoPathComparer>();
        var fileList = new UniqueObservableCollection<ScannedFileInfo>(mockComparer);
        
        mockConfig.FileConfigurations.Returns(fileConfigs);
        mockConfig.AudioTracks.Returns(audioTracks);
        mockConfig.VideoTracks.Returns(videoTracks);
        mockConfig.SubtitleTracks.Returns(subtitleTracks);
        mockConfig.FileList.Returns(fileList);
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);
        mockConfig.GetTrackListForType(TrackType.Video).Returns(videoTracks);
        mockConfig.GetTrackListForType(TrackType.Text).Returns(subtitleTracks);
        
        return (mockConfig, fileConfigs, audioTracks, videoTracks, subtitleTracks);
    }

    private static BatchTrackConfigurationInitializer CreateInitializer(
        IBatchConfiguration batchConfig, 
        ILanguageProvider? languageProvider = null)
    {
        languageProvider ??= CreateMockLanguageProvider();
        var trackConfigFactory = new TrackConfigurationFactory(languageProvider);
        return new BatchTrackConfigurationInitializer(batchConfig, trackConfigFactory);
    }

    [Fact]
    public void Initialize_CreatesPerFileConfiguration()
    {
        // Arrange
        var (mockConfig, fileConfigs, audioTracks, _, _) = CreateMockConfig();
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

        // Assert - Verify per-file configuration was created
        Assert.True(fileConfigs.ContainsKey(scannedFile.Id));
        Assert.Equal(3, fileConfigs[scannedFile.Id].AudioTracks.Count);
        
        // Verify global tracks were populated (first file)
        Assert.Equal(3, audioTracks.Count);
    }

    [Fact]
    public void Initialize_PopulatesMultipleTrackTypes()
    {
        // Arrange
        var (mockConfig, fileConfigs, audioTracks, videoTracks, subtitleTracks) = CreateMockConfig();
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

        // Assert
        var fileConfig = fileConfigs[scannedFile.Id];
        Assert.Equal(2, fileConfig.AudioTracks.Count);
        Assert.Single(fileConfig.VideoTracks);
        Assert.Equal(2, fileConfig.SubtitleTracks.Count);
        
        // Verify ScannedFileInfo has correct track counts
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
        var (mockConfig, fileConfigs, audioTracks, _, _) = CreateMockConfig();
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
        
        // Per-file configurations should be correct
        Assert.Equal(2, fileConfigs[firstFile.Id].AudioTracks.Count);
        Assert.Equal(3, fileConfigs[secondFile.Id].AudioTracks.Count);
    }

    [Fact]
    public void Initialize_HandlesEmptyTrackTypes()
    {
        // Arrange
        var (mockConfig, fileConfigs, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var scannedFile = new ScannedFileInfo(mediaInfoResult, "file.mkv");

        // Act - Call with no track types (empty params array)
        initializer.Initialize(scannedFile);

        // Assert - Nothing should be created (method returns early when trackTypes is empty)
        Assert.False(fileConfigs.ContainsKey(scannedFile.Id));
    }

    [Fact]
    public void Initialize_NullScannedFile_DoesNothing()
    {
        // Arrange
        var (mockConfig, fileConfigs, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // Act
        initializer.Initialize(null!, TrackType.Audio);

        // Assert - No entries created
        Assert.Empty(fileConfigs);
    }

    [Fact]
    public void Initialize_NullResult_DoesNothing()
    {
        // Arrange
        var (mockConfig, fileConfigs, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var scannedFile = new ScannedFileInfo(null!, "file.mkv");

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - No entries created
        Assert.Empty(fileConfigs);
    }

    [Fact]
    public void Initialize_EmptyMediaInfo_CreatesConfigWithoutTracks()
    {
        // Arrange
        var (mockConfig, fileConfigs, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // Create a MediaInfoResult with creating library but no tracks
        var emptyResult = new MediaInfoResultBuilder().WithCreatingLibrary().Build();
        var scannedFile = new ScannedFileInfo(emptyResult, "file.mkv");

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - ScannedFileInfo should have 0 audio tracks
        Assert.Equal(0, scannedFile.AudioTrackCount);
        
        // FileConfiguration should be created even with no tracks
        Assert.True(fileConfigs.ContainsKey(scannedFile.Id));
        Assert.Empty(fileConfigs[scannedFile.Id].AudioTracks);
    }
}
