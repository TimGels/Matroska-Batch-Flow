using System.Globalization;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Uno.Models;
using static MatroskaBatchFlow.Uno.Presentation.UiText;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Provides the localized text that the validation views show through <c>x:Bind</c> function bindings.
/// </summary>
/// <remarks>
/// <para>
/// Check identity, row membership and navigation never depend on this text.
/// </para>
/// <para>
/// Every phrase is one complete resource string. Code fills in numbers, file names and scanned values,
/// or joins complete phrases with punctuation, but never inserts a translated word into a sentence.
/// Phrases that name a track type have one string per type, such as <c>ChecksLanguageTitleAudio</c>.
/// </para>
/// </remarks>
public static class ValidationText
{
    // Input page: the checks status line and each file's check button.

    /// <summary>
    /// Gets the checks status line shown above the file list on the Input page.
    /// </summary>
    /// <param name="summary">The summary of file checks.</param>
    /// <param name="updating"><see langword="true"/> to indicate that checks are being updated; otherwise, <see langword="false"/>.</param>
    /// <returns>The status line, for example "Updating file checks..." or "1 unsupported file / 2 blocking checks".</returns>
    public static string InputHeadline(FileCheckSummary summary, bool updating) =>
        updating ? Get("ChecksUpdating") : Headline(summary);

    /// <summary>
    /// Gets the label of the link that opens the Validation page from the Input page.
    /// </summary>
    /// <param name="count">The number of checks to review.</param>
    /// <returns>The link label, for example "View 1 check" or "View all 3 checks".</returns>
    public static string ReviewCount(int count) => Counted("ChecksReview", count);

    /// <summary>
    /// Gets the screen reader name of a file's check button on the Input page.
    /// </summary>
    /// <param name="count">The number of batch checks that involve the file.</param>
    /// <param name="name">The file name.</param>
    /// <returns>The accessible name, for example "Review 2 batch checks involving movie.mkv".</returns>
    public static string ReviewFile(int count, string name) => Format(CountedKey("ChecksReviewFile", count), count, name);

    /// <summary>
    /// Gets the tooltip of a file's check button on the Input page.
    /// </summary>
    /// <param name="topics">The checks that involve the file.</param>
    /// <returns>The title of each check, one per line.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A check's track type is not audio, video or subtitle.</exception>
    public static string TopicList(IReadOnlyList<FileCheckKey> topics) => string.Join(Environment.NewLine, topics.Select(Title));

    // Validation page: header and empty state.

    /// <summary>
    /// Gets the summary line at the top of the Validation page.
    /// </summary>
    /// <param name="summary">The summary of file checks.</param>
    /// <returns>The batch size followed by the findings, for example "3 files in this batch - 1 unsupported file / 2 blocking checks".</returns>
    public static string Summary(FileCheckSummary summary) =>
        Format("ChecksSummary", Counted("ChecksBatchSize", summary.FileCount), Headline(summary));

    /// <summary>
    /// Gets the confirmation shown after unsupported files were removed from the batch.
    /// </summary>
    /// <param name="count">The number of removed files.</param>
    /// <returns>
    /// The confirmation, for example "Removed 2 unsupported files from the batch.",
    /// or an empty string when <paramref name="count"/> is 0.
    /// </returns>
    public static string Removed(int count) => count > 0 ? Counted("ChecksRemoved", count) : string.Empty;

    /// <summary>
    /// Gets the label of the button that clears the file filter on the Validation page.
    /// </summary>
    /// <param name="name">The name of the file that the page is filtered to, or <see langword="null"/> when it is not filtered.</param>
    /// <returns>The label, for example "File: movie.mkv".</returns>
    public static string Scope(string? name) => Format("ChecksFileScope", name);

    /// <summary>
    /// Gets the title shown on the Validation page when there are no checks to review.
    /// </summary>
    /// <param name="count">The number of files in the batch.</param>
    /// <returns>"No files to check" for an empty batch; otherwise, "No issues found by enabled checks".</returns>
    public static string EmptyTitle(int count) => Get(count == 0 ? "ChecksEmptyBatch" : "ChecksClean");

