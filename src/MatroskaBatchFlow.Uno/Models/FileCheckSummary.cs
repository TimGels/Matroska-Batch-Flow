using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// Groups validation evidence for presentation without changing validation or processing policy.
/// </summary>
/// <param name="FileCount">Total files in the batch, including unsupported files and files with no findings.</param>
/// <param name="Groups">Review topics, rather than one entry per file that triggered a rule.</param>
public sealed record FileCheckSummary(int FileCount, IReadOnlyList<FileCheckGroup> Groups)
{
    /// <summary>
    /// Gets whether the batch has files, so the checks summary is shown on the Input page.
    /// </summary>
    public bool IsVisible => FileCount > 0;

    /// <summary>
    /// Gets whether any check has findings.
    /// </summary>
    public bool HasFindings => Groups.Count > 0;

    /// <summary>
    /// Gets whether any check blocks batch edits.
    /// </summary>
    public bool HasBlockingIssues => Groups.Any(group => group.IsBlocking);

    /// <summary>
    /// Gets the number of unsupported files.
    /// </summary>
    public int UnsupportedCount => Groups.Where(group => group.IsFormatCheck).Sum(group => group.Files.Count);

    /// <summary>
    /// Gets the number of blocking checks, not counting the file format check.
    /// </summary>
    public int BlockingCheckCount => Groups.Count(group => group.IsBlocking && !group.IsFormatCheck);

    /// <summary>
    /// Gets the number of checks that do not block batch edits.
    /// </summary>
    public int AdvisoryCheckCount => Groups.Count(group => !group.IsBlocking);

    private static FileCheckKey KeyFor(FileValidationResult result, ValidationComparison? comparison) =>
        new(result.CheckKind, result.Severity, comparison?.TrackType, comparison?.TrackPosition,
            result.CheckKind == ValidationCheckKind.Other ? result.Message : null);

    /// <summary>
    /// Counts blockers for the footer without building the display groups or formatting scanned values.
    /// </summary>
    /// <param name="results">Findings from the same validation run.</param>
    /// <returns>Distinct unsupported file paths and distinct blocking topics other than file format.</returns>
    internal static (int UnsupportedFiles, int OtherChecks) CountBlockers(IReadOnlyList<FileValidationResult> results)
    {
        var files = new HashSet<string>(StringComparer.Ordinal);
        var checks = new HashSet<FileCheckKey>();
        foreach (var result in results.Where(result => result.IsBlocking))
        {
            if (result.CheckKind == ValidationCheckKind.FileFormat)
            {
                files.Add(result.ValidatedFilePath);
            }
            else
            {
                foreach (var comparison in result.Comparisons.DefaultIfEmpty())
                    checks.Add(KeyFor(result, comparison));
            }
        }
        return (files.Count, checks.Count);
    }

    /// <summary>
    /// Builds one review topic per enabled finding kind, severity, track type and track position.
    /// </summary>
    /// <param name="results">Findings from the same validation run. Several mismatches can describe the same comparison.</param>
    /// <param name="fileCount">Size of the batch that was validated, not the number of files with findings.</param>
    /// <param name="languageProvider">Resolves scanned language tags for display; the raw tag remains the grouping key for value rows.</param>
    /// <returns>Topics in review order: highest severity first, then format and track-count checks ahead of track properties.</returns>
    public static FileCheckSummary Create(
        IReadOnlyList<FileValidationResult> results,
        int fileCount,
        ILanguageProvider languageProvider)
    {
        var groups = results
            .Where(result => result.Severity != ValidationSeverity.Off)
            .SelectMany(result => result.Comparisons.DefaultIfEmpty()
                .Select(comparison => (Result: result, Comparison: comparison)))
            .GroupBy(item => KeyFor(item.Result, item.Comparison))
            .Select(group =>
            {
                var first = group.First();
                var kind = group.Key.Kind;
                var comparison = first.Comparison;
                // One comparison already contains every participant, including matches. Concatenating
                // the evidence from each mismatch would count the same files more than once.
                var files = comparison is null
                    ? group.Select(item => new FileCheckFile(item.Result.ValidatedFilePath,
                            new(kind, Diagnostic: item.Result.Message, FormatIssue: item.Result.FormatIssue)))
                        .DistinctBy(file => file.FilePath).ToArray()
                    : comparison.Files.Select(file => new FileCheckFile(
                        file.FilePath, new(kind, file.Value, ResolveLanguageName(kind, file.Value, languageProvider)))).ToArray();
                return new FileCheckGroup(group.Key, files);
            })
            .OrderByDescending(group => group.Severity)
            .ThenByDescending(group => group.IsFormatCheck)
            .ThenByDescending(group => group.Kind == ValidationCheckKind.TrackCount)
            .ThenBy(group => group.TrackType)
            .ThenBy(group => group.TrackPosition)
            .ThenBy(group => group.Kind switch
            {
                ValidationCheckKind.DefaultFlag => 0,
                ValidationCheckKind.ForcedFlag => 1,
                ValidationCheckKind.Language => 2,
                _ => 3
            })
            .ToArray();

        return new FileCheckSummary(fileCount, groups);
    }

    /// <summary>
    /// Groups participating files by raw scanned value for the expandable comparison rows.
    /// </summary>
    /// <param name="check">A topic from this summary. File diagnostics have no comparison rows.</param>
    /// <param name="batchPaths">All paths in the corresponding batch, even when reviewing just one file.</param>
    /// <returns>One row per distinct value, plus a separate row for supported files missing the track position, if any.</returns>
    /// <remarks>
    /// The missing-track row provides context only. Its files did not participate in this property check.
    /// </remarks>
    public IReadOnlyList<FileCheckValue> GetComparisonRows(FileCheckGroup check, IEnumerable<string> batchPaths)
    {
        if (!check.HasComparison)
            return [];

        var rows = check.Files.GroupBy(file => file.Evidence.RawValue, StringComparer.Ordinal)
            .Select(group => new FileCheckValue(group.First().Evidence, group.ToArray())).ToList();
        if (check.TrackPosition is not null && check.Kind != ValidationCheckKind.TrackCount)
        {
            var accountedFor = check.Files.Select(file => file.FilePath).ToHashSet(StringComparer.Ordinal);
            foreach (var unsupported in Groups.Where(group => group.IsFormatCheck))
                accountedFor.UnionWith(unsupported.Files.Select(file => file.FilePath));

            var absent = new FileCheckEvidence(check.Kind);
            var missing = batchPaths.Where(path => !accountedFor.Contains(path))
                .Select(path => new FileCheckFile(path, absent)).ToArray();
            if (missing.Length > 0)
            {
                rows.Add(new FileCheckValue(absent, missing));
            }
        }
        return rows;
    }

    private static string? ResolveLanguageName(ValidationCheckKind kind, string value, ILanguageProvider languageProvider)
    {
        if (kind != ValidationCheckKind.Language || string.IsNullOrWhiteSpace(value))
            return null;

        var language = languageProvider.Resolve(value);
        return language == MatroskaLanguageOption.Undetermined && value != "und" ? null : language.Name;
    }
}
