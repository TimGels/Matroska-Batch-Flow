using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Utilities;

/// <summary>
/// Provides a method to convert exit codes from the <c>mkvpropedit</c> command-line tool into <see cref="MkvPropeditStatus"/> values.
/// </summary>
public static class MkvPropeditStatusHelper
{
    /// <summary>
    /// Converts an exit code from the <c>mkvpropedit</c> command-line tool into a <see cref="MkvPropeditStatus"/> value.
    /// </summary>
    /// <param name="exitCode">The exit code returned by the <c>mkvpropedit</c> command-line tool.</param>
    /// <returns>A <see cref="MkvPropeditStatus"/> value representing the status of the command execution.</returns>
    public static MkvPropeditStatus FromExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => MkvPropeditStatus.Success,
            1 => MkvPropeditStatus.Warning,
            2 => MkvPropeditStatus.Error,
            _ => MkvPropeditStatus.Unknown
        };
    }
}
