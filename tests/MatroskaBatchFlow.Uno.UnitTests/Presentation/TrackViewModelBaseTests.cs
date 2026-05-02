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

/// <summary>
/// Contains unit tests for the TrackViewModelBase class, verifying correct behavior of track property updates
/// against TrackIntent properties.
/// </summary>
public class TrackViewModelBaseTests
{
    private readonly ILogger _mockLogger = Substitute.For<ILogger>();

    /// <summary>
    /// Test implementation of TrackViewModelBase for testing purposes
    /// </summary>
    private class TestTrackViewModel : TrackViewModelBase
    {
        private readonly IList<TrackIntent> _testTracks;

        public TestTrackViewModel(ILogger logger, ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration, IUIPreferencesService uiPreferences, IList<TrackIntent> tracks)
            : base(logger, languageProvider, batchConfiguration, uiPreferences)
        {
            _testTracks = tracks;
            SetupEventHandlers();
        }

        protected override IList<TrackIntent> GetTracks() => _testTracks;

        protected override TrackType GetTrackType() => TrackType.Text;

        protected override void SetupEventHandlers()
        {
            // Minimal implementation for testing
        }

        protected override void OnBatchConfigurationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
        {
            // Minimal implementation for testing
        }
    }

    [Fact]
    public void TrackName_WhenChanged_UpdatesTrackIntentName()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Original" };
        var globalTracks = new List<TrackIntent> { intent };

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act
        viewModel.IsTrackNameModificationEnabled = true;
        viewModel.TrackName = "Updated Name";

        // Assert
        Assert.Equal("Updated Name", intent.Name);
        Assert.True(intent.ShouldModifyName);
    }

    [Fact]
    public void SelectedTrack_WhenSetToNull_ResetsAllPropertiesToDefault()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo)
        {
            Type = TrackType.Text,
            Index = 0,
            Name = "Test Track",
            Default = true,
        };
        var globalTracks = new List<TrackIntent> { intent };

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act - Set selected track to null
        viewModel.SelectedTrack = null;

        // Assert - All properties should be reset to defaults
        Assert.False(viewModel.IsDefaultTrack);
        Assert.True(viewModel.IsEnabledTrack);
        Assert.False(viewModel.IsForcedTrack);
        Assert.Equal(string.Empty, viewModel.TrackName);
        Assert.Null(viewModel.SelectedLanguage);
        Assert.False(viewModel.IsDefaultFlagModificationEnabled);
        Assert.False(viewModel.IsEnabledFlagModificationEnabled);
        Assert.False(viewModel.IsForcedFlagModificationEnabled);
        Assert.False(viewModel.IsTrackNameModificationEnabled);
        Assert.False(viewModel.IsSelectedLanguageModificationEnabled);
    }

    [Fact]
    public void IsTrackSelected_ReturnsTrueWhenTrackIsSelected()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0 };
        var globalTracks = new List<TrackIntent> { intent };

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);

        // Act
        viewModel.SelectedTrack = globalTracks[0];

        // Assert
        Assert.True(viewModel.IsTrackSelected);
    }

    [Fact]
    public void IsTrackSelected_ReturnsFalseWhenNoTrackIsSelected()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var globalTracks = new List<TrackIntent>();

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);

        // Act & Assert
        Assert.False(viewModel.IsTrackSelected);
    }

    [Fact]
    public void IsDefaultTrack_WhenChanged_UpdatesTrackIntentDefaultFlag()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Default = false };
        var globalTracks = new List<TrackIntent> { intent };

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act
        viewModel.IsDefaultTrack = true;

        // Assert
        Assert.True(intent.Default);
    }

    [Fact]
    public void IsForcedTrack_WhenChanged_UpdatesTrackIntentForcedFlag()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo) { Type = TrackType.Text, Index = 0, Forced = false };
        var globalTracks = new List<TrackIntent> { intent };

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act
        viewModel.IsForcedTrack = true;

        // Assert
        Assert.True(intent.Forced);
    }

    [Fact]
    public void SelectedLanguage_WhenChanged_UpdatesTrackIntentLanguage()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        var intent = new TrackIntent(trackInfo)
        {
            Type = TrackType.Text,
            Index = 0,
            Language = MatroskaLanguageOption.Undetermined,
        };
        var globalTracks = new List<TrackIntent> { intent };

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        var newLanguage = new MatroskaLanguageOption("English", "en", "eng", "eng", "eng");

        // Act
        viewModel.SelectedLanguage = newLanguage;

        // Assert
        Assert.Same(newLanguage, intent.Language);
    }

    [Fact]
    public void GetTrackAvailabilityCount_ReturnsCorrectCount()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult1 = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        
        var mediaInfoResult2 = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .AddTrackOfType(TrackType.Text)
            .Build();

        var file1 = new ScannedFileInfo(mediaInfoResult1, "file1.mkv");
        var file2 = new ScannedFileInfo(mediaInfoResult2, "file2.mkv");

        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        fileList.Add(file1);
        fileList.Add(file2);

        mockBatchConfig.FileList.Returns(fileList);

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackIntent>());

        // Act
        int count0 = viewModel.GetTrackAvailabilityCount(0);
        int count1 = viewModel.GetTrackAvailabilityCount(1);

        // Assert
        Assert.Equal(2, count0); // Both files have track 0
        Assert.Equal(1, count1); // Only file2 has track 1
    }

    [Fact]
    public void GetTrackAvailabilityText_ReturnsFormattedString()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();

        var file1 = new ScannedFileInfo(mediaInfoResult, "file1.mkv");

        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        fileList.Add(file1);

        mockBatchConfig.FileList.Returns(fileList);

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackIntent>());

        // Act
        string result = viewModel.GetTrackAvailabilityText(0);

        // Assert
        Assert.Equal("1/1", result);
    }

    [Fact]
    public void TotalFileCount_ReturnsCorrectCount()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var fileList = new UniqueObservableCollection<ScannedFileInfo>(Substitute.For<IScannedFileInfoPathComparer>());
        mockBatchConfig.FileList.Returns(fileList);

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackIntent>());

        // Act & Assert
        Assert.Equal(0, viewModel.TotalFileCount);
    }

    [Fact]
    public void ShowTrackAvailabilityText_ReflectsUIPreferencesValue()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        mockUIPreferences.ShowTrackAvailabilityText.Returns(true);

        // Act
        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackIntent>());

        // Assert
        Assert.True(viewModel.ShowTrackAvailabilityText);
    }

    [Fact]
    public void UIPreferences_PropertyChanged_UpdatesShowTrackAvailabilityText()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        mockUIPreferences.ShowTrackAvailabilityText.Returns(false);

        var viewModel = new TestTrackViewModel(_mockLogger, mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackIntent>());
        Assert.False(viewModel.ShowTrackAvailabilityText);

        // Act
        mockUIPreferences.ShowTrackAvailabilityText.Returns(true);
        mockUIPreferences.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            mockUIPreferences,
            new PropertyChangedEventArgs(nameof(IUIPreferencesService.ShowTrackAvailabilityText)));

        // Assert
        Assert.True(viewModel.ShowTrackAvailabilityText);
    }
}
