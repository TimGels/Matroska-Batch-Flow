namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Identifies a file check independently of its diagnostic message.
/// </summary>
public enum ValidationCheckKind
{
    /// <summary>
    /// A diagnostic without a structured kind. Its message identifies the check.
    /// </summary>
    Other,

    /// <summary>
    /// Whether the file is a supported Matroska input, checked before any track comparison.
    /// </summary>
    FileFormat,

    /// <summary>
    /// The number of tracks of one type, compared across files.
    /// </summary>
    TrackCount,

    /// <summary>
    /// The language tag of the track at one position, compared across files.
    /// </summary>
    Language,

    /// <summary>
    /// The default flag of the track at one position, compared across files.
    /// </summary>
    DefaultFlag,

    /// <summary>
    /// The forced flag of the track at one position, compared across files.
    /// </summary>
    ForcedFlag
}
