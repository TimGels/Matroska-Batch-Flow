using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Uno.Models;
using static MatroskaBatchFlow.Uno.Presentation.UiText;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Provides the localized text that the Results page shows through <c>x:Bind</c> function bindings.
/// </summary>
/// <remarks>
/// Every phrase is one complete resource string. Errors and warnings from mkvpropedit are shown verbatim.
/// </remarks>
public static class ResultsText
{
    /// <summary>
    /// Gets the headline that summarizes the batch.
    /// </summary>
    /// <param name="counts">The number of files per result.</param>
    /// <returns>The headline, for example "Batch completed" or "2 files could not be updated", or an empty string before the first batch.</returns>
    public static string Headline(BatchResultCounts counts) => counts.Outcome switch
    {
        BatchOutcome.None => string.Empty,
        BatchOutcome.Succeeded => Get("ResultsSucceededTitle"),
        BatchOutcome.NoChanges => Get("ResultsNoChangesTitle"),
        BatchOutcome.Warnings => Get("ResultsWarningsTitle"),
        BatchOutcome.Failures => Counted("ResultsFailuresTitle", counts.Failed),
        BatchOutcome.Canceled => Get("ResultsCanceledTitle"),
        BatchOutcome.Interrupted => Get("ResultsInterruptedTitle"),
        _ => throw new ArgumentOutOfRangeException(nameof(counts), counts.Outcome, "Unknown batch outcome.")
    };

    /// <summary>
    /// Gets the explanation below the headline, which tells the user what happened and where to look.
    /// </summary>
    /// <param name="counts">The number of files per result.</param>
    /// <returns>The explanation, or an empty string before the first batch.</returns>
    public static string Message(BatchResultCounts counts) => counts.Outcome switch
    {
        BatchOutcome.None => string.Empty,
        BatchOutcome.Succeeded when counts.Unchanged == 0 => Counted("ResultsSucceededAll", counts.Updated),
        BatchOutcome.Succeeded => Format(CountedKey("ResultsSucceededSome", counts.Updated), counts.Updated, counts.Total),
        BatchOutcome.NoChanges => Counted("ResultsNoChangesMessage", counts.Total),
        BatchOutcome.Warnings => Counted("ResultsWarningsMessage", counts.Warnings),
        BatchOutcome.Failures => Counted("ResultsFailuresMessage", counts.Failed),
        BatchOutcome.Canceled => Counted("ResultsCanceledMessage", counts.Canceled),
        BatchOutcome.Interrupted => Counted("ResultsInterruptedMessage", counts.Interrupted),
        _ => throw new ArgumentOutOfRangeException(nameof(counts), counts.Outcome, "Unknown batch outcome.")
    };

    /// <summary>
    /// Gets when the batch started and how long it took.
    /// </summary>
    /// <param name="startedAt">When the first file started, or <see langword="null"/>.</param>
    /// <param name="elapsed">How long the batch took, or <see langword="null"/>.</param>
    /// <returns>The timing, for example "Started 23-11-2026 19:40 · Took 1 min 12 s", or an empty string when no file started.</returns>
    public static string Timing(DateTimeOffset? startedAt, TimeSpan? elapsed) => startedAt switch
    {
        null => string.Empty,
        { } start when elapsed is { } duration => Format("ResultsTiming", start.LocalDateTime, Duration(duration)),
        { } start => Format("ResultsStarted", start.LocalDateTime)
    };

    /// <summary>
    /// Gets a duration in seconds, minutes or hours, depending on its length.
    /// </summary>
    /// <param name="duration">The duration, or <see langword="null"/>.</param>
    /// <returns>The duration, for example "0.4 s", "2 min 5 s" or "1 h 3 min", or an empty string for <see langword="null"/>.</returns>
    public static string Duration(TimeSpan? duration) => duration switch
    {
        null => string.Empty,
        { TotalSeconds: < 0.1 } => Get("ResultsDurationInstant"),
        { TotalMinutes: < 1 } d => Format("ResultsDurationSeconds", d.TotalSeconds),
        { TotalHours: < 1 } d => Format("ResultsDurationMinutes", (int)d.TotalMinutes, d.Seconds),
        { } d => Format("ResultsDurationHours", (int)d.TotalHours, d.Minutes)
    };

