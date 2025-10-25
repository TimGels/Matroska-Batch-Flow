namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Represents the status of the <c>mkvpropedit</c> command-line tool execution returned as an exit code. 
/// Mapped to the corresponding exit codes except for Unknown which can be used for unexpected exit codes.
/// See <see href="https://mkvtoolnix.download/doc/mkvpropedit.html#d4e1073"> mkvpropedit exit codes </see>.
/// </summary>
public enum MkvPropeditStatus
{
    Success = 0,
    Warning = 1,
    Error = 2,
    Unknown = 3
}
