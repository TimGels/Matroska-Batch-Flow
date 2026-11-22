using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// A single review topic with evidence from the scanned files.
/// </summary>
/// <param name="Topic">Identifies the check independently of its display title.</param>
/// <param name="Files">All participants for a comparison, or the affected files and their reasons for a diagnostic.</param>
public sealed record FileCheckGroup(
    FileCheckKey Topic,
    IReadOnlyList<FileCheckFile> Files)
{
    /// <summary>
    /// Gets the severity of the check.
    /// </summary>
    public ValidationSeverity Severity => Topic.Severity;

    /// <summary>
    /// Gets what the check compares.
    /// </summary>
    public ValidationCheckKind Kind => Topic.Kind;

    /// <summary>
    /// Gets the compared track type, or null for a file diagnostic.
    /// </summary>
    public TrackType? TrackType => Topic.TrackType;

    /// <summary>
    /// Gets the one-based track position, or null for a track count or a file diagnostic.
    /// </summary>
    public int? TrackPosition => Topic.TrackPosition;

    /// <summary>
    /// Gets whether the check compares scanned values across files, rather than reporting a diagnostic per file.
    /// </summary>
    public bool HasComparison => TrackType.HasValue;

    /// <summary>
    /// Gets whether this is the unsupported file format check.
    /// </summary>
    public bool IsFormatCheck => Kind == ValidationCheckKind.FileFormat;

    /// <summary>
    /// Gets whether the check blocks batch edits.
    /// </summary>
    public bool IsBlocking => Severity == ValidationSeverity.Error;

    /// <summary>
    /// Gets whether the check is a warning.
    /// </summary>
    public bool IsWarning => Severity == ValidationSeverity.Warning;

    /// <summary>
    /// Gets whether the check is informational.
    /// </summary>
    public bool IsInformation => Severity == ValidationSeverity.Info;

    /// <summary>
    /// Gets the key of the family heading this check is listed under on the Validation page.
    /// </summary>
    /// <remarks>
    /// Track count checks share one family across track types. File diagnostics have no family heading.
    /// </remarks>
    public (ValidationCheckKind Kind, TrackType? Type) FamilyKey => !HasComparison ? default
        : (Kind, Kind == ValidationCheckKind.TrackCount ? null : TrackType);
}

/// <summary>
/// Groups related checks within the Blocking or Advisory list. Only the first family carries the category heading.
/// </summary>
public sealed class FileCheckSection(int categoryCount, IList<FileCheckGroup> checks)
    : ReadOnlyCollection<FileCheckGroup>(checks)
{
    /// <summary>
    /// Gets the number of checks in the Blocking or Advisory category, shown in its heading.
    /// Zero for every family after the first in its category.
    /// </summary>
    public int CategoryCount { get; } = categoryCount;

    /// <summary>
    /// Gets the first check in the family, which identifies the category and family for the headings.
    /// </summary>
    public FileCheckKey Topic => this[0].Topic;

    /// <summary>
    /// Gets whether this family is the first in its category and shows the category heading.
    /// </summary>
    public bool ShowHeading => CategoryCount > 0;

    /// <summary>
    /// Gets whether this family has a family heading. File diagnostics have none.
    /// </summary>
    public bool ShowFamily => Topic.TrackType.HasValue;
}

/// <summary>
/// An individual file and its value within a review topic.
/// </summary>
/// <param name="FilePath">Path identifying the file, even when other files have the same name.</param>
/// <param name="Evidence">Scanned data or diagnostic, not a localized display label.</param>
public sealed record FileCheckFile(string FilePath, FileCheckEvidence Evidence)
{
    /// <summary>
    /// Gets the file name with its extension, without the directory.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the directory that contains the file, or an empty string when the path has none.
    /// </summary>
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;
}

/// <summary>
/// Raw evidence shared by files in a value row. Only unknown/unstructured diagnostics use the message verbatim.
/// </summary>
/// <param name="Kind">The check that produced the evidence.</param>
/// <param name="RawValue">The scanned value as text, or null for a file without the track and for file diagnostics. An empty string is a track without a language tag.</param>
/// <param name="LanguageName">The display name of a scanned language tag; null for other checks and for empty or unknown tags.</param>
/// <param name="Diagnostic">The message of a file diagnostic, shown verbatim; otherwise, null.</param>
/// <param name="FormatIssue">Why the file is unsupported, for the file format check; otherwise, null.</param>
public sealed record FileCheckEvidence(ValidationCheckKind Kind, string? RawValue = null, string? LanguageName = null,
    string? Diagnostic = null, UnsupportedFormatIssue? FormatIssue = null);

/// <summary>
/// An expandable row of files with the same scanned value, or files missing the selected track.
/// </summary>
public sealed partial class FileCheckValue(FileCheckEvidence evidence, IReadOnlyList<FileCheckFile> files) : ObservableObject
{
    /// <summary>
    /// Gets the scanned value that the files in this row share.
    /// </summary>
    public FileCheckEvidence Evidence { get; } = evidence;

    /// <summary>
    /// Gets the files with this value.
    /// </summary>
    public IReadOnlyList<FileCheckFile> Files { get; } = files;

    /// <summary>
    /// Key used to restore expansion: null for an absent track, an empty string for a present track with no language tag.
    /// </summary>
    public string? RawValue => Evidence.RawValue;

    /// <summary>
    /// Gets or sets whether the row is expanded to list its files.
    /// </summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
}
