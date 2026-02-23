using System.Diagnostics;
using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// Executes pipeline stages sequentially and manages overlay feedback for stages that opt in.
/// </summary>
public sealed partial class PipelineRunner(
    IInputOperationFeedbackService feedbackService,
    ILogger<PipelineRunner> logger) : IPipelineRunner
{
    /// <summary>
    /// Minimum time an overlay-enabled stage stays visible, preventing sub-perceptual flicker.
    /// </summary>
    private static readonly TimeSpan MinOverlayDuration = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc />
    public async Task RunAsync(IReadOnlyList<IPipelineStage> stages, PipelineContext context, CancellationToken ct = default)
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
            }
        }
        finally
        {
            feedbackService.Clear();
        }
    }
}
