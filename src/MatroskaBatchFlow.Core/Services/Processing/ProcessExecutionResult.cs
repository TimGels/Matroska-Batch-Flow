namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
/// <param name="ExitCode">The exit code of the process.</param>
/// <param name="StdOut">The standard output produced by the process.</param>
/// <param name="StdErr">The standard error output produced by the process.</param>
/// <param name="Warnings">A list of warning messages captured from the process output.</param>
/// <param name="Canceled">Indicates whether the process was canceled.</param>
public sealed record ProcessExecutionResult(int ExitCode, string StdOut, string StdErr, IReadOnlyList<string> Warnings, bool Canceled);
