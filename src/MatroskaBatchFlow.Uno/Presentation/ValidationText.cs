using System.Globalization;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Uno.Models;
using static MatroskaBatchFlow.Uno.Presentation.UiText;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>View bindings for validation text. Topic identity, row membership and navigation never depend on these labels.</summary>
public static class ValidationText
{
    public static string InputHeadline(FileCheckSummary summary, bool updating) =>
        updating ? Get("ChecksUpdating") : Headline(summary);

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

    public static string Summary(FileCheckSummary summary) =>
        Format("ChecksSummary", Counted("ChecksBatchSize", summary.FileCount), Headline(summary));

    public static string ComparisonHeading(FileCheckSummary summary) => summary.UnsupportedCount == 0
        ? Format("ChecksComparison", Counted("ChecksBatchSize", summary.FileCount))
        : Format("ChecksSupportedComparison", summary.FileCount - summary.UnsupportedCount, summary.FileCount);

    public static string ReviewCount(int count) => Counted("ChecksReview", count);
    public static string ReviewFile(int count, string name) => Format(CountedKey("ChecksReviewFile", count), count, name);
    public static string TopicList(IReadOnlyList<FileCheckKey> topics) => string.Join(Environment.NewLine, topics.Select(Title));
    public static string EmptyTitle(int count) => Get(count == 0 ? "ChecksEmptyBatch" : "ChecksClean");
    public static string EmptyDescription(int count) => Get(count == 0 ? "ChecksAddFiles" : "ChecksCleanDescription");
    public static string Scope(string? name) => Format("ChecksFileScope", name);
    public static string Removed(int count) => count > 0 ? Counted("ChecksRemoved", count) : string.Empty;
    public static string RemoveAction(int count) => Counted("ChecksRemoveAction", count);

    private static string Category(FileCheckKey topic) => Get(topic.Severity == ValidationSeverity.Error ? "ChecksBlocking" : "ChecksAdvisory");
    public static string CategoryHeading(FileCheckKey topic, int count) => Format("ChecksCategoryCount", Category(topic), count);
    public static string Selector(FileCheckKey topic) => Format("ChecksSelector", Category(topic), Title(topic));

    private static string TrackName(TrackType? type) => type switch
    {
        TrackType.Audio => Get("ChecksAudio"),
        TrackType.Video => Get("ChecksVideo"),
        TrackType.Text => Get("ChecksSubtitle"),
        _ => type?.ToString() ?? string.Empty
    };

    public static string Family(FileCheckKey topic)
    {
        if (topic.TrackType is null)
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
        return Format(key, TrackName(topic.TrackType));
    }

    public static string Title(FileCheckKey? topic)
    {
        if (topic is null)
            return string.Empty;
        if (topic.Kind == ValidationCheckKind.FileFormat)
            return Get("ChecksUnsupportedTitle");
        if (topic.TrackType is null)
            return Get("ChecksDiagnosticTitle");

        var key = topic.Kind switch
        {
            ValidationCheckKind.TrackCount => "ChecksTrackCountTitle",
            ValidationCheckKind.Language => "ChecksLanguageTitle",
            ValidationCheckKind.DefaultFlag => "ChecksDefaultTitle",
            ValidationCheckKind.ForcedFlag => "ChecksForcedTitle",
            _ => "ChecksOtherTitle"
        };
        return Format(key, TrackName(topic.TrackType), topic.TrackPosition);
    }

    public static string NavigationLabel(FileCheckKey topic) => topic.TrackType is null ? Title(topic)
        : topic.Kind == ValidationCheckKind.TrackCount ? TrackName(topic.TrackType) : Format("ChecksTrackPosition", topic.TrackPosition);

    public static string Consequence(FileCheckKey? topic) => topic is null ? string.Empty : Get(
        topic.Kind == ValidationCheckKind.FileFormat ? "ChecksAlwaysBlock" :
        topic.Severity == ValidationSeverity.Error ? "ChecksBlockBySettings" : "ChecksDoNotBlock");

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

    public static string FileCount(int count) => Counted("ChecksFiles", count);
    public static string ValueAndCount(FileCheckEvidence evidence, int count) => Format("ChecksValueCount", Value(evidence), FileCount(count));
    public static string Coverage(TrackCoverage? scope) => scope is null ? string.Empty :
        Format(CountedKey("ChecksCoverage", scope.Supported), TrackName(scope.Type), scope.Position, scope.WithTrack, scope.Supported);
    public static string ReturnAction(TrackCheckInspection? inspection) =>
        Get(inspection?.FromFile == true ? "ChecksBackToFileCheck" : "ChecksBackToCheck");
}
