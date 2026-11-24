using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
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
    public async Task ClearHistory_ShowsEmptyPage()
    {
        await _history.AddAsync(Run(File("file.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        Assert.True(viewModel.ClearHistoryCommand.CanExecute(null));

        await viewModel.ClearHistoryCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Runs);
        Assert.Null(viewModel.SelectedRun);
        Assert.True(viewModel.HasNoBatch);
        Assert.Empty(viewModel.VisibleFiles);
        Assert.False(viewModel.ClearHistoryFailed);
    }

    [Fact]
    public async Task ClearHistory_WhenDeletionFails_KeepsTheRunsAndShowsAnErrorUntilRetry()
    {
        await _history.AddAsync(Run(File("file.mkv", ProcessingStatus.Succeeded)));
        var viewModel = new BatchResultsViewModel(_history);
        // A directory at the temporary file path makes deletion fail on both Windows and Unix.
        var blockedPath = Directory.CreateDirectory(_historyPath + ".tmp");

        await viewModel.ClearHistoryCommand.ExecuteAsync(null);

        Assert.True(viewModel.ClearHistoryFailed);
        Assert.Single(viewModel.Runs);
        Assert.True(System.IO.File.Exists(_historyPath));

        blockedPath.Delete();
        await viewModel.ClearHistoryCommand.ExecuteAsync(null);

        Assert.False(viewModel.ClearHistoryFailed);
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
