using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;
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
        _batchConfiguration.AudioTracks.Returns(new ObservableCollection<TrackIntent>());
        _batchConfiguration.FileList.Returns(new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Original")]
    public void ResetProperty_Name_RestoresDetectedNameInViewModel(string scannedName)
    {
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithStreamKindID(0)
            .WithTitle(scannedName)
            .Build();
        var result = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddMatroskaGeneralTrack()
            .AddTrack(trackInfo)
            .Build();
        var file = new ScannedFileInfo(result, "file.mkv");
        var config = new BatchConfiguration(
            Substitute.For<IScannedFileInfoPathComparer>(),
            Substitute.For<ILogger<BatchConfiguration>>());
        config.FileList.Add(file);
        var intent = new TrackIntent(trackInfo) { Type = TrackType.Audio, Index = 0, Name = scannedName };
        config.AudioTracks.Add(intent);
        var engine = new FileProcessingEngine([new AudioTrackNamingRule()]);
        var viewModel = new AudioViewModel(_logger, _languageProvider, config, engine, _uiPreferences);
        viewModel.IsTrackNameModificationEnabled = true;
        viewModel.TrackName = "Edited";
        Assert.True(viewModel.IsNameUserSet);

        viewModel.ResetPropertyCommand.Execute(TrackIntentProperty.Name);

        Assert.False(intent.IsNameUserSet);
        Assert.Equal(scannedName, intent.Name);
        Assert.Equal(scannedName, viewModel.TrackName);
        Assert.False(viewModel.IsNameUserSet);
        Assert.True(viewModel.IsTrackNameModificationEnabled);
    }

    [Fact]
    public void ResetProperty_Name_UnsupportedFirstFile_RestoresNameFromSupportedFile()
    {
        var config = new BatchConfiguration(
            Substitute.For<IScannedFileInfoPathComparer>(),
            Substitute.For<ILogger<BatchConfiguration>>());
        config.FileList.Add(CreateAudioFile("unsupported.mkv", "Ignored", supported: false));
        var supportedFile = CreateAudioFile("supported.mkv", "Original", supported: true);
        config.FileList.Add(supportedFile);
        var intent = new TrackIntent(supportedFile.GetTracks(TrackType.Audio)[0]) { Type = TrackType.Audio, Index = 0, Name = "Original" };
        config.AudioTracks.Add(intent);
        var engine = new FileProcessingEngine([new AudioTrackNamingRule()]);
        var viewModel = new AudioViewModel(_logger, _languageProvider, config, engine, _uiPreferences);
        viewModel.TrackName = "Edited";

        viewModel.ResetPropertyCommand.Execute(TrackIntentProperty.Name);

        Assert.Equal("Original", intent.Name);
        Assert.False(intent.IsNameUserSet);
    }

    [Fact]
    public void ResetProperty_Name_NoSupportedFiles_KeepsUserSetName()
    {
        var config = new BatchConfiguration(
            Substitute.For<IScannedFileInfoPathComparer>(),
            Substitute.For<ILogger<BatchConfiguration>>());
        var file = CreateAudioFile("unsupported.mkv", "Original", supported: false);
        config.FileList.Add(file);
        var intent = new TrackIntent(file.GetTracks(TrackType.Audio)[0]) { Type = TrackType.Audio, Index = 0 };
        config.AudioTracks.Add(intent);
        var engine = new FileProcessingEngine([new AudioTrackNamingRule()]);
        var viewModel = new AudioViewModel(_logger, _languageProvider, config, engine, _uiPreferences);
        viewModel.TrackName = "Edited";

        viewModel.ResetPropertyCommand.Execute(TrackIntentProperty.Name);

        Assert.Equal("Edited", intent.Name);
        Assert.True(intent.IsNameUserSet);
    }

    private static ScannedFileInfo CreateAudioFile(string path, string title, bool supported)
    {
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        if (supported)
        {
            builder.AddMatroskaGeneralTrack();
        }

        var result = builder
            .AddTrack(new TrackInfoBuilder()
                .WithType(TrackType.Audio)
                .WithStreamKindID(0)
                .WithTitle(title)
                .Build())
            .Build();
        return new ScannedFileInfo(result, path);
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

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "Track 1" };
        var audioTracks = new ObservableCollection<TrackIntent> { intent };
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);

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

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "Track 1" };
        var audioTracks = new ObservableCollection<TrackIntent> { intent };
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);

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
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);

        // Assert
        Assert.True(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void IsFileListPopulated_ReturnsFalseWhenNoFilesExist()
    {
        // Arrange - fileList is empty by default

        // Act
        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);

        // Assert
        Assert.False(viewModel.IsFileListPopulated);
    }

    [Fact]
    public void OnFileListChanged_UpdatesIsFileListPopulated()
    {
        // Arrange
        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        _batchConfiguration.FileList.Returns(fileList);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);
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
        var audioTracks = new ObservableCollection<TrackIntent>();
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);
        Assert.Empty(viewModel.AudioTracks);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Audio);
        var newTrack = new TrackIntent(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "New Track" };

        // Act
        audioTracks.Add(newTrack);

        // Assert
        Assert.Single(viewModel.AudioTracks);
    }

    [Fact]
    public void OnBatchConfigurationChanged_UpdatesAudioTracksWhenAudioTracksPropertyChanges()
    {
        // Arrange
        var initialTracks = new ObservableCollection<TrackIntent>();
        _batchConfiguration.AudioTracks.Returns(initialTracks);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Audio)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Audio);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Audio, Index = 0, Name = "Track 1" };
        var newTracks = new ObservableCollection<TrackIntent> { intent };

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
        var audioTracks = new ObservableCollection<TrackIntent>();
        _batchConfiguration.AudioTracks.Returns(audioTracks);

        var viewModel = new AudioViewModel(_logger, _languageProvider, _batchConfiguration, Substitute.For<IFileProcessingEngine>(), _uiPreferences);
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
