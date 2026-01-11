using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.IntegrationTests;

/// <summary>
/// Contains integration tests that verify track name modification behavior across multiple files in batch subtitle
/// editing scenarios.
/// </summary>
/// <remarks>These tests ensure that enabling and applying track name modifications affect only the intended files
/// and tracks, and that related state changes and command generation behave as expected. The class is part of the
/// integration test suite and interacts with real configuration and view model components to validate end-to-end
/// functionality.</remarks>
[Collection("Integration")]
public class TrackModificationIntegrationTests
{
    [Fact]
    public async Task IntegrationTest_EditingTrackOnlyInSecondFile_TriggersStateChangedAndGeneratesCommands()
    {
        // Arrange - create real components for integration testing
        var platformService = new PlatformService();
        var fileComparer = new ScannedFileInfoPathComparer(platformService);
        var batchConfig = new BatchConfiguration(fileComparer);
        
        // Mock only external dependencies (language data, UI preferences)
        var mockLanguageProvider = Substitute.For<ILanguageProvider>();
        mockLanguageProvider.Languages.Returns(ImmutableList<MatroskaLanguageOption>.Empty);
        var mockUIPreferences = Substitute.For<IUIPreferencesService>();

        var file1Path = "file1.mkv";
        var file2Path = "file2.mkv";

        // Build MediaInfo results - File1 has 1 subtitle track, File2 has 17 subtitle tracks
        var file1MediaInfo = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.Text)
            .Build();

        var file2Builder = new MediaInfoResultBuilder().WithCreatingLibrary();
        for (int i = 0; i < 17; i++)
        {
            file2Builder.AddTrackOfType(TrackType.Text);
        }
        var file2MediaInfo = file2Builder.Build();

        // Create ScannedFileInfo objects
        var file1 = new ScannedFileInfo(file1MediaInfo, file1Path);
        var file2 = new ScannedFileInfo(file2MediaInfo, file2Path);

        // Add files to batch configuration
        batchConfig.FileList.Add(file1);
        batchConfig.FileList.Add(file2);

        // Initialize per-file configurations
        var availabilityRecorder = new FileTrackAvailabilityRecorder(batchConfig);
        var trackConfigFactory = new TrackConfigurationFactory(mockLanguageProvider);
        var initializer = new BatchTrackConfigurationInitializer(batchConfig, availabilityRecorder, trackConfigFactory);
        initializer.Initialize(file1, TrackType.Text);
        initializer.Initialize(file2, TrackType.Text);

        // Track StateChanged event robustly
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        batchConfig.StateChanged += (_, __) => tcs.TrySetResult(true);

        // Create SubtitleViewModel using real BatchConfiguration
        var subtitleViewModel = new SubtitleViewModel(mockLanguageProvider, batchConfig, mockUIPreferences);

        // Select track 17 (index 16) - only exists in file2
        Assert.Equal(17, batchConfig.SubtitleTracks.Count);
        subtitleViewModel.SelectedTrack = batchConfig.SubtitleTracks[16];

        // Act - enable name modification and set a new name
        tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        batchConfig.StateChanged += (_, __) => tcs.TrySetResult(true);

        subtitleViewModel.IsTrackNameModificationEnabled = true;
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(tcs.Task.IsCompleted, "StateChanged should fire when enabling modification");

        // Prepare for the next state change
        tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        batchConfig.StateChanged += (_, __) => tcs.TrySetResult(true);

        subtitleViewModel.TrackName = "Track17Modified";
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(tcs.Task.IsCompleted, "StateChanged should fire when updating track name");

        // Verify global configuration was updated
        Assert.Equal("Track17Modified", batchConfig.SubtitleTracks[16].Name);
        Assert.True(batchConfig.SubtitleTracks[16].ShouldModifyName);

        // Verify per-file configurations
        var file1Config = batchConfig.FileConfigurations[file1.Id];
        Assert.Single(file1Config.SubtitleTracks);
        Assert.NotEqual("Track17Modified", file1Config.SubtitleTracks[0].Name);

        var file2Config = batchConfig.FileConfigurations[file2.Id];
        Assert.Equal(17, file2Config.SubtitleTracks.Count);
        Assert.Equal("Track17Modified", file2Config.SubtitleTracks[16].Name);
        Assert.True(file2Config.SubtitleTracks[16].ShouldModifyName);

        // Verify command generation works correctly
        var argumentsGenerator = new MkvPropeditArgumentsGenerator();
        var commands = argumentsGenerator.BuildBatchArguments(batchConfig);

        Assert.Single(commands);
        Assert.NotEmpty(commands[0]);
        Assert.Contains("--edit track:s17", commands[0]);
        Assert.Contains("--set name=", commands[0]);
        Assert.Contains("Track17Modified", commands[0]);

        Assert.False(commands.All(c => string.IsNullOrWhiteSpace(c)));
    }
}
