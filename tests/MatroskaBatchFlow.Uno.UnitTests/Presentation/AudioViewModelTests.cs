using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

public class AudioViewModelTests
{
    private readonly ILanguageProvider _languageProvider;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly ILogger<AudioViewModel> _logger;

    public AudioViewModelTests()
    {
        _languageProvider = Substitute.For<ILanguageProvider>();
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _uiPreferences = Substitute.For<IUIPreferencesService>();
        _logger = Substitute.For<ILogger<AudioViewModel>>();

        _languageProvider.Languages.Returns([]);
        _batchConfiguration.AudioTracks.Returns(new ObservableCollection<TrackConfiguration>());
        _batchConfiguration.FileList.Returns(new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>()));
        _batchConfiguration.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());
    }

    [Fact]
    public void Constructor_InitializesAudioTracksFromBatchConfiguration()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Audio);

        var audioTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "Track 1" }
        };
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.Single(viewModel.AudioTracks);
        Assert.Equal("Track 1", viewModel.AudioTracks[0].Name);
    }

    [Fact]
    public void Constructor_SetsSelectedTrackToFirstTrack()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Audio);

        var audioTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "Track 1" }
        };
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

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
            .AddTrackOfType(TrackType.Audio)
            .Build();

        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        fileList.Add(new ScannedFileInfo(mediaInfoResult, "file1.mkv"));
        _batchConfiguration.FileList.Returns(fileList);

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.True(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void IsFileListPopulated_ReturnsFalseWhenNoFilesExist()
    {
        // Arrange - fileList is empty by default

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.False(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void OnFileListChanged_UpdatesIsFileListPopulated()
    {
        // Arrange
        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        _batchConfiguration.FileList.Returns(fileList);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);
        Assert.False(viewModel.IsFileListPopulated);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();

        bool propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsFileListPopulated))
                propertyChangedRaised = true;
        };

        // Act
        fileList.Add(new ScannedFileInfo(mediaInfoResult, "file1.mkv"));

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void OnBatchConfigurationAudioTracksChanged_UpdatesAudioTracks()
    {
        // Arrange
        var audioTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);
        Assert.Empty(viewModel.AudioTracks);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Audio);
        var newTrack = new TrackConfiguration(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "New Track" };

        // Act
        audioTracks.Add(newTrack);

        // Assert
        Assert.Single(viewModel.AudioTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_UpdatesAudioTracksWhenAudioTracksPropertyChanges()
    {
        // Arrange
        var initialTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.AudioTracks.Returns(initialTracks);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Audio);

        var newTracks = new ObservableCollection<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "Track 1" }
        };

        // Act
        _batchConfiguration.AudioTracks.Returns(newTracks);
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.AudioTracks)));

        // Assert
        Assert.Single(viewModel.AudioTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_DoesNotUpdateWhenOtherPropertiesChange()
    {
        // Arrange
        var audioTracks = new ObservableCollection<TrackConfiguration>();
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);
        int audioTracksChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.AudioTracks))
                audioTracksChangedCount++;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.VideoTracks)));

        // Assert
        Assert.Equal(0, audioTracksChangedCount);
    }

}