    /// <summary>
    /// Gets the description shown below <see cref="EmptyTitle"/>.
    /// </summary>
    /// <param name="count">The number of files in the batch.</param>
    /// <returns>A prompt to add files for an empty batch; otherwise, an explanation of what the enabled checks cover.</returns>
    public static string EmptyDescription(int count) => Get(count == 0 ? "ChecksAddFiles" : "ChecksCleanDescription");

    // Validation page: the list of checks, grouped under category and family headings.

    /// <summary>
    /// Gets the heading above the blocking or advisory checks in the list.
    /// </summary>
    /// <param name="topic">A check in the category; its severity selects the heading.</param>
    /// <param name="count">The number of checks in the category.</param>
    /// <returns>The heading, for example "Blocking (3)" or "Advisory (1)".</returns>
    public static string CategoryHeading(FileCheckKey topic, int count) =>
        Format(topic.Severity == ValidationSeverity.Error ? "ChecksBlockingHeading" : "ChecksAdvisoryHeading", count);

    /// <summary>
    /// Gets the heading of a group of related checks within a category.
    /// </summary>
    /// <param name="topic">A check in the group.</param>
    /// <returns>
    /// The heading, for example "Track counts" or "Audio language tags",
    /// or an empty string for a check without a track type, such as an unsupported file.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The check's track type is not audio, video or subtitle.</exception>
    public static string Family(FileCheckKey topic)
    {
        if (topic.TrackType is not { } type)
            return string.Empty;
        if (topic.Kind == ValidationCheckKind.TrackCount)
            return Get("ChecksTrackCounts");

        var key = topic.Kind switch
        {
            ValidationCheckKind.Language => "ChecksLanguageFamily",
            ValidationCheckKind.DefaultFlag => "ChecksDefaultFamily",
            ValidationCheckKind.ForcedFlag => "ChecksForcedFamily",
            _ => "ChecksOtherFamily"
        };
        return Get(TrackKey(key, type));
    }

    /// <summary>
    /// Gets the text of a check's entry in the list, shown below its group heading.
    /// </summary>
    /// <param name="topic">The check.</param>
    /// <returns>
    /// The track type below "Track counts", for example "Audio"; the track position below other group headings,
    /// for example "Track 2"; or the full <see cref="Title"/> for a check without a track type.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The check's track type is not audio, video or subtitle.</exception>
    public static string NavigationLabel(FileCheckKey topic)
    {
        if (topic.TrackType is not { } type)
            return Title(topic);

        return topic.Kind == ValidationCheckKind.TrackCount
            ? Get(TrackKey("ChecksTrackCountLabel", type))
            : Format("ChecksTrackPosition", topic.TrackPosition);
    }

    /// <summary>
    /// Gets the name of a check including its category, used by screen readers and by the check picker in narrow windows.
    /// </summary>
    /// <param name="topic">The check.</param>
    /// <returns>The name, for example "Blocking: Audio track 2 · Language tag".</returns>
    /// <exception cref="ArgumentOutOfRangeException">The check's track type is not audio, video or subtitle.</exception>
    public static string Selector(FileCheckKey topic) =>
        Format(topic.Severity == ValidationSeverity.Error ? "ChecksBlockingSelector" : "ChecksAdvisorySelector", Title(topic));

    // Validation page: details of the selected check.

    /// <summary>
    /// Gets the title of a check.
    /// </summary>
    /// <param name="topic">The check, or <see langword="null"/> when no check is selected.</param>
    /// <returns>
    /// The title, for example "Audio track 2 · Language tag", "Subtitle track count" or "Unsupported files",
    /// or an empty string when <paramref name="topic"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The check's track type is not audio, video or subtitle.</exception>
    public static string Title(FileCheckKey? topic)
    {
        if (topic is null)
            return string.Empty;
        if (topic.Kind == ValidationCheckKind.FileFormat)
            return Get("ChecksUnsupportedTitle");
        if (topic.TrackType is not { } type)
            return Get("ChecksDiagnosticTitle");

        var key = topic.Kind switch
        {
            ValidationCheckKind.TrackCount => "ChecksTrackCountTitle",
            ValidationCheckKind.Language => "ChecksLanguageTitle",
            ValidationCheckKind.DefaultFlag => "ChecksDefaultTitle",
            ValidationCheckKind.ForcedFlag => "ChecksForcedTitle",
            _ => "ChecksOtherTitle"
        };
        return Format(TrackKey(key, type), topic.TrackPosition);
    }

