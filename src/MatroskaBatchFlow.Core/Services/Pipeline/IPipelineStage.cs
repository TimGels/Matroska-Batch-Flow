using System.Diagnostics.CodeAnalysis;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Represents a single stage in a composable pipeline.
/// Each stage performs a discrete unit of work and communicates with other stages via a shared <see cref="PipelineContext"/>.
/// </summary>
/// <remarks>
/// Stages should be stateless singletons — all per-run state lives in the <see cref="PipelineContext"/>.
/// If a stage has no work to do (e.g., no files in context), it should return gracefully without throwing.
/// </remarks>
public interface IPipelineStage
{
    /// <summary>
    /// User-visible label for overlay display (e.g., "Scanning files…").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether progress is indeterminate (unknown duration) or determinate (known current/total).
    /// </summary>
    bool IsIndeterminate { get; }

    /// <summary>
    /// Whether this stage should display an overlay to the user.
    /// Near-instant stages can return <see langword="false"/> to avoid visual flicker.
    /// </summary>
    [ExcludeFromCodeCoverage]
    bool ShowsOverlay => true;

    /// <summary>
    /// Executes this stage's work.
    /// </summary>
    /// <param name="context">Shared data bag for passing data between stages.</param>
    /// <param name="progress">Optional progress reporter for determinate stages.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct);
}
