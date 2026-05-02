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

public class SubtitleViewModelTests
{
    private readonly ILanguageProvider _languageProvider;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly ILogger<SubtitleViewModel> _logger;

    public SubtitleViewModelTests()
    {
        _languageProvider = Substitute.For<ILanguageProvider>();
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _uiPreferences = Substitute.For<IUIPreferencesService>();
        _logger = Substitute.For<ILogger<SubtitleViewModel>>();

        _languageProvider.Languages.Returns([]);
        _batchConfiguration.SubtitleTracks.Returns(new ObservableCollection<TrackIntent>());
        _batchConfiguration.FileList.Returns(new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>()));
    }

    [Fact]
    public void Constructor_InitializesSubtitleTracksFromBatchConfiguration()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Track 1" };
        var subtitleTracks = new ObservableCollection<TrackIntent> { intent };
        _batchConfiguration.SubtitleTracks.Returns(subtitleTracks);

        // Act
        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.Single(viewModel.SubtitleTracks);
        Assert.Equal("Track 1", viewModel.SubtitleTracks[0].Name);
    }

    [Fact]
    public void Constructor_SetsSelectedTrackToFirstTrack()
    {
        // Arrange
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Track 1" };
        var subtitleTracks = new ObservableCollection<TrackIntent> { intent };
        _batchConfiguration.SubtitleTracks.Returns(subtitleTracks);

        // Act
        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

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
            .AddTrackOfType(TrackType.Text)
            .Build();

        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        fileList.Add(new ScannedFileInfo(mediaInfoResult, "file1.mkv"));
        _batchConfiguration.FileList.Returns(fileList);

        // Act
        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.True(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void IsFileListPopulated_ReturnsFalseWhenNoFilesExist()
    {
        // Arrange - fileList is empty by default

        // Act
        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        // Assert
        Assert.False(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void OnFileListChanged_UpdatesIsFileListPopulated()
    {
        // Arrange
        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        _batchConfiguration.FileList.Returns(fileList);

        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);
        Assert.False(viewModel.IsFileListPopulated);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
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
    public void OnBatchConfigurationSubtitleTracksChanged_UpdatesSubtitleTracks()
    {
        // Arrange
        var subtitleTracks = new ObservableCollection<TrackIntent>();
        _batchConfiguration.SubtitleTracks.Returns(subtitleTracks);

        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);
        Assert.Empty(viewModel.SubtitleTracks);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);
        var newTrack = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Name = "New Track" };

        // Act
        subtitleTracks.Add(newTrack);

        // Assert
        Assert.Single(viewModel.SubtitleTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_UpdatesSubtitleTracksWhenSubtitleTracksPropertyChanges()
    {
        // Arrange
        var initialTracks = new ObservableCollection<TrackIntent>();
        _batchConfiguration.SubtitleTracks.Returns(initialTracks);

        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Track 1" };
        var newTracks = new ObservableCollection<TrackIntent> { intent };

        // Act
        _batchConfiguration.SubtitleTracks.Returns(newTracks);
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.SubtitleTracks)));

        // Assert
        Assert.Single(viewModel.SubtitleTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_DoesNotUpdateWhenOtherPropertiesChange()
    {
        // Arrange
        var subtitleTracks = new ObservableCollection<TrackIntent>();
        _batchConfiguration.SubtitleTracks.Returns(subtitleTracks);

        var viewModel = new SubtitleViewModel(_logger, _languageProvider, _batchConfiguration, _uiPreferences);
        int subtitleTracksChangedCount = 0;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.SubtitleTracks))
                subtitleTracksChangedCount++;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.AudioTracks)));

        // Assert
        Assert.Equal(0, subtitleTracksChangedCount);
    }
}
