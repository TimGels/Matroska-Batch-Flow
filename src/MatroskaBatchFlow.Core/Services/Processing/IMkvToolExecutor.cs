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
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.</param>
    /// <returns>A <see cref="MkvPropeditResult"/> representing the outcome of the operation. This includes success or failure
    /// details.</returns>
    public Task<MkvPropeditResult> ExecuteAsync(string commandLineArguments, CancellationToken ct = default);
}
