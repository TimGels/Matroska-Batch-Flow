using System.Collections.Immutable;
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

/// <summary>
/// Contains unit tests for the TrackViewModelBase class, verifying correct behavior of track property updates in batch
/// configurations.
/// </summary>
/// <remarks>These tests ensure that changes to track properties in the view model are properly propagated to both
/// global and per-file configurations. The class uses a test-specific implementation of TrackViewModelBase to
/// facilitate testing scenarios.</remarks>
public class TrackViewModelBaseTests
{
    /// <summary>
    /// Test implementation of TrackViewModelBase for testing purposes
    /// </summary>
    private class TestTrackViewModel : TrackViewModelBase
    {
        private readonly IList<TrackConfiguration> _testTracks;

        public TestTrackViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration, IUIPreferencesService uiPreferences, IList<TrackConfiguration> tracks)
            : base(languageProvider, batchConfiguration, uiPreferences)
        {
            _testTracks = tracks;
            SetupEventHandlers();
        }

        protected override IList<TrackConfiguration> GetTracks() => _testTracks;

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
    public void UpdateBatchConfigTrackProperty_UpdatesGlobalAndPerFileConfigurations()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        // Create test track info using builder
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();
        var trackInfo = mediaInfoResult.Media.Track.First(t => t.Type == TrackType.Text);

