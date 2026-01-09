using System.Collections.Immutable;
using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
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
                    Dictionary<string, FileTrackConfiguration> fileConfigs, 
                    Dictionary<string, FileTrackAvailability> fileTrackMap,
                    ObservableCollection<TrackConfiguration> audioTracks,
                    ObservableCollection<TrackConfiguration> videoTracks,
                    ObservableCollection<TrackConfiguration> subtitleTracks) CreateMockConfig()
    {
        var mockConfig = Substitute.For<IBatchConfiguration>();
        var fileConfigs = new Dictionary<string, FileTrackConfiguration>();
        var fileTrackMap = new Dictionary<string, FileTrackAvailability>();
        var audioTracks = new ObservableCollection<TrackConfiguration>();
        var videoTracks = new ObservableCollection<TrackConfiguration>();
        var subtitleTracks = new ObservableCollection<TrackConfiguration>();
        var fileList = new ObservableCollection<ScannedFileInfo>();
        
        mockConfig.FileConfigurations.Returns(fileConfigs);
        mockConfig.FileTrackMap.Returns(fileTrackMap);
        mockConfig.AudioTracks.Returns(audioTracks);
        mockConfig.VideoTracks.Returns(videoTracks);
        mockConfig.SubtitleTracks.Returns(subtitleTracks);
        mockConfig.FileList.Returns(fileList);
        mockConfig.GetTrackListForType(TrackType.Audio).Returns(audioTracks);
        mockConfig.GetTrackListForType(TrackType.Video).Returns(videoTracks);
        mockConfig.GetTrackListForType(TrackType.Text).Returns(subtitleTracks);
        
        return (mockConfig, fileConfigs, fileTrackMap, audioTracks, videoTracks, subtitleTracks);
    }

    private static BatchTrackConfigurationInitializer CreateInitializer(
        IBatchConfiguration batchConfig, 
        ILanguageProvider? languageProvider = null)
    {
        languageProvider ??= CreateMockLanguageProvider();
        var availabilityRecorder = new FileTrackAvailabilityRecorder(batchConfig);
        var trackConfigFactory = new TrackConfigurationFactory(languageProvider);
        return new BatchTrackConfigurationInitializer(batchConfig, availabilityRecorder, trackConfigFactory);
    }

    [Fact]
    public void Initialize_CreatesPerFileConfiguration()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var scannedFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - Verify per-file configuration was created
        Assert.True(fileConfigs.ContainsKey("file.mkv"));
        Assert.Equal(3, fileConfigs["file.mkv"].AudioTracks.Count);
        
        // Verify track availability was recorded
        Assert.True(fileTrackMap.ContainsKey("file.mkv"));
        Assert.Equal(3, fileTrackMap["file.mkv"].AudioTrackCount);
        
        // Verify global tracks were populated (first file)
        Assert.Equal(3, audioTracks.Count);
    }

    [Fact]
    public void Initialize_PopulatesMultipleTrackTypes()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, audioTracks, videoTracks, subtitleTracks) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Audio)
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Text)
            .AddTrackOfType(TrackType.Text)
            .Build();
        var scannedFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio, TrackType.Video, TrackType.Text);

        // Assert
        var fileConfig = fileConfigs["file.mkv"];
        Assert.Equal(2, fileConfig.AudioTracks.Count);
        Assert.Single(fileConfig.VideoTracks);
        Assert.Equal(2, fileConfig.SubtitleTracks.Count);
        
        var availability = fileTrackMap["file.mkv"];
        Assert.Equal(2, availability.AudioTrackCount);
        Assert.Equal(1, availability.VideoTrackCount);
        Assert.Equal(2, availability.SubtitleTrackCount);
        
        // Verify global tracks populated
        Assert.Equal(2, audioTracks.Count);
        Assert.Single(videoTracks);
        Assert.Equal(2, subtitleTracks.Count);
    }

    [Fact]
    public void Initialize_UpdatesGlobalTracksToMaximumCount()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, audioTracks, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // First file - 2 audio tracks
        var firstFile = new ScannedFileInfo
        {
            Path = "file1.mkv",
            Result = new MediaInfoResultBuilder()
                .WithCreatingLibrary()
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .Build()
        };
        initializer.Initialize(firstFile, TrackType.Audio);

        // Second file - 3 audio tracks
        var secondFile = new ScannedFileInfo
        {
            Path = "file2.mkv",
            Result = new MediaInfoResultBuilder()
                .WithCreatingLibrary()
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .AddTrackOfType(TrackType.Audio)
                .Build()
        };
        initializer.Initialize(secondFile, TrackType.Audio);

        // Assert - Global tracks should now be 3 (maximum across all files)
        Assert.Equal(3, audioTracks.Count);
        
        // Per-file configurations should be correct
        Assert.Equal(2, fileConfigs["file1.mkv"].AudioTracks.Count);
        Assert.Equal(3, fileConfigs["file2.mkv"].AudioTracks.Count);
    }

    [Fact]
    public void Initialize_HandlesEmptyTrackTypes()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var scannedFile = new ScannedFileInfo { Path = "file.mkv", Result = mediaInfoResult };

        // Act - Call with no track types (empty params array)
        initializer.Initialize(scannedFile);

        // Assert - Nothing should be created (method returns early when trackTypes is empty)
        Assert.False(fileTrackMap.ContainsKey("file.mkv"));
        Assert.False(fileConfigs.ContainsKey("file.mkv"));
    }

    [Fact]
    public void Initialize_NullScannedFile_DoesNothing()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // Act
        initializer.Initialize(null!, TrackType.Audio);

        // Assert - No entries created
        Assert.Empty(fileConfigs);
        Assert.Empty(fileTrackMap);
    }

    [Fact]
    public void Initialize_NullResult_DoesNothing()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        var scannedFile = new ScannedFileInfo { Path = "file.mkv", Result = null! };

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - No entries created
        Assert.Empty(fileConfigs);
        Assert.Empty(fileTrackMap);
    }

    [Fact]
    public void Initialize_EmptyMediaInfo_CreatesConfigWithoutTracks()
    {
        // Arrange
        var (mockConfig, fileConfigs, fileTrackMap, _, _, _) = CreateMockConfig();
        var initializer = CreateInitializer(mockConfig);

        // Create a MediaInfoResult with creating library but no tracks
        var emptyResult = new MediaInfoResultBuilder().WithCreatingLibrary().Build();
        var scannedFile = new ScannedFileInfo { Path = "file.mkv", Result = emptyResult };

        // Act
        initializer.Initialize(scannedFile, TrackType.Audio);

        // Assert - FileTrackMap should have entry with 0 tracks
        Assert.True(fileTrackMap.ContainsKey("file.mkv"));
        Assert.Equal(0, fileTrackMap["file.mkv"].AudioTrackCount);
        
        // FileConfiguration should be created even with no tracks
        Assert.True(fileConfigs.ContainsKey("file.mkv"));
        Assert.Empty(fileConfigs["file.mkv"].AudioTracks);
    }
}