    /// <summary>
    /// Gets the explanation of whether a check stops batch edits.
    /// </summary>
    /// <param name="topic">The check, or <see langword="null"/> when no check is selected.</param>
    /// <returns>
    /// The explanation, for example "Blocks batch edits under your current validation settings.",
    /// or an empty string when <paramref name="topic"/> is <see langword="null"/>.
    /// </returns>
    public static string Consequence(FileCheckKey? topic) => topic is null ? string.Empty : Get(
        topic.Kind == ValidationCheckKind.FileFormat ? "ChecksAlwaysBlock" :
        topic.Severity == ValidationSeverity.Error ? "ChecksBlockBySettings" : "ChecksDoNotBlock");

    /// <summary>
    /// Gets the heading above the scanned values of the selected check.
    /// </summary>
    /// <param name="summary">The summary of file checks.</param>
    /// <returns>
    /// The heading, for example "Scanned values · 3 files in this batch",
    /// or "Scanned values · 4 of 5 files supported" when the batch contains unsupported files.
    /// </returns>
    public static string ComparisonHeading(FileCheckSummary summary) => summary.UnsupportedCount == 0
        ? Format("ChecksComparison", Counted("ChecksBatchSize", summary.FileCount))
        : Format("ChecksSupportedComparison", summary.FileCount - summary.UnsupportedCount, summary.FileCount);

    /// <summary>
    /// Gets a scanned value in readable form.
    /// </summary>
    /// <param name="evidence">The value scanned from one file, or <see langword="null"/> when there is none.</param>
    /// <returns>
    /// The value, for example "Yes", "English (eng)" or "2 tracks"; "Not specified" for an empty language tag;
    /// "Track not present" when the file has no such track; a format problem such as "Missing .mkv extension.";
    /// a diagnostic message unchanged; or an empty string when <paramref name="evidence"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The format problem has an unknown kind.</exception>
    public static string Value(FileCheckEvidence? evidence)
    {
        if (evidence is null)
            return string.Empty;
        if (evidence.FormatIssue is { } issue)
        {
            return issue.Kind switch
            {
                UnsupportedFormatKind.MissingExtension => Get("ChecksMissingExtension"),
                UnsupportedFormatKind.WrongExtension => Format("ChecksWrongExtension", issue.Detail),
                UnsupportedFormatKind.UnknownContainer => Get("ChecksUnknownContainer"),
                UnsupportedFormatKind.WrongContainer => Format("ChecksWrongContainer", issue.Detail),
                _ => throw new ArgumentOutOfRangeException(nameof(evidence))
            };
        }
        if (evidence.Diagnostic is { } diagnostic)
            return diagnostic;
        if (evidence.RawValue is not { } raw)
            return Get("ChecksAbsentTrack");

        return evidence.Kind switch
        {
            ValidationCheckKind.Language when string.IsNullOrWhiteSpace(raw) => Get("ChecksUnspecified"),
            ValidationCheckKind.Language when evidence.LanguageName is { } name => Format("ChecksLanguageTag", name, raw),
            ValidationCheckKind.DefaultFlag or ValidationCheckKind.ForcedFlag when bool.TryParse(raw, out bool flag)
                => Get(flag ? "ChecksYes" : "ChecksNo"),
            ValidationCheckKind.TrackCount when int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                => Counted("ChecksTracks", count),
            _ => raw
        };
    }

