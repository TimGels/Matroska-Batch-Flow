using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// The overall result of a batch, which decides the headline on the Results page.
/// </summary>
public enum BatchOutcome
{
    /// <summary>
    /// No batch has run yet.
    /// </summary>
    None,

    /// <summary>
    /// Files are still waiting or being processed.
    /// </summary>
    InProgress,

    /// <summary>
    /// Every file was updated or needed no changes, and at least one file was updated.
    /// </summary>
    Succeeded,

    /// <summary>
    /// No file needed changes, so mkvpropedit did not run.
    /// </summary>
    NoChanges,

    /// <summary>
    /// All files were updated, but mkvpropedit reported warnings for some of them.
    /// </summary>
    Warnings,

    /// <summary>
    /// At least one file could not be updated.
    /// </summary>
    Failures,

    /// <summary>
    /// The batch was canceled before every file was processed.
    /// </summary>
    Canceled
}

/// <summary>
/// The number of files in a batch per result.
/// </summary>
/// <param name="Total">All files in the batch.</param>
/// <param name="Updated">Files that mkvpropedit updated, including those with warnings.</param>
/// <param name="Warnings">Updated files for which mkvpropedit reported warnings.</param>
/// <param name="Failed">Files that could not be updated.</param>
/// <param name="Unchanged">Files that needed no changes, so mkvpropedit did not run for them.</param>
/// <param name="Canceled">Files that were not processed because the batch was canceled.</param>
/// <param name="Remaining">Files that are still waiting or being processed.</param>
public readonly record struct BatchResultCounts(
    int Total, int Updated, int Warnings, int Failed, int Unchanged, int Canceled, int Remaining)
{
    /// <summary>
    /// Gets the number of files that finished: updated, failed or unchanged.
    /// </summary>
    public int Finished => Updated + Failed + Unchanged;

    /// <summary>
    /// Gets the overall result. A cancellation takes precedence over failures, failures over warnings.
    /// </summary>
    public BatchOutcome Outcome => this switch
    {
        { Total: 0 } => BatchOutcome.None,
        { Remaining: > 0 } => BatchOutcome.InProgress,
        { Canceled: > 0 } => BatchOutcome.Canceled,
        { Failed: > 0 } => BatchOutcome.Failures,
        { Warnings: > 0 } => BatchOutcome.Warnings,
        { Updated: 0 } => BatchOutcome.NoChanges,
        _ => BatchOutcome.Succeeded
    };

    /// <summary>
    /// Counts the files in a batch by their processing status.
    /// </summary>
    /// <param name="reports">The reports of the files in the batch.</param>
    /// <returns>The counts.</returns>
    public static BatchResultCounts From(IEnumerable<FileProcessingReport> reports)
    {
        int total = 0, succeeded = 0, warnings = 0, failed = 0, unchanged = 0, canceled = 0, remaining = 0;

        foreach (var report in reports)
        {
            total++;
            switch (report.Status)
            {
                case ProcessingStatus.Succeeded:
                    succeeded++;
                    break;
                case ProcessingStatus.SucceededWithWarnings:
                    warnings++;
                    break;
                case ProcessingStatus.Failed:
                    failed++;
                    break;
                case ProcessingStatus.Skipped:
                    unchanged++;
                    break;
                case ProcessingStatus.Canceled:
                    canceled++;
                    break;
                default:
                    remaining++;
                    break;
            }
        }

        return new BatchResultCounts(total, succeeded + warnings, warnings, failed, unchanged, canceled, remaining);
    }

    /// <summary>
    /// Gets the number of files that a filter shows.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The number of files.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="filter"/> is unknown.</exception>
    public int CountFor(FileResultFilter filter) => filter switch
    {
        FileResultFilter.All => Total,
        FileResultFilter.Failed => Failed,
        FileResultFilter.Warnings => Warnings,
        FileResultFilter.Updated => Updated,
        FileResultFilter.Unchanged => Unchanged,
        FileResultFilter.Canceled => Canceled,
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown file result filter.")
    };
}

