using System.Diagnostics;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Service to run external processes.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Executes a process asynchronously with the specified start information and monitors its output and error streams.
    /// </summary>
    /// <param name="startInfo">The <see cref="ProcessStartInfo"/> object that specifies the process to start and its configuration.</param>
    /// <param name="isWarning">An optional delegate that determines whether a line of output or error data should be classified as a warning.
    /// If not provided, no lines will be classified as warnings.</param>
    /// <param name="onOutputLine">An optional callback invoked for every line of output or error data as soon as it is received.
    /// It runs on a background thread and may be invoked concurrently for the output and error streams.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the process to complete.
    /// When canceled, the process tree is killed.</param>
    /// <returns>A <see cref="ProcessExecutionResult"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process fails to start.</exception>
    public Task<ProcessExecutionResult> RunAsync(
        ProcessStartInfo startInfo,
        Func<string, bool>? isWarning = null,
        Action<string>? onOutputLine = null,
        CancellationToken ct = default);
}
