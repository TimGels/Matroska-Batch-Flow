using System.Text.Json.Serialization;
using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// A finished batch as saved in the batch history.
/// </summary>
/// <remarks>
/// Plain data that is written to disk, so a run can still be shown after the app restarts.
/// </remarks>
/// <param name="Id">The identifier of the batch.</param>
/// <param name="RecordedAt">When the batch finished and was saved.</param>
/// <param name="Files">The result of every file in the batch, in processing order.</param>
public sealed partial record BatchRunRecord(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] DateTimeOffset RecordedAt,
    [property: JsonRequired] IReadOnlyList<FileRunRecord> Files)
{
    /// <summary>
    /// Creates a record of a batch that finished.
    /// </summary>
    /// <param name="batch">The batch.</param>
    /// <param name="recordedAt">When the batch finished.</param>
    /// <returns>The record.</returns>
    public static BatchRunRecord From(BatchExecutionReport batch, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return new BatchRunRecord(batch.Id, recordedAt, batch.FileReports.Select(FileRunRecord.From).ToArray());
    }
}

/// <summary>
/// The saved result of one file in a batch.
/// </summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="Status">The processing status when the batch ended.</param>
/// <param name="StartedAt">When processing the file started, or <see langword="null"/> when it was not started.</param>
/// <param name="FinishedAt">When processing the file finished, or <see langword="null"/> when it was not started.</param>
/// <param name="Duration">How long processing the file took, or <see langword="null"/> when it was not started.</param>
/// <param name="Command">The display-only mkvpropedit command line, if available.</param>
/// <param name="Errors">The errors as reported by mkvpropedit or the exception.</param>
/// <param name="Warnings">The warnings as reported by mkvpropedit.</param>
public sealed record FileRunRecord(
    [property: JsonRequired] string Path,
    [property: JsonRequired] ProcessingStatus Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    TimeSpan? Duration,
    string? Command,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Creates a record of a file's processing report.
    /// </summary>
    /// <remarks>
    /// Pending and running statuses are retained so interrupted processing is distinguishable from cancellation.
    /// </remarks>
    /// <param name="report">The processing report.</param>
    /// <returns>The record.</returns>
    public static FileRunRecord From(FileProcessingReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new FileRunRecord(
            report.Path,
            report.Status,
            report.StartedAt,
            report.FinishedAt,
            report.Duration,
            string.IsNullOrWhiteSpace(report.ExecutedCommand) ? null : report.ExecutedCommand,
            report.Errors.ToArray(),
            report.Warnings.ToArray());
    }
}

/// <summary>
/// A saved run as offered in the run picker of the Results page, with its counts and timing.
/// </summary>
public sealed partial class BatchRunSummary : ObservableObject
{
    private IReadOnlyList<FileResultItem>? _files;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRunSummary"/> class.
    /// </summary>
    /// <param name="run">The saved run.</param>
    public BatchRunSummary(BatchRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        Run = run;
        Counts = BatchResultCounts.From(run.Files.Select(file => file.Status));

        DateTimeOffset? started = null;
        DateTimeOffset? finished = null;
        foreach (var file in run.Files)
        {
            if (file.StartedAt is { } start && (started is null || start < started))
                started = start;

            if (file.FinishedAt is { } end && (finished is null || end > finished))
                finished = end;
        }

        StartedAt = started;
        Elapsed = started is { } from && finished is { } to && to >= from ? to - from : null;
    }

    /// <summary>
    /// Gets the saved run.
    /// </summary>
    public BatchRunRecord Run { get; }

    /// <summary>
    /// Gets the rows for this run, with their expansion state retained while switching runs.
    /// </summary>
    public IReadOnlyList<FileResultItem> Files =>
        _files ??= Run.Files.Select(file => new FileResultItem(file)).OrderBy(item => item.SortRank).ToArray();

    /// <summary>
    /// Gets the number of files per result.
    /// </summary>
    public BatchResultCounts Counts { get; }

    /// <summary>
    /// Gets when the first file started, or <see langword="null"/> when no file started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; }

    /// <summary>
    /// Gets the time from the start of the first file to the end of the last file, or <see langword="null"/> when no file finished.
    /// </summary>
    public TimeSpan? Elapsed { get; }

    /// <summary>
    /// Gets when the run started, or when it was saved if no file started.
    /// </summary>
    public DateTimeOffset When => StartedAt ?? Run.RecordedAt;

    /// <summary>
    /// Gets a prefix of the run ID that tells this run apart from others with the same picker label, or
    /// <see langword="null"/> when the label is unique.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PickerLabel))]
    public partial string? PickerId { get; private set; }

    /// <summary>
    /// Gets the label of this run in the run picker, including <see cref="PickerId"/> when it is set.
    /// </summary>
    public string PickerLabel => ResultsText.RunLabel(When, Counts, PickerId);

    /// <summary>
    /// Sets <see cref="PickerId"/> on runs whose picker labels would otherwise match, using the shortest ID prefix of
    /// at least four characters that is unique within each group.
    /// </summary>
    /// <param name="runs">All runs listed in the picker.</param>
    internal static void DisambiguatePickerLabels(IEnumerable<BatchRunSummary> runs)
    {
        // The picker displays local time to the minute. Only a failure headline includes a count.
        var groups = runs.GroupBy(run => (run.When.LocalDateTime.Ticks / TimeSpan.TicksPerMinute,
            run.Counts.Outcome, run.Counts.Outcome == BatchOutcome.Failures ? run.Counts.Failed : 0));
        foreach (var group in groups)
        {
            var entries = group.Select(run => (Run: run, Id: run.Run.Id.ToString("N"))).ToArray();
            if (entries.Length == 1)
            {
                entries[0].Run.PickerId = null;
                continue;
            }

            var length = 4;
            while (entries.DistinctBy(entry => entry.Id[..length]).Count() != entries.Length)
            {
                length++;
            }
            foreach (var (run, id) in entries)
            {
                run.PickerId = id[..length];
            }
        }
    }
}
