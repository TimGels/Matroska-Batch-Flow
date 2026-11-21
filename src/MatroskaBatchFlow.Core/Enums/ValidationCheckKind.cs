namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Identifies a file check independently of its diagnostic message.
/// </summary>
public enum ValidationCheckKind
{
    Other,
    FileFormat,
    TrackCount,
    Language,
    DefaultFlag,
    ForcedFlag
}
