using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

public sealed class BatchResultsViewModelTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 23, 19, 0, 0, TimeSpan.Zero);

    private readonly string _directory = Directory.CreateTempSubdirectory("mbf-results-").FullName;
    private readonly string _historyPath;
    private readonly BatchHistoryService _history;

    public BatchResultsViewModelTests()
    {
        var userSettings = Substitute.For<IWritableSettings<UserSettings>>();
        userSettings.Value.Returns(new UserSettings());
        userSettings.ReadFromDiskAsync().Returns(_ => Task.FromResult(userSettings.Value));
        _historyPath = Path.Combine(_directory, "BatchHistory.json");
        _history = new BatchHistoryService(_historyPath, userSettings, NullLogger<BatchHistoryService>.Instance);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task NewRun_IsShownWithFilesThatNeedAttentionFirst()
    {
        var viewModel = new BatchResultsViewModel(_history);
        Assert.Equal(BatchOutcome.None, viewModel.Outcome);
        Assert.False(viewModel.HasRuns);

        await _history.AddAsync(Run(
            File("updated.mkv", ProcessingStatus.Succeeded),
            File("unchanged.mkv", ProcessingStatus.Skipped),
            File("failed.mkv", ProcessingStatus.Failed),
            File("warned.mkv", ProcessingStatus.SucceededWithWarnings)));

        Assert.True(viewModel.HasRuns);
        Assert.Equal(BatchOutcome.Failures, viewModel.Outcome);
        Assert.Equal(new BatchResultCounts(4, 2, 1, 1, 1, 0, 0), viewModel.Counts);
        Assert.Equal(["failed.mkv", "warned.mkv", "updated.mkv", "unchanged.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));
        Assert.Equal(
            [FileResultFilter.All, FileResultFilter.Failed, FileResultFilter.Warnings, FileResultFilter.Updated, FileResultFilter.Unchanged],
            viewModel.FilterOptions.Select(o => o.Filter));
    }

    [Fact]
    public async Task SelectingOlderRun_ShowsItsFiles_UntilANewRunFinishes()
    {
        await _history.AddAsync(Run(File("old.mkv", ProcessingStatus.Succeeded)));
        await _history.AddAsync(Run(File("recent.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        Assert.Equal(["recent.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));

        viewModel.SelectedRun = viewModel.Runs[1];
        Assert.Equal(["old.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));

        await _history.AddAsync(Run(File("new.mkv", ProcessingStatus.Succeeded)));

        Assert.Same(viewModel.Runs[0], viewModel.SelectedRun);
        Assert.Equal(["new.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));
    }

    [Fact]
    public async Task Filter_FallsBackToAll_WhenSelectedRunHasNoMatchingFiles()
    {
        await _history.AddAsync(Run(File("next.mkv", ProcessingStatus.Succeeded), File("other.mkv", ProcessingStatus.Skipped)));
        await _history.AddAsync(Run(File("ok.mkv", ProcessingStatus.Succeeded), File("bad.mkv", ProcessingStatus.Failed)));
        var viewModel = new BatchResultsViewModel(_history);

        viewModel.SelectedFilter = viewModel.FilterOptions.Single(o => o.Filter == FileResultFilter.Failed);
        Assert.Equal(["bad.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));

        viewModel.SelectedRun = viewModel.Runs[1];

        Assert.Equal(FileResultFilter.All, viewModel.SelectedFilter!.Filter);
        Assert.Equal(["next.mkv", "other.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));
    }

    [Fact]
    public async Task ClearResultHistory_ShowsEmptyPageAndHidesThePicker()
    {
        await _history.AddAsync(Run(File("file.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        Assert.True(viewModel.CanClearResultHistory);
        var result = await viewModel.RemoveHistoryAsync(viewModel.CaptureListedResults());

        Assert.Empty(viewModel.Runs);
        Assert.Null(viewModel.SelectedRun);
        Assert.True(viewModel.HasNoBatch);
        Assert.False(viewModel.HasHistoryPicker);
        Assert.False(viewModel.CanClearResultHistory);
        Assert.Empty(viewModel.VisibleFiles);
        Assert.Equal(HistoryRemovalStatus.Removed, result.Status);
    }

    [Fact]
    public async Task ClearResultHistory_UsesTheDisplayedCountAndDoesNotRemoveALaterResult()
    {
        await _history.AddAsync(Run(File("first.mkv", ProcessingStatus.Succeeded)));
        await _history.AddAsync(Run(File("second.mkv", ProcessingStatus.Failed)));
        var viewModel = new BatchResultsViewModel(_history);
        var confirmed = viewModel.CaptureListedResults();
        var later = Run(File("later.mkv", ProcessingStatus.Succeeded));
        await _history.AddAsync(later);

        Assert.Equal(2, confirmed.RunIds.Count);
        var result = await viewModel.RemoveHistoryAsync(confirmed);

        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 2), result);
        Assert.Equal(later.Id, Assert.Single(viewModel.Runs).Run.Id);
        Assert.Same(viewModel.Runs[0], viewModel.SelectedRun);
    }

    [Fact]
    public async Task UnreadableHistory_OffersRecoveryWithoutOfferingClear_EvenWithNoListedRuns()
    {
        var viewModel = new BatchResultsViewModel(_history);
        var pickerAvailability = new List<bool>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.HasHistoryPicker))
            {
                pickerAvailability.Add(viewModel.HasHistoryPicker);
            }
        };
        await System.IO.File.WriteAllTextAsync(_historyPath, "invalid", TestContext.Current.CancellationToken);
        await _history.LoadAsync();

        Assert.True(viewModel.HasUnreadableHistory);
        Assert.False(viewModel.CanRemoveResultHistory);
        Assert.False(viewModel.CanClearResultHistory);
        Assert.Empty(viewModel.Runs);
        Assert.Equal(HistoryRecoveryStatus.Recovered, (await viewModel.ResetUnreadableHistoryAsync()).Status);

        Assert.Equal([true, false], pickerAvailability);
        Assert.False(viewModel.HasUnreadableHistory);
        Assert.True(viewModel.CanRemoveResultHistory);
        Assert.False(viewModel.CanClearResultHistory);
    }

    [Fact]
    public async Task ClearResultHistory_WhenDeletionFails_KeepsTheRunsAndAllowsRetry()
    {
        await _history.AddAsync(Run(File("file.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        // A directory at the temporary file path makes deletion fail on both Windows and Unix.
        var blockedPath = Directory.CreateDirectory(_historyPath + ".tmp");

        var removal = viewModel.CaptureListedResults();
        var result = await viewModel.RemoveHistoryAsync(removal);

        Assert.Equal(HistoryRemovalStatus.Failed, result.Status);
        Assert.Single(viewModel.Runs);
        Assert.True(System.IO.File.Exists(_historyPath));

        blockedPath.Delete();
        result = await viewModel.RemoveHistoryAsync(removal);

        Assert.Equal(HistoryRemovalStatus.Removed, result.Status);
        Assert.Empty(viewModel.Runs);
    }

    [Fact]
    public async Task FileExpansion_StaysWithItsRowAcrossFiltersAndRunSelection()
    {
        await _history.AddAsync(Run(File("old.mkv", ProcessingStatus.Succeeded)));
        await _history.AddAsync(Run(File("ok.mkv", ProcessingStatus.Succeeded), File("bad.mkv", ProcessingStatus.Failed)));
        var viewModel = new BatchResultsViewModel(_history);
        var originalRun = viewModel.SelectedRun;
        var expanded = viewModel.VisibleFiles.First();
        Assert.Equal("bad.mkv", expanded.FileName);
        expanded.IsExpanded = true;

        viewModel.SelectedFilter = viewModel.FilterOptions.Single(o => o.Filter == FileResultFilter.Updated);
        Assert.False(Assert.Single(viewModel.VisibleFiles).IsExpanded);
        viewModel.SelectedRun = viewModel.Runs[1];
        Assert.False(Assert.Single(viewModel.VisibleFiles).IsExpanded);
        viewModel.SelectedRun = originalRun;
        viewModel.SelectedFilter = viewModel.FilterOptions.Single(o => o.Filter == FileResultFilter.Failed);

        Assert.Same(expanded, Assert.Single(viewModel.VisibleFiles));
        Assert.True(expanded.IsExpanded);
    }

    [Fact]
    public async Task InterruptedReports_AreNotShownAsCanceled()
    {
        var source = new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "file.mkv");
        var pending = FileRunRecord.From(new FileProcessingReport { SourceFile = source, Status = ProcessingStatus.Pending });
        var running = FileRunRecord.From(new FileProcessingReport { SourceFile = source, Status = ProcessingStatus.Running });
        await _history.AddAsync(Run(pending, running, File("canceled.mkv", ProcessingStatus.Canceled)));
        var viewModel = new BatchResultsViewModel(_history);

        Assert.Equal(ProcessingStatus.Pending, pending.Status);
        Assert.Equal(ProcessingStatus.Running, running.Status);
        Assert.Equal(BatchOutcome.Interrupted, viewModel.Outcome);
        Assert.Equal(1, viewModel.Counts.Canceled);
        viewModel.SelectedFilter = viewModel.FilterOptions.Single(o => o.Filter == FileResultFilter.Interrupted);
        Assert.Equal(2, viewModel.VisibleFiles.Count);
        Assert.All(viewModel.VisibleFiles, row => Assert.True(row.HasNote));
    }

    [Fact]
    public async Task Timing_SpansFromFirstStartToLastFinish()
    {
        await _history.AddAsync(Run(
            File("second.mkv", ProcessingStatus.Succeeded, startSeconds: 4, durationSeconds: 3),
            File("first.mkv", ProcessingStatus.Succeeded, startSeconds: 0, durationSeconds: 4),
            File("canceled.mkv", ProcessingStatus.Canceled) with { StartedAt = null, FinishedAt = null, Duration = null }));

        var viewModel = new BatchResultsViewModel(_history);

        Assert.Equal(Start, viewModel.StartedAt);
        Assert.Equal(TimeSpan.FromSeconds(7), viewModel.Elapsed);
    }

    [Fact]
    public async Task HasUnsavedRuns_IsSetWhileARunIsNotSaved_UntilTheNextBatchSavesIt()
    {
        var viewModel = new BatchResultsViewModel(_history);
        Directory.CreateDirectory(_historyPath + ".tmp");

        await _history.AddAsync(Run(File("unsaved.mkv", ProcessingStatus.Succeeded)));
        Assert.True(viewModel.HasUnsavedRuns);

        Directory.Delete(_historyPath + ".tmp");
        await _history.AddAsync(Run(File("next.mkv", ProcessingStatus.Succeeded)));
        Assert.False(viewModel.HasUnsavedRuns);
        Assert.Equal(2, viewModel.Runs.Count);
    }

    [Theory]
    [InlineData(BatchOutcome.Canceled, ProcessingStatus.Failed, ProcessingStatus.Canceled)]
    [InlineData(BatchOutcome.Failures, ProcessingStatus.Failed, ProcessingStatus.SucceededWithWarnings)]
    [InlineData(BatchOutcome.Warnings, ProcessingStatus.SucceededWithWarnings, ProcessingStatus.Skipped)]
    [InlineData(BatchOutcome.Succeeded, ProcessingStatus.Succeeded, ProcessingStatus.Skipped)]
    [InlineData(BatchOutcome.NoChanges, ProcessingStatus.Skipped, ProcessingStatus.Skipped)]
    public void Outcome_PrefersCancellationThenFailuresThenWarnings(BatchOutcome expected, params ProcessingStatus[] statuses)
    {
        Assert.Equal(expected, BatchResultCounts.From(statuses).Outcome);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public async Task RemoveRun_SelectsTheNextOlderRun_OrThePreviousRunAtTheEnd(int removedIndex, int nextIndex)
    {
        for (var index = 0; index < 3; index++)
        {
            await _history.AddAsync(Run(File($"{index}.mkv", ProcessingStatus.Succeeded)));
        }
        var viewModel = new BatchResultsViewModel(_history);
        var next = viewModel.Runs[nextIndex];
        next.Files[0].IsExpanded = true;
        viewModel.SelectedRun = viewModel.Runs[removedIndex];
        var removed = viewModel.SelectedRun;

        var result = await viewModel.RemoveHistoryAsync(new([removed.Run.Id]));

        Assert.Equal(2, viewModel.Runs.Count);
        Assert.DoesNotContain(removed, viewModel.Runs);
        Assert.Same(next, viewModel.SelectedRun);
        Assert.True(Assert.Single(viewModel.VisibleFiles).IsExpanded);
        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 1), result);
    }

    [Fact]
    public async Task RemoveRun_LastRun_ShowsEmptyPage()
    {
        await _history.AddAsync(Run(File("file.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);

        var result = await viewModel.RemoveHistoryAsync(new([viewModel.SelectedRun!.Run.Id]));

        Assert.Empty(viewModel.Runs);
        Assert.Null(viewModel.SelectedRun);
        Assert.Empty(viewModel.VisibleFiles);
        Assert.Empty(viewModel.FilterOptions);
        Assert.True(viewModel.HasNoBatch);
        Assert.Equal(HistoryRemovalStatus.Removed, result.Status);
    }

    [Fact]
    public async Task RemoveRun_WhenWriteFails_KeepsSelectionAndFilter_AndAllowsRetry()
    {
        await _history.AddAsync(Run(File("old.mkv", ProcessingStatus.Succeeded)));
        await _history.AddAsync(Run(File("failed.mkv", ProcessingStatus.Failed), File("updated.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        var selected = viewModel.SelectedRun;
        viewModel.SelectedFilter = viewModel.FilterOptions.Single(option => option.Filter == FileResultFilter.Failed);
        var expanded = Assert.Single(viewModel.VisibleFiles);
        expanded.IsExpanded = true;
        var blockedPath = Directory.CreateDirectory(_historyPath + ".tmp");

        var removal = new HistoryRemoval([selected!.Run.Id]);
        var result = await viewModel.RemoveHistoryAsync(removal);

        Assert.Equal(HistoryRemovalStatus.Failed, result.Status);
        Assert.Same(selected, viewModel.SelectedRun);
        Assert.Same(expanded, Assert.Single(viewModel.VisibleFiles));
        Assert.True(expanded.IsExpanded);
        blockedPath.Delete();

        result = await viewModel.RemoveHistoryAsync(removal);

        Assert.Equal(HistoryRemovalStatus.Removed, result.Status);
        Assert.Equal(FileResultFilter.All, viewModel.SelectedFilter!.Filter);
        Assert.Equal("old.mkv", Assert.Single(viewModel.VisibleFiles).FileName);
    }

    [Fact]
    public async Task PickerLabels_DisambiguateMatchingLocalMinutesAndOutcomes_UntilTheCollisionIsGone()
    {
        var first = Run(File("one.mkv", ProcessingStatus.Succeeded)) with { Id = Guid.Parse("abcde100-0000-0000-0000-000000000000") };
        var second = Run(File("two.mkv", ProcessingStatus.Succeeded, startSeconds: 59)) with { Id = Guid.Parse("abcde200-0000-0000-0000-000000000000") };
        var different = Run(File("failed.mkv", ProcessingStatus.Failed));
        await _history.AddAsync(first);
        await _history.AddAsync(different);
        var viewModel = new BatchResultsViewModel(_history);
        var original = viewModel.Runs.Single(run => run.Run.Id == first.Id);
        Assert.Null(original.PickerId);

        await _history.AddAsync(second);

        var added = viewModel.Runs.Single(run => run.Run.Id == second.Id);
        Assert.NotNull(original.PickerId);
        Assert.NotNull(added.PickerId);
        Assert.NotEqual(original.PickerId, added.PickerId);
        Assert.StartsWith(original.PickerId, first.Id.ToString("N"));
        Assert.StartsWith(added.PickerId, second.Id.ToString("N"));
        Assert.Null(viewModel.Runs.Single(run => run.Run.Id == different.Id).PickerId);

        await viewModel.RemoveHistoryAsync(new([second.Id]));
        Assert.Null(original.PickerId);
    }

    [Fact]
    public async Task BulkRemoval_KeepsTheViewedRunFilterAndExpansion_WhenCheckingOtherRuns()
    {
        await _history.AddAsync(Run(File("older.mkv", ProcessingStatus.Succeeded)));
        await _history.AddAsync(Run(File("viewed.mkv", ProcessingStatus.Failed), File("other.mkv", ProcessingStatus.Succeeded)));
        await _history.AddAsync(Run(File("newer.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        var viewed = viewModel.Runs[1];
        viewModel.SelectedRun = viewed;
        viewModel.SelectedFilter = viewModel.FilterOptions.Single(option => option.Filter == FileResultFilter.Failed);
        var row = Assert.Single(viewModel.VisibleFiles);
        row.IsExpanded = true;

        await viewModel.RemoveHistoryAsync(new([viewModel.Runs[0].Run.Id, viewModel.Runs[2].Run.Id]));

        Assert.Same(viewed, Assert.Single(viewModel.Runs));
        Assert.Same(viewed, viewModel.SelectedRun);
        Assert.Equal(FileResultFilter.Failed, viewModel.SelectedFilter.Filter);
        Assert.Same(row, Assert.Single(viewModel.VisibleFiles));
        Assert.True(row.IsExpanded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HistoryMutation_PreventsOverlappingChanges_UntilStorageCompletes(bool resetUnreadable)
    {
        await _history.AddAsync(Run(File("file.mkv", ProcessingStatus.Succeeded)));
        var history = Substitute.For<IBatchHistoryService>();
        history.Runs.Returns(_history.Runs);
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        history.ResetUnreadableHistoryAsync().Returns(async _ =>
        {
            await completion.Task;
            return new HistoryRecoveryResult(HistoryRecoveryStatus.Recovered);
        });
        history.RemoveAsync(Arg.Any<HistoryRemoval>()).Returns(async _ =>
        {
            await completion.Task;
            return new HistoryRemovalResult(HistoryRemovalStatus.Removed, 1);
        });
        var viewModel = new BatchResultsViewModel(history);

        var removal = new HistoryRemoval([viewModel.Runs[0].Run.Id]);
        Task operation = resetUnreadable ? viewModel.ResetUnreadableHistoryAsync() : viewModel.RemoveHistoryAsync(removal);

        Assert.True(viewModel.IsChangingHistory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.RemoveHistoryAsync(removal));
        await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.ResetUnreadableHistoryAsync());
        completion.SetResult(true);
        await operation;
        Assert.False(viewModel.IsChangingHistory);
    }

    private static BatchRunRecord Run(params FileRunRecord[] files) => new(Guid.NewGuid(), Start, files);

    private static FileRunRecord File(string path, ProcessingStatus status, int startSeconds = 0, int durationSeconds = 1) => new(
        path,
        status,
        Start.AddSeconds(startSeconds),
        Start.AddSeconds(startSeconds + durationSeconds),
        TimeSpan.FromSeconds(durationSeconds),
        null,
        Array.Empty<string>(),
        Array.Empty<string>());
}
