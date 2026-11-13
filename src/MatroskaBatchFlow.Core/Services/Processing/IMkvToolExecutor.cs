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
    /// <param name="ct">An optional <see cref="CancellationToken"/>. It is only checked before the tool starts. A running tool is
    /// never stopped, because it edits the file in place and stopping it could leave the file corrupted.</param>
    /// <returns>A <see cref="MkvPropeditResult"/> representing the outcome of the operation. This includes success or failure
    /// details.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> was canceled before the tool started.</exception>
    public Task<MkvPropeditResult> ExecuteAsync(string commandLineArguments, IProgress<int>? progress = null, CancellationToken ct = default);
}
