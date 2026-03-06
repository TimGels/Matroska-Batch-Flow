using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Stores per-file track values that are initially populated from the MediaInfo scan and
/// subsequently updated when the user changes settings in the UI.
/// The original scanned data is always available via <see cref="ScannedTrackInfo"/>.
/// </summary>
/// <remarks>
/// The batch configuration uses a dual-model approach for tracks:
/// <list type="bullet">
/// <item>
/// <see cref="Services.TrackConfiguration"/> - one per track index in the global collection.
/// Carries modification intent (<c>ShouldModify*</c> flags) and is shared across all files.
/// </item>
/// <item>
/// <see cref="FileTrackValues"/> - one per track per file in <see cref="FileTrackConfiguration"/>.
/// Holds the actual values (Name, Language, flags) to write for that specific file.
/// </item>
/// </list>
/// During command generation, <see cref="Services.MkvPropeditArgumentsGenerator"/> reads
/// <c>ShouldModify*</c> from the global track and the values to write from the per-file track.
/// </remarks>
public sealed class FileTrackValues
{
    /// <summary>
    /// The raw track information as returned by MediaInfo.
    /// </summary>
    public required MediaInfoResult.MediaInfo.TrackInfo ScannedTrackInfo { get; init; }

    /// <summary>
    /// The type of this track (Audio, Video, Text).
    /// </summary>
    public TrackType Type { get; init; }

    /// <summary>
    /// Zero-based index of this track within its type (e.g. 0 = first audio track).
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// The track name as scanned from the file.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The track language as scanned from the file.
    /// </summary>
    public MatroskaLanguageOption Language { get; set; } = MatroskaLanguageOption.Undetermined;

    /// <summary>
    /// Whether this track has the default flag set.
    /// </summary>
    public bool Default { get; set; }

    /// <summary>
    /// Whether this track has the forced flag set.
    /// </summary>
    public bool Forced { get; set; }

    /// <summary>
    /// Whether this track is enabled (corresponds to the Matroska FlagEnabled element).
    /// </summary>
    public bool Enabled { get; set; }
}