        // Set up global track collection
        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Original" }
        };

        // Set up per-file configurations
        var file1 = new ScannedFileInfo(mediaInfoResult, "file1.mkv");
        var file2 = new ScannedFileInfo(mediaInfoResult, "file2.mkv");

        var file1Config = new FileTrackConfiguration { FilePath = file1.Path };
        file1Config.SubtitleTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Original" });

        var file2Config = new FileTrackConfiguration { FilePath = file2.Path };
        file2Config.SubtitleTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Original" });

        var fileConfigurations = new Dictionary<Guid, FileTrackConfiguration>
        {
            { file1.Id, file1Config },
            { file2.Id, file2Config }
        };

        mockBatchConfig.FileConfigurations.Returns(fileConfigurations);

        // Create view model
        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);

        // Set the selected track
        viewModel.SelectedTrack = globalTracks[0];

        // Act - enable modification and update the track name
        viewModel.IsTrackNameModificationEnabled = true;
        viewModel.TrackName = "Updated Name";

        // Assert - verify global track was updated
        Assert.Equal("Updated Name", globalTracks[0].Name);
        Assert.True(globalTracks[0].ShouldModifyName);

        // Assert - verify per-file configurations were also updated
        Assert.Equal("Updated Name", file1Config.SubtitleTracks[0].Name);
        Assert.True(file1Config.SubtitleTracks[0].ShouldModifyName);

        Assert.Equal("Updated Name", file2Config.SubtitleTracks[0].Name);
        Assert.True(file2Config.SubtitleTracks[0].ShouldModifyName);
    }

    [Fact]
    public void UpdateBatchConfigTrackProperty_SkipsFilesWithoutTrack()
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

        // Global has track 0
        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Original" }
        };

        // File1 has track 0
        var file1 = new ScannedFileInfo(mediaInfoResult, "file1.mkv");
        var file1Config = new FileTrackConfiguration { FilePath = file1.Path };
        file1Config.SubtitleTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Original" });

        // File2 has NO tracks (different track count)
        var file2 = new ScannedFileInfo(mediaInfoResult, "file2.mkv");
        var file2Config = new FileTrackConfiguration { FilePath = file2.Path };
        // Intentionally empty subtitle tracks list

        var fileConfigurations = new Dictionary<Guid, FileTrackConfiguration>
        {
            { file1.Id, file1Config },
            { file2.Id, file2Config }
        };

        mockBatchConfig.FileConfigurations.Returns(fileConfigurations);

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act - enable modification and update track name
        viewModel.IsTrackNameModificationEnabled = true;
        viewModel.TrackName = "Updated Name";

        // Assert - file1 should be updated
        Assert.Equal("Updated Name", file1Config.SubtitleTracks[0].Name);

        // Assert - file2 should not crash (it has no tracks to update)
        Assert.Empty(file2Config.SubtitleTracks);
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

        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Name = "Test Track", Default = true }
        };

        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
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

        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0 }
        };

        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);

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

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();

        var globalTracks = new List<TrackConfiguration>();
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);

        // Act & Assert
        Assert.False(viewModel.IsTrackSelected);
    }

    [Fact]
    public void Languages_PropertyChangedRaised_WhenValueChanges()
    {
        // Arrange
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        var mockBatchConfig = Substitute.For<IBatchConfiguration>();
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var initialLanguages = ImmutableList<MatroskaLanguageOption>.Empty;
        mockLanguageProvider.Languages.Returns(initialLanguages);

        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();

        var globalTracks = new List<TrackConfiguration>();
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);

        bool propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.Languages))
                propertyChangedRaised = true;
        };

        var newLanguages = ImmutableList.Create(new MatroskaLanguageOption("English", "en", "eng", "eng", "eng"));

        // Act
        viewModel.Languages = newLanguages;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Same(newLanguages, viewModel.Languages);
    }

    [Fact]
    public void IsDefaultTrack_UpdatesBatchConfiguration_WhenChanged()
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

        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Default = false }
        };

        var file1 = new ScannedFileInfo(mediaInfoResult, "file1.mkv");
        var file1Config = new FileTrackConfiguration { FilePath = file1.Path };
        file1Config.SubtitleTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Default = false });

        var fileConfigurations = new Dictionary<Guid, FileTrackConfiguration>
        {
            { file1.Id, file1Config }
        };

        mockBatchConfig.FileConfigurations.Returns(fileConfigurations);

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act
        viewModel.IsDefaultTrack = true;

        // Assert
        Assert.True(globalTracks[0].Default);
        Assert.True(file1Config.SubtitleTracks[0].Default);
    }

    [Fact]
    public void IsForcedTrack_UpdatesBatchConfiguration_WhenChanged()
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

        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Forced = false }
        };

        var file1 = new ScannedFileInfo(mediaInfoResult, "file1.mkv");
        var file1Config = new FileTrackConfiguration { FilePath = file1.Path };
        file1Config.SubtitleTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Forced = false });

        var fileConfigurations = new Dictionary<Guid, FileTrackConfiguration>
        {
            { file1.Id, file1Config }
        };

        mockBatchConfig.FileConfigurations.Returns(fileConfigurations);

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        // Act
        viewModel.IsForcedTrack = true;

        // Assert
        Assert.True(globalTracks[0].Forced);
        Assert.True(file1Config.SubtitleTracks[0].Forced);
    }

    [Fact]
    public void SelectedLanguage_UpdatesBatchConfiguration_WhenChanged()
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

        var globalTracks = new List<TrackConfiguration>
        {
            new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Language = MatroskaLanguageOption.Undetermined }
        };

        var file1 = new ScannedFileInfo(mediaInfoResult, "file1.mkv");
        var file1Config = new FileTrackConfiguration { FilePath = file1.Path };
        file1Config.SubtitleTracks.Add(new TrackConfiguration(trackInfo) { Type = TrackType.Text, Index = 0, Language = MatroskaLanguageOption.Undetermined });

        var fileConfigurations = new Dictionary<Guid, FileTrackConfiguration>
        {
            { file1.Id, file1Config }
        };

        mockBatchConfig.FileConfigurations.Returns(fileConfigurations);

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, globalTracks);
        viewModel.SelectedTrack = globalTracks[0];

        var newLanguage = new MatroskaLanguageOption("English", "en", "eng", "eng", "eng");

        // Act
        viewModel.SelectedLanguage = newLanguage;

        // Assert
        Assert.Same(newLanguage, globalTracks[0].Language);
        Assert.Same(newLanguage, file1Config.SubtitleTracks[0].Language);
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
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackConfiguration>());

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
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackConfiguration>());

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
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackConfiguration>());

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
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        // Act
        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackConfiguration>());

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
        mockBatchConfig.FileConfigurations.Returns(new Dictionary<Guid, FileTrackConfiguration>());

        var viewModel = new TestTrackViewModel(mockLanguageProvider, mockBatchConfig, mockUIPreferences, new List<TrackConfiguration>());
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
