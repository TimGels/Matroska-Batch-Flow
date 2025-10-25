using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Represents the result of executing an MKVPropEdit operation including execution details.
/// </summary>
public sealed record MkvPropeditResult
{
    /// <summary>
    /// Gets the status of the MKVPropEdit operation.
    /// </summary>
    public MkvPropeditStatus Status { get; init; }

    /// <summary>
    /// Gets the standard output produced by the MKVPropEdit operation. 
    /// Can be <see langword="null"/> if no output is available.
    /// </summary>
    public string? StandardOutput { get; init; }

    /// <summary>
    /// Gets the standard error output produced by the MKVPropEdit operation. 
    /// Can be <see langword="null"/> if no error output is available.
    /// </summary>
    public string? StandardError { get; init; }

    /// <summary>
    /// Gets a collection of warning messages generated during the MKVPropEdit operation. 
    /// This collection is empty if no warnings were produced.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Gets the resolved path to the MKVPropEdit executable used for the operation.
    /// </summary>
    public string ResolvedExecutablePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the arguments passed to the MKVPropEdit executable for the operation.
    /// </summary>
    public string ExecutableArguments { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the result represents a fatal error.
    /// </summary>
    public bool IsFatal => Status is not MkvPropeditStatus.Success and not MkvPropeditStatus.Warning;

    /// <summary>
    /// Gets a full (simulated) concatenated command line string, combining the resolved executable path and its arguments.
    /// Beware that this is for display purposes only and is not the actual command line used to start the process.
    /// </summary>
    public string SimulatedCommandLine => (ResolvedExecutablePath.Length > 0, ExecutableArguments.Length > 0) switch
    {
        (true, true) => $"{ResolvedExecutablePath} {ExecutableArguments}",
        (true, false) => ResolvedExecutablePath,
        (false, true) => ExecutableArguments,
        _ => string.Empty
    };
}
