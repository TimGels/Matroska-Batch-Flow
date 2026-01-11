using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
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
}