    /// <summary>
    /// Gets the label of a saved run in the run picker.
    /// </summary>
    /// <param name="when">When the run started.</param>
    /// <param name="counts">The number of files per result in the run.</param>
    /// <param name="pickerId">A distinguishing run ID prefix, or null when the label is already unique.</param>
    /// <returns>The label, for example "23-11-2026 19:40 · Batch completed".</returns>
    public static string RunLabel(DateTimeOffset when, BatchResultCounts counts, string? pickerId)
    {
        var label = Format("ResultsRunLabel", when.LocalDateTime, Headline(counts));
        return pickerId is null ? label : Format("ResultsRunLabelWithId", label, pickerId);
    }

    public static string PickerLabel(BatchRunSummary? run) =>
        run?.PickerLabel ?? Get("ResultsPickerEmpty");

    public static string PickerName(BatchRunSummary? run) => Format("ResultsPickerNameWithValue", PickerLabel(run));

    public static string RemoveRunName(string label) => Format("ResultsRemoveNamedRun", label);

    /// <summary>
    /// Gets the result of a file.
    /// </summary>
    /// <param name="status">The processing status of the file.</param>
    /// <returns>The result, for example "Updated" or "No changes needed".</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is unknown.</exception>
    public static string Status(ProcessingStatus status) => status switch
    {
        ProcessingStatus.Pending => Get("ResultsStatusPending"),
        ProcessingStatus.Running => Get("ResultsStatusRunning"),
        ProcessingStatus.Succeeded => Get("ResultsStatusUpdated"),
        ProcessingStatus.SucceededWithWarnings => Get("ResultsStatusWarnings"),
        ProcessingStatus.Failed => Get("ResultsStatusFailed"),
        ProcessingStatus.Skipped => Get("ResultsStatusUnchanged"),
        ProcessingStatus.Canceled => Get("ResultsStatusCanceled"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown processing status.")
    };

    /// <summary>
    /// Gets why processing was skipped or did not finish.
    /// </summary>
    /// <param name="status">The processing status of the file.</param>
    /// <returns>The explanation for a skipped, canceled or interrupted file; otherwise, an empty string.</returns>
    public static string Note(ProcessingStatus status) => status switch
    {
        ProcessingStatus.Skipped => Get("ResultsNoteUnchanged"),
        ProcessingStatus.Canceled => Get("ResultsNoteCanceled"),
        ProcessingStatus.Pending => Get("ResultsNotePending"),
        ProcessingStatus.Running => Get("ResultsNoteRunning"),
        _ => string.Empty
    };

    /// <summary>
    /// Gets the screen reader name of a file's result row.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="status">The processing status of the file.</param>
    /// <returns>The accessible name, for example "movie.mkv, Updated".</returns>
    public static string RowName(string fileName, ProcessingStatus status) => Format("ResultsRowName", fileName, Status(status));

    /// <summary>
    /// Gets the label of a filter option with its number of files.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="count">The number of files the filter shows.</param>
    /// <returns>The label, for example "Failed (2)".</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="filter"/> is unknown.</exception>
    public static string Filter(FileResultFilter filter, int count) => filter switch
    {
        FileResultFilter.All => Format("ResultsFilterAll", count),
        FileResultFilter.Failed => Format("ResultsFilterFailed", count),
        FileResultFilter.Warnings => Format("ResultsFilterWarnings", count),
        FileResultFilter.Updated => Format("ResultsFilterUpdated", count),
        FileResultFilter.Unchanged => Format("ResultsFilterUnchanged", count),
        FileResultFilter.Canceled => Format("ResultsFilterCanceled", count),
        FileResultFilter.Interrupted => Format("ResultsFilterInterrupted", count),
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown file result filter.")
    };
}
