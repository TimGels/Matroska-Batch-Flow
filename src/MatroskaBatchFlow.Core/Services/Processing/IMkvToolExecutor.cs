using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Service to execute mkvtoolnix tools.
/// </summary>
public interface IMkvToolExecutor
{
    /// <summary>
    /// Executes the specified command line asynchronously and returns the result of the operation.
    /// </summary>
    /// <param name="commandLineArguments">The command line arguments string to use when executing.</param>
    /// <param name="progress">An optional receiver for progress percentages (0-100) reported by the tool. Progress is only
    /// reported for operations that read the whole file, such as calculating track statistics.</param>
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
    /// The tool is only stopped while it is still reading the file; once it may be writing, it is allowed to finish.</param>
    /// <returns>A <see cref="MkvPropeditResult"/> representing the outcome of the operation. This includes success or failure
    /// details.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> was canceled and the tool was stopped
    /// before it could modify the file.</exception>
    public Task<MkvPropeditResult> ExecuteAsync(string commandLineArguments, IProgress<int>? progress = null, CancellationToken ct = default);
}
