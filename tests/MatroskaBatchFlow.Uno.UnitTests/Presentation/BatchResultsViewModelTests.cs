using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

public class BatchResultsViewModelTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 23, 19, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBatchReportStore _store = new();

    [Fact]
    public void FinishedBatch_ListsFilesThatNeedAttentionFirst()
    {
        var viewModel = new BatchResultsViewModel(_store);
        Assert.Equal(BatchOutcome.None, viewModel.Outcome);

        StartBatch(
            Report("updated.mkv", ProcessingStatus.Succeeded),
            Report("unchanged.mkv", ProcessingStatus.Skipped),
            Report("failed.mkv", ProcessingStatus.Failed),
            Report("warned.mkv", ProcessingStatus.SucceededWithWarnings));

        Assert.Equal(BatchOutcome.Failures, viewModel.Outcome);
        Assert.Equal(new BatchResultCounts(4, 2, 1, 1, 1, 0, 0), viewModel.Counts);
        Assert.Equal(["failed.mkv", "warned.mkv", "updated.mkv", "unchanged.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));
        Assert.Equal(
            [FileResultFilter.All, FileResultFilter.Failed, FileResultFilter.Warnings, FileResultFilter.Updated, FileResultFilter.Unchanged],
            viewModel.FilterOptions.Select(o => o.Filter));
    }

    [Fact]
    public void RunningBatch_BuildsListWhenLastFileFinishes()
    {
        var viewModel = new BatchResultsViewModel(_store);
        var first = Report("first.mkv", ProcessingStatus.Pending);
        var second = Report("second.mkv", ProcessingStatus.Pending);
        StartBatch(first, second);

        first.Status = ProcessingStatus.Succeeded;
        second.Status = ProcessingStatus.Running;

        Assert.Equal(BatchOutcome.InProgress, viewModel.Outcome);
        Assert.Empty(viewModel.VisibleFiles);

        second.Status = ProcessingStatus.Succeeded;

        Assert.Equal(BatchOutcome.Succeeded, viewModel.Outcome);
        Assert.Equal(2, viewModel.VisibleFiles.Count);
    }

    [Fact]
    public void Filter_ShowsMatchingFilesAndFallsBackToAllWhenNextBatchHasNone()
    {
        var viewModel = new BatchResultsViewModel(_store);
        StartBatch(Report("ok.mkv", ProcessingStatus.Succeeded), Report("bad.mkv", ProcessingStatus.Failed));

        viewModel.SelectedFilter = viewModel.FilterOptions.Single(o => o.Filter == FileResultFilter.Failed);
        Assert.Equal(["bad.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));

        StartBatch(Report("next.mkv", ProcessingStatus.Succeeded), Report("other.mkv", ProcessingStatus.Skipped));

        Assert.Equal(FileResultFilter.All, viewModel.SelectedFilter!.Filter);
        Assert.Equal(["next.mkv", "other.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));
    }

    [Fact]
    public void NextBatchWithSameCounts_ListsItsOwnFiles()
    {
        var viewModel = new BatchResultsViewModel(_store);
        StartBatch(Report("first.mkv", ProcessingStatus.Succeeded));

        StartBatch(Report("second.mkv", ProcessingStatus.Succeeded));

        Assert.Equal(["second.mkv"], viewModel.VisibleFiles.Select(f => f.FileName));
    }

    [Fact]
    public void Timing_SpansFromFirstStartToLastFinish()
    {
        var viewModel = new BatchResultsViewModel(_store);
        var notStarted = Report("canceled.mkv", ProcessingStatus.Canceled);
        notStarted.StartedAt = null;
        notStarted.FinishedAt = null;

        StartBatch(
            Report("second.mkv", ProcessingStatus.Succeeded, startSeconds: 4, durationSeconds: 3),
            Report("first.mkv", ProcessingStatus.Succeeded, startSeconds: 0, durationSeconds: 4),
            notStarted);

        Assert.Equal(Start, viewModel.StartedAt);
        Assert.Equal(TimeSpan.FromSeconds(7), viewModel.Elapsed);
    }

    [Theory]
    [InlineData(BatchOutcome.Canceled, ProcessingStatus.Failed, ProcessingStatus.Canceled)]
    [InlineData(BatchOutcome.Failures, ProcessingStatus.Failed, ProcessingStatus.SucceededWithWarnings)]
    [InlineData(BatchOutcome.Warnings, ProcessingStatus.SucceededWithWarnings, ProcessingStatus.Skipped)]
    [InlineData(BatchOutcome.Succeeded, ProcessingStatus.Succeeded, ProcessingStatus.Skipped)]
    [InlineData(BatchOutcome.NoChanges, ProcessingStatus.Skipped, ProcessingStatus.Skipped)]
    [InlineData(BatchOutcome.InProgress, ProcessingStatus.Failed, ProcessingStatus.Pending)]
    public void Outcome_PrefersCancellationThenFailuresThenWarnings(BatchOutcome expected, params ProcessingStatus[] statuses)
    {
        var counts = BatchResultCounts.From(statuses.Select((status, i) => Report($"{i}.mkv", status)));

        Assert.Equal(expected, counts.Outcome);
    }

    private void StartBatch(params FileProcessingReport[] reports)
    {
        _store.Reset();
        var batch = _store.CreateBatch();
        _store.SetActiveBatch(batch);
        batch.AddFileReports(reports);
    }

    private static FileProcessingReport Report(string path, ProcessingStatus status, int startSeconds = 0, int durationSeconds = 1) => new()
    {
        SourceFile = new ScannedFileInfo(new MediaInfoResultBuilder().Build(), path),
        Path = path,
        Status = status,
        StartedAt = Start.AddSeconds(startSeconds),
        FinishedAt = Start.AddSeconds(startSeconds + durationSeconds),
        Duration = TimeSpan.FromSeconds(durationSeconds)
    };
}
