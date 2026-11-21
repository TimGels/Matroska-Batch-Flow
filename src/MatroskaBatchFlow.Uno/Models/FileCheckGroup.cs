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
    public ValidationSeverity Severity => Topic.Severity;
    public ValidationCheckKind Kind => Topic.Kind;
    public TrackType? TrackType => Topic.TrackType;
    public int? TrackPosition => Topic.TrackPosition;
    public bool HasComparison => TrackType.HasValue;
    public bool IsFormatCheck => Kind == ValidationCheckKind.FileFormat;
    public bool IsBlocking => Severity == ValidationSeverity.Error;
    public bool IsWarning => Severity == ValidationSeverity.Warning;
    public bool IsInformation => Severity == ValidationSeverity.Info;
    // Count checks share a family across track types; file diagnostics have no family heading.
    public (ValidationCheckKind Kind, TrackType? Type) FamilyKey => !HasComparison ? default
        : (Kind, Kind == ValidationCheckKind.TrackCount ? null : TrackType);
}

/// <summary>Groups related checks within the Blocking or Advisory list. Only the first family carries the category heading.</summary>
public sealed class FileCheckSection(int categoryCount, IList<FileCheckGroup> checks)
    : ReadOnlyCollection<FileCheckGroup>(checks)
{
    public int CategoryCount { get; } = categoryCount;
    public FileCheckKey Topic => this[0].Topic;
    public bool ShowHeading => CategoryCount > 0;
    public bool ShowFamily => Topic.TrackType.HasValue;
}

/// <summary>
/// An individual file and its value within a review topic.
/// </summary>
/// <param name="FilePath">Path identifying the file, even when other files have the same name.</param>
/// <param name="Evidence">Scanned data or diagnostic, not a localized display label.</param>
public sealed record FileCheckFile(string FilePath, FileCheckEvidence Evidence)
{
    public string FileName => Path.GetFileName(FilePath);
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;
}

/// <summary>Raw evidence shared by files in a value row. Only unknown/unstructured diagnostics use the message verbatim.</summary>
public sealed record FileCheckEvidence(ValidationCheckKind Kind, string? RawValue = null, string? LanguageName = null,
    string? Diagnostic = null, UnsupportedFormatIssue? FormatIssue = null);

/// <summary>An expandable row of files with the same scanned value, or files missing the selected track.</summary>
public sealed partial class FileCheckValue(FileCheckEvidence evidence, IReadOnlyList<FileCheckFile> files) : ObservableObject
{
    public FileCheckEvidence Evidence { get; } = evidence;
    public IReadOnlyList<FileCheckFile> Files { get; } = files;
    /// <summary>Key used to restore expansion: null for an absent track, an empty string for a present track with no language tag.</summary>
    public string? RawValue => Evidence.RawValue;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
}
