namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Represents the status of the <c>mkvpropedit</c> command-line tool execution returned as an exit code.
/// </summary>
public enum MkvPropeditStatus
{
    Success,
    Warning,
    Error,
    Unknown
}