/// <summary>
/// Selects which files the Results page lists.
/// </summary>
public enum FileResultFilter
{
    All,
    Failed,
    Warnings,
    Updated,
    Unchanged,
    Canceled
}

/// <summary>
/// A filter that the user can choose on the Results page, with the number of files it shows.
/// </summary>
/// <param name="Filter">The filter.</param>
/// <param name="Count">The number of files the filter shows.</param>
public sealed record FileResultFilterOption(FileResultFilter Filter, int Count);

/// <summary>
/// The result of one file on the Results page.
/// </summary>
/// <param name="Report">The processing report of the file.</param>
public sealed record FileResultItem(FileProcessingReport Report)
{
    /// <summary>
    /// Gets the full path of the file.
    /// </summary>
    public string FilePath => Report.Path;

    /// <summary>
    /// Gets the file name with its extension, without the directory.
    /// </summary>
    public string FileName => Path.GetFileName(Report.Path);

    /// <summary>
    /// Gets the directory that contains the file, or an empty string when the path has none.
    /// </summary>
    public string Directory => Path.GetDirectoryName(Report.Path) ?? string.Empty;

    /// <summary>
    /// Gets the processing status of the file.
    /// </summary>
    public ProcessingStatus Status => Report.Status;

    /// <summary>
    /// Gets how long processing the file took, or <see langword="null"/> when it was not started.
    /// </summary>
    public TimeSpan? Duration => Report.Duration;

    /// <summary>
    /// Gets the errors as reported by mkvpropedit or the exception, shown verbatim.
    /// </summary>
    public IReadOnlyList<string> Errors => Report.Errors;

    /// <summary>
    /// Gets the warnings as reported by mkvpropedit, shown verbatim.
    /// </summary>
    public IReadOnlyList<string> Warnings => Report.Warnings;

    /// <summary>
    /// Gets the mkvpropedit command line that ran for the file, or <see langword="null"/> when mkvpropedit did not run.
    /// </summary>
    public string? Command => string.IsNullOrWhiteSpace(Report.ExecutedCommand) ? null : Report.ExecutedCommand;

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasCommand => Command is not null;

    /// <summary>
    /// Gets whether the row explains why mkvpropedit did not run: the file needed no changes or the batch was canceled.
    /// </summary>
    public bool HasNote => IsUnchanged || IsCanceled;

    public bool IsUpdated => Status == ProcessingStatus.Succeeded;

    public bool IsUpdatedWithWarnings => Status == ProcessingStatus.SucceededWithWarnings;

    public bool IsFailed => Status == ProcessingStatus.Failed;

    public bool IsUnchanged => Status == ProcessingStatus.Skipped;

    public bool IsCanceled => Status == ProcessingStatus.Canceled;

    public bool IsUnfinished => Status is ProcessingStatus.Pending or ProcessingStatus.Running;

    /// <summary>
    /// Gets the position of the file in the list: files that need attention come first.
    /// </summary>
    public int SortRank => Status switch
    {
        ProcessingStatus.Failed => 0,
        ProcessingStatus.SucceededWithWarnings => 1,
        ProcessingStatus.Canceled => 2,
        ProcessingStatus.Running => 3,
        ProcessingStatus.Pending => 4,
        ProcessingStatus.Succeeded => 5,
        _ => 6
    };

    /// <summary>
    /// Gets whether a filter shows this file.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns><see langword="true"/> if the filter shows the file; otherwise, <see langword="false"/>.</returns>
    public bool Matches(FileResultFilter filter) => filter switch
    {
        FileResultFilter.All => true,
        FileResultFilter.Failed => IsFailed,
        FileResultFilter.Warnings => IsUpdatedWithWarnings,
        FileResultFilter.Updated => IsUpdated || IsUpdatedWithWarnings,
        FileResultFilter.Unchanged => IsUnchanged,
        FileResultFilter.Canceled => IsCanceled,
        _ => false
    };
}
