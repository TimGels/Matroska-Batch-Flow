using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

public class VideoViewModelTests
{
    private readonly ILanguageProvider _languageProvider;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IUIPreferencesService _uiPreferences;

    public VideoViewModelTests()
    {
        _languageProvider = Substitute.For<ILanguageProvider>();
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _uiPreferences = Substitute.For<IUIPreferencesService>();

        _languageProvider.Languages.Returns([]);
        _batchConfiguration.VideoTracks.Returns(new ObservableCollection<TrackConfiguration>());
        _batchConfiguration.FileList.Returns(new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>()));
        _batchConfiguration.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());
    }

    [Fact]
    public void Constructor_InitializesVideoTracksFromBatchConfiguration()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Video);

        var videoTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Video, Index = 0, Name = "Track 1" }
        };
        _batchConfiguration.VideoTracks.Returns(videoTracks);

        // Act
        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.Single(viewModel.VideoTracks);
        Assert.Equal("Track 1", viewModel.VideoTracks[0].Name);
    }

    [Fact]
    public void Constructor_SetsSelectedTrackToFirstTrack()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Video);

        var videoTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Video, Index = 0, Name = "Track 1" }
        };
        _batchConfiguration.VideoTracks.Returns(videoTracks);

        // Act
        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.NotNull(viewModel.SelectedTrack);
        Assert.Equal(0, viewModel.SelectedTrack.Index);
    }

    [Fact]
    public void IsFileListPopulated_ReturnsTrueWhenFilesExist()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();

        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        fileList.Add(new ScannedFileInfo(mediaInfoResult, "file1.mkv"));
        _batchConfiguration.FileList.Returns(fileList);

        // Act
        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.True(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void IsFileListPopulated_ReturnsFalseWhenNoFilesExist()
    {
        // Arrange - fileList is empty by default

        // Act
        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.False(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void OnFileListChanged_UpdatesIsFileListPopulated()
    {
        // Arrange
        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        _batchConfiguration.FileList.Returns(fileList);

        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);
        Assert.False(viewModel.IsFileListPopulated);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();

        bool propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsFileListPopulated))
                propertyChangedRaised = true;
        };

        // Act
        fileList.Add(new ScannedFileInfo(mediaInfoResult, "file1.mkv"));
        _batchConfiguration.FileList.CollectionChanged += Raise.Event<NotifyCollectionChangedEventHandler>(
            _batchConfiguration.FileList,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new[] { fileList[0] }));

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void OnBatchConfigurationVideoTracksChanged_UpdatesVideoTracks()
    {
        // Arrange
        var videoTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.VideoTracks.Returns(videoTracks);

        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);
        Assert.Empty(viewModel.VideoTracks);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Video);
        var newTrack = new TrackConfiguration(trackInfo) { Type = TrackType.Video, Index = 0, Name = "New Track" };

        // Act
        videoTracks.Add(newTrack);
        _batchConfiguration.VideoTracks.CollectionChanged += Raise.Event<NotifyCollectionChangedEventHandler>(
            _batchConfiguration.VideoTracks,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new[] { newTrack }));

        // Assert
        Assert.Single(viewModel.VideoTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_UpdatesVideoTracksWhenVideoTracksPropertyChanges()
    {
        // Arrange
        var initialTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.VideoTracks.Returns(initialTracks);

        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Video);

        var newTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Video, Index = 0, Name = "Track 1" }
        };

        // Act
        _batchConfiguration.VideoTracks.Returns(newTracks);
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.VideoTracks)));

        // Assert
        Assert.Single(viewModel.VideoTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_DoesNotUpdateWhenOtherPropertiesChange()
    {
        // Arrange
        var videoTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.VideoTracks.Returns(videoTracks);

        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);
        int videoTracksChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.VideoTracks))
                videoTracksChangedCount++;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.AudioTracks)));

        // Assert
        Assert.Equal(0, videoTracksChangedCount);
    }

    [Fact]
    public void GetTrackType_ReturnsVideo()
    {
        // Arrange
        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        // Act - Use reflection to access protected method
        var method = typeof(VideoViewModel).GetMethod("GetTrackType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(viewModel, null);

        // Assert
        Assert.Equal(TrackType.Video, result);
    }

    [Fact]
    public void VideoTracks_IsTrackSelected_UpdatedWhenTracksChange()
    {
        // Arrange
        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        bool isTrackSelectedChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsTrackSelected))
                isTrackSelectedChanged = true;
        };

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Video);

        var newTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Video, Index = 0 }
        };

        // Act
        viewModel.VideoTracks = newTracks;

        // Assert
        Assert.True(isTrackSelectedChanged);
    }

    [Fact]
    public void OnBatchConfigurationVideoTracksChanged_ResetsSelectedTrackToFirstTrack()
    {
        // Arrange
        var videoTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.VideoTracks.Returns(videoTracks);

        var viewModel = new VideoViewModel(_languageProvider, _batchConfiguration, _uiPreferences);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Video)
            .Build();
        var videoTrackInfos = mediaInfoResult.Media.Track.Where(t => t.Type == TrackType.Video).ToArray();
        
        var track1 = new TrackConfiguration(videoTrackInfos[0]) { Type = TrackType.Video, Index = 0, Name = "Track 1" };
        var track2 = new TrackConfiguration(videoTrackInfos[1]) { Type = TrackType.Video, Index = 1, Name = "Track 2" };

        videoTracks.Add(track1);
        videoTracks.Add(track2);

        // Simulate collection changed event
        _batchConfiguration.VideoTracks.CollectionChanged += Raise.Event<NotifyCollectionChangedEventHandler>(
            _batchConfiguration.VideoTracks,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        // Assert
        Assert.NotNull(viewModel.SelectedTrack);
        Assert.Equal(0, viewModel.SelectedTrack.Index);
    }
}