    /// <summary>
    /// Gets the number of files that share a scanned value.
    /// </summary>
    /// <param name="count">The number of files.</param>
    /// <returns>The count, for example "1 file" or "3 files".</returns>
    public static string FileCount(int count) => Counted("ChecksFiles", count);

    /// <summary>
    /// Gets the screen reader name of a scanned value row.
    /// </summary>
    /// <param name="evidence">The scanned value.</param>
    /// <param name="count">The number of files with this value.</param>
    /// <returns>The accessible name, for example "English (eng), 3 files".</returns>
    /// <exception cref="ArgumentOutOfRangeException">The format problem has an unknown kind.</exception>
    public static string ValueAndCount(FileCheckEvidence evidence, int count) => Format("ChecksValueCount", Value(evidence), FileCount(count));

    /// <summary>
    /// Gets the label of the button that removes the unsupported files from the batch.
    /// </summary>
    /// <param name="count">The number of unsupported files.</param>
    /// <returns>"Remove file from batch" for one file; otherwise, for example "Remove 2 unsupported files from batch".</returns>
    public static string RemoveAction(int count) => Counted("ChecksRemoveAction", count);

    // Audio, Video and Subtitle pages: the note shown after opening a track from a check.

    /// <summary>
    /// Gets the note on a track page that states how many supported files contain the selected track.
    /// </summary>
    /// <param name="scope">The selected track and the files that contain it, or <see langword="null"/> when no track is selected.</param>
    /// <returns>
    /// The note, for example "Batch-wide settings: Audio track 2 is present in 3 of 5 supported files.",
    /// or an empty string when <paramref name="scope"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The track type is not audio, video or subtitle.</exception>
    public static string Coverage(TrackCoverage? scope) => scope is null ? string.Empty :
        Format(CountedKey(TrackKey("ChecksCoverage", scope.Type), scope.Supported), scope.Position, scope.WithTrack, scope.Supported);

    /// <summary>
    /// Gets the label of the link on a track page that returns to the check it was opened from.
    /// </summary>
    /// <param name="inspection">The check that opened the track page, or <see langword="null"/> when it was opened another way.</param>
    /// <returns>"Back to this file's check" when the review was focused on one file; otherwise, "Back to this check".</returns>
    public static string ReturnAction(TrackCheckInspection? inspection) =>
        Get(inspection?.FromFile == true ? "ChecksBackToFileCheck" : "ChecksBackToCheck");

    // Shared helpers.

    /// <summary>
    /// Gets the number of findings of each kind, or the clean result when there are none.
    /// </summary>
    /// <param name="summary">The summary of file checks.</param>
    /// <returns>
    /// The findings, for example "1 unsupported file / 2 blocking checks / 1 advisory check",
    /// or "No issues found by enabled checks" when there are no findings.
    /// </returns>
    private static string Headline(FileCheckSummary summary)
    {
        if (!summary.HasFindings)
            return Get("ChecksClean");

        var parts = new List<string>(3);
        if (summary.UnsupportedCount > 0)
        {
            parts.Add(Counted("ChecksUnsupportedCount", summary.UnsupportedCount));
        }
        if (summary.BlockingCheckCount > 0)
        {
            parts.Add(Counted("ChecksBlockingCount", summary.BlockingCheckCount));
        }
        if (summary.AdvisoryCheckCount > 0)
        {
            parts.Add(Counted("ChecksAdvisoryCount", summary.AdvisoryCheckCount));
        }
        return string.Join(Get("ChecksListSeparator"), parts);
    }

    /// <summary>
    /// Gets the name of the resource string written for one track type.
    /// </summary>
    /// <param name="key">The base name, for example <c>ChecksLanguageTitle</c>.</param>
    /// <param name="type">The track type.</param>
    /// <returns>The base name followed by the track type, for example <c>ChecksLanguageTitleAudio</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not audio, video or subtitle.</exception>
    private static string TrackKey(string key, TrackType type) => key + type switch
    {
        TrackType.Audio => "Audio",
        TrackType.Video => "Video",
        TrackType.Text => "Subtitle",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Validation only compares audio, video and subtitle tracks.")
    };
}
