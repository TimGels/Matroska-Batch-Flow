using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

public sealed class BatchResultsViewModelTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 23, 19, 0, 0, TimeSpan.Zero);

    private readonly string _directory = Directory.CreateTempSubdirectory("mbf-results-").FullName;
    private readonly BatchHistoryService _history;

    public BatchResultsViewModelTests()
    {
        _history = new BatchHistoryService(Path.Combine(_directory, "BatchHistory.json"), NullLogger<BatchHistoryService>.Instance);
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
        Assert.Equal(new BatchResultCounts(4, 2, 1, 1, 1, 0), viewModel.Counts);
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
        Assert.False(viewModel.ClearHistoryCommand.CanExecute(null));
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
