using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// Selection state of the Validation page: the file filter, the selected check and the expanded values.
/// Stores keys rather than file lists, so the selection can be restored against new validation results.
/// </summary>
/// <param name="filePath">The file to focus, or null to review the whole batch.</param>
public sealed class FileCheckVisit(string? filePath = null)
{
    /// <summary>
    /// Gets the file that the page is filtered to, or null when it shows the whole batch.
    /// </summary>
    public string? FilePath { get; internal set; } = filePath;

    /// <summary>
    /// Gets or sets the selected check, or null to select the first check.
    /// </summary>
    internal FileCheckKey? Check { get; set; }

    /// <summary>
    /// Gets or sets the raw values of the expanded comparison rows.
    /// </summary>
    internal HashSet<string?> ExpandedValues { get; set; } = [];
}

/// <summary>
/// Identifies one review topic across revalidation, even when the set of participating files changes.
/// </summary>
/// <param name="Kind">The rule's structured identity.</param>
/// <param name="Severity">The reported severity; findings of different severities remain separate.</param>
/// <param name="TrackType">Track type for a comparison, or null for a file diagnostic.</param>
/// <param name="TrackPosition">One-based position for a track property; null for track counts or file diagnostics.</param>
/// <param name="Diagnostic">The original message for <see cref="ValidationCheckKind.Other"/> so unrelated diagnostics are not merged.</param>
public sealed record FileCheckKey(ValidationCheckKind Kind, ValidationSeverity Severity,
    TrackType? TrackType, int? TrackPosition, string? Diagnostic);

/// <summary>
/// Opens a batch track editor from a validation check without narrowing the scope of edits.
/// </summary>
/// <param name="TrackPosition">One-based position to select, or null for a track-count check with no specific position.</param>
public sealed record TrackCheckInspection(int? TrackPosition);
