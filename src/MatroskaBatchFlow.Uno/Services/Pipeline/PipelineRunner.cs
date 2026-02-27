using System.Diagnostics;
using MatroskaBatchFlow.Core.Services.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// Executes pipeline stages sequentially and manages overlay feedback for stages that opt in.
/// </summary>
/// <remarks>
/// Concurrent calls to <see cref="RunAsync"/> are serialized: the second caller waits until the
/// first run completes (or is cancelled) before it begins. This prevents interleaved mutations
/// on shared state (batch configuration, validation) and avoids one run clearing the overlay
/// while another is still in progress.
/// </remarks>
public sealed partial class PipelineRunner(IInputOperationFeedbackService feedbackService, ILogger<PipelineRunner> logger) : IPipelineRunner
{
    /// <summary>
    /// Minimum overlay visibility duration in milliseconds.
    /// </summary>
    private const int MinOverlayDurationMs = 500;

    /// <summary>
    /// Minimum time an overlay-enabled stage stays visible, preventing sub-perceptual flicker.
    /// </summary>
    private static readonly TimeSpan MinOverlayDuration = TimeSpan.FromMilliseconds(MinOverlayDurationMs);

    /// <summary>
    /// Ensures that at most one pipeline run executes at a time.
    /// </summary>
    private readonly SemaphoreSlim _runLock = new(1, 1);

    /// <inheritdoc />
    public async Task RunAsync(IReadOnlyList<IPipelineStage> stages, PipelineContext context, CancellationToken ct = default)
    {
        if (_runLock.CurrentCount == 0)
            LogRunQueued();

        await _runLock.WaitAsync(ct);
        try
        {
            try
            {
                for (var i = 0; i < stages.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var stage = stages[i];
                    LogStageStarting(stage.DisplayName, i + 1, stages.Count);

                    IProgress<(int current, int total)>? progress = null;
                    long overlayStartedTicks = 0;

                    if (stage.ShowsOverlay)
                    {
                        // Publish initial overlay state for this stage.
                        feedbackService.Publish(new InputOperationOverlayState(
                            stage.DisplayName,
                            stage.IsIndeterminate,
                            Current: 0,
                            Total: 0,
                            BlocksInput: true));

                        // Yield so the UI can render the overlay before the stage runs.
                        await Task.Yield();

                        overlayStartedTicks = Stopwatch.GetTimestamp();

                        // Create a progress reporter that updates the overlay with determinate progress.
                        progress = new Progress<(int current, int total)>(p =>
                            feedbackService.Publish(new InputOperationOverlayState(
                                stage.DisplayName,
                                IsIndeterminate: false,
                                p.current,
                                p.total,
                                BlocksInput: true)));
                    }

                    await stage.ExecuteAsync(context, progress, ct);

                    // Ensure the overlay stays visible long enough for the user to perceive it.
                    if (stage.ShowsOverlay)
                    {
                        var elapsed = Stopwatch.GetElapsedTime(overlayStartedTicks);
                        var remaining = MinOverlayDuration - elapsed;
                        if (remaining > TimeSpan.Zero)
                            await Task.Delay(remaining, ct);
                    }

                    LogStageCompleted(stage.DisplayName);

                    if (context.IsAborted)
                    {
                        LogPipelineAborted(stage.DisplayName);
                        break;
                    }
                }
            }
            finally
            {
                feedbackService.Clear();
            }
        }
        finally
        {
            _runLock.Release();
        }
    }
}
