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
    Canceled,

    /// <summary>
    /// Processing stopped unexpectedly before every file reached a final status.
    /// </summary>
    Interrupted
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
/// <param name="Interrupted">Files that did not start or finish before the batch stopped unexpectedly.</param>
public readonly record struct BatchResultCounts(
    int Total, int Updated, int Warnings, int Failed, int Unchanged, int Canceled, int Interrupted)
{
    /// <summary>
    /// Gets the overall result. An interruption takes precedence over cancellation, failures and warnings.
    /// </summary>
    public BatchOutcome Outcome => this switch
    {
        { Total: 0 } => BatchOutcome.None,
        { Interrupted: > 0 } => BatchOutcome.Interrupted,
        { Canceled: > 0 } => BatchOutcome.Canceled,
        { Failed: > 0 } => BatchOutcome.Failures,
        { Warnings: > 0 } => BatchOutcome.Warnings,
        { Updated: 0 } => BatchOutcome.NoChanges,
        _ => BatchOutcome.Succeeded
    };

    /// <summary>
    /// Counts the files of a finished batch by their processing status.
    /// </summary>
    /// <param name="statuses">The processing status of every file in the batch.</param>
    /// <returns>The counts. Files that stayed pending or running count as interrupted.</returns>
    public static BatchResultCounts From(IEnumerable<ProcessingStatus> statuses)
    {
        int total = 0, succeeded = 0, warnings = 0, failed = 0, unchanged = 0, canceled = 0, interrupted = 0;

        foreach (var status in statuses)
        {
            total++;
            switch (status)
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
                    interrupted++;
                    break;
            }
        }

        return new BatchResultCounts(total, succeeded + warnings, warnings, failed, unchanged, canceled, interrupted);
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
        FileResultFilter.Interrupted => Interrupted,
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
    Canceled,
    Interrupted
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
public sealed partial class FileResultItem(FileRunRecord record) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>
    /// Gets the full path of the file.
    /// </summary>
    public string FilePath => record.Path;

    /// <summary>
    /// Gets the file name with its extension, without the directory.
    /// </summary>
    public string FileName => Path.GetFileName(record.Path);

    /// <summary>
    /// Gets the directory that contains the file, or an empty string when the path has none.
    /// </summary>
    public string Directory => Path.GetDirectoryName(record.Path) ?? string.Empty;

    /// <summary>
    /// Gets the processing status of the file.
    /// </summary>
    public ProcessingStatus Status => record.Status;

    /// <summary>
    /// Gets how long processing the file took, or <see langword="null"/> when it was not started.
    /// </summary>
    public TimeSpan? Duration => record.Duration;

    /// <summary>
    /// Gets the errors as reported by mkvpropedit or the exception, shown verbatim.
    /// </summary>
    public IReadOnlyList<string> Errors => record.Errors;

    /// <summary>
    /// Gets the warnings as reported by mkvpropedit, shown verbatim.
    /// </summary>
    public IReadOnlyList<string> Warnings => record.Warnings;

    /// <summary>
    /// Gets the display-only mkvpropedit command line, if available.
    /// </summary>
    public string? Command => record.Command;

    public bool HasErrors => Errors.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasCommand => Command is not null;

    /// <summary>
    /// Gets whether the row explains why processing was skipped or did not finish.
    /// </summary>
    public bool HasNote => IsUnchanged || IsCanceled || IsInterrupted;

    public bool IsUpdated => Status == ProcessingStatus.Succeeded;

    public bool IsUpdatedWithWarnings => Status == ProcessingStatus.SucceededWithWarnings;

    public bool IsFailed => Status == ProcessingStatus.Failed;

    public bool IsUnchanged => Status == ProcessingStatus.Skipped;

    public bool IsCanceled => Status == ProcessingStatus.Canceled;

    public bool IsInterrupted => Status is ProcessingStatus.Pending or ProcessingStatus.Running;

    /// <summary>
    /// Gets the position of the file in the list: files that need attention come first.
    /// </summary>
    public int SortRank => Status switch
    {
        ProcessingStatus.Failed or ProcessingStatus.Pending or ProcessingStatus.Running => 0,
        ProcessingStatus.SucceededWithWarnings => 1,
        ProcessingStatus.Canceled => 2,
        ProcessingStatus.Succeeded => 3,
        _ => 4
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
        FileResultFilter.Interrupted => IsInterrupted,
        _ => false
    };
}
