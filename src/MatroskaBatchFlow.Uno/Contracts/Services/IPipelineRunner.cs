using MatroskaBatchFlow.Core.Abstractions.Pipeline;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Executes an ordered sequence of <see cref="IPipelineStage"/> instances,
/// managing overlay feedback and progress reporting automatically.
/// </summary>
public interface IPipelineRunner
{
    /// <summary>
    /// Runs the given stages sequentially, publishing overlay state for stages that opt in.
    /// The overlay is always cleared when execution completes (success or failure).
    /// </summary>
    /// <param name="stages">The ordered stages to execute.</param>
    /// <param name="context">The shared context bag passed to every stage.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    Task RunAsync(IReadOnlyList<IPipelineStage> stages, PipelineContext context, CancellationToken ct = default);
}
