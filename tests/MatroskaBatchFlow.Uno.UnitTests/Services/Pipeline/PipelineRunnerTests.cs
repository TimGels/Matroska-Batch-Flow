using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Services.Pipeline;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Services.Pipeline;

/// <summary>
/// Contains unit tests for the <see cref="PipelineRunner"/> class.
/// </summary>
public class PipelineRunnerTests
{
    private readonly IInputOperationFeedbackService _feedbackService;
    private readonly PipelineRunner _runner;

    public PipelineRunnerTests()
    {
        _feedbackService = Substitute.For<IInputOperationFeedbackService>();
        _runner = new PipelineRunner(
            _feedbackService,
            Substitute.For<ILogger<PipelineRunner>>());
    }

    [Fact]
    public async Task RunAsync_StopsExecutingStages_WhenContextIsAborted()
    {
        var executedStages = new List<string>();

        var abortingStage = CreateStage("Aborting", (ctx, _, _) =>
        {
            executedStages.Add("Aborting");
            ctx.IsAborted = true;
            return Task.CompletedTask;
        }, showsOverlay: false);

        var subsequentStage = CreateStage("Should Not Run", (_, _, _) =>
        {
            executedStages.Add("Should Not Run");
            return Task.CompletedTask;
        }, showsOverlay: false);

        var context = new PipelineContext();

        await _runner.RunAsync([abortingStage, subsequentStage], context, TestContext.Current.CancellationToken);

        Assert.Single(executedStages);
        Assert.Equal("Aborting", executedStages[0]);
        Assert.True(context.IsAborted);
    }

    [Fact]
    public async Task RunAsync_ExecutesAllStages_WhenNotAborted()
    {
        var executedStages = new List<string>();

        var stage1 = CreateStage("Stage 1", (_, _, _) =>
        {
            executedStages.Add("Stage 1");
            return Task.CompletedTask;
        }, showsOverlay: false);

        var stage2 = CreateStage("Stage 2", (_, _, _) =>
        {
            executedStages.Add("Stage 2");
            return Task.CompletedTask;
        }, showsOverlay: false);

        var context = new PipelineContext();

        await _runner.RunAsync([stage1, stage2], context, TestContext.Current.CancellationToken);

        Assert.Equal(2, executedStages.Count);
        Assert.Equal("Stage 1", executedStages[0]);
        Assert.Equal("Stage 2", executedStages[1]);
    }

    [Fact]
    public async Task RunAsync_ClearsFeedback_EvenWhenAborted()
    {
        var abortingStage = CreateStage("Aborting", (ctx, _, _) =>
        {
            ctx.IsAborted = true;
            return Task.CompletedTask;
        }, showsOverlay: false);

        var context = new PipelineContext();

        await _runner.RunAsync([abortingStage], context, TestContext.Current.CancellationToken);

        _feedbackService.Received(1).Clear();
    }

    [Fact]
    public async Task RunAsync_ClearsFeedback_WhenStageThrows()
    {
        var throwingStage = CreateStage("Throwing", (_, _, _) =>
            throw new InvalidOperationException("Stage failed"));

        var context = new PipelineContext();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _runner.RunAsync([throwingStage], context, TestContext.Current.CancellationToken));

        _feedbackService.Received(1).Clear();
    }

    [Fact]
    public async Task RunAsync_ClearsFeedback_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();

        var blockingStage = CreateStage("Blocking", async (_, _, token) =>
        {
            await cts.CancelAsync();
            token.ThrowIfCancellationRequested();
        }, showsOverlay: false);

        var context = new PipelineContext();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _runner.RunAsync([blockingStage], context, cts.Token));

        _feedbackService.Received(1).Clear();
    }

    [Fact]
    public async Task RunAsync_PublishesInitialOverlayState_ForOverlayStage()
    {
        var stage = CreateStage("Scanning files…", (_, _, _) => Task.CompletedTask, showsOverlay: true, isIndeterminate: true);

        await _runner.RunAsync([stage], new PipelineContext(), TestContext.Current.CancellationToken);

        _feedbackService.Received().Publish(Arg.Is<InputOperationOverlayState>(s =>
            s.Message == "Scanning files…" &&
            s.IsIndeterminate == true &&
            s.Current == 0 &&
            s.Total == 0 &&
            s.BlocksInput == true));
    }

    [Fact]
    public async Task RunAsync_DoesNotPublishOverlay_ForNonOverlayStage()
    {
        var stage = CreateStage("Silent Stage", (_, _, _) => Task.CompletedTask, showsOverlay: false);

        await _runner.RunAsync([stage], new PipelineContext(), TestContext.Current.CancellationToken);

        _feedbackService.DidNotReceive().Publish(Arg.Any<InputOperationOverlayState>());
    }

    [Fact]
    public async Task RunAsync_PublishesDeterminateProgress_WhenProgressReported()
    {
        var stage = CreateStage("Processing…", (_, progress, _) =>
        {
            progress?.Report((3, 10));
            return Task.CompletedTask;
        }, showsOverlay: true, isIndeterminate: false);

        await _runner.RunAsync([stage], new PipelineContext(), TestContext.Current.CancellationToken);

        _feedbackService.Received().Publish(Arg.Is<InputOperationOverlayState>(s =>
            s.Current == 3 &&
            s.Total == 10 &&
            s.IsIndeterminate == false));
    }

    [Fact]
    public async Task RunAsync_PublishesOverlayForEachOverlayStage_InOrder()
    {
        var publishedMessages = new List<string>();
        _feedbackService
            .When(f => f.Publish(Arg.Any<InputOperationOverlayState>()))
            .Do(ci => publishedMessages.Add(ci.Arg<InputOperationOverlayState>().Message));

        var stage1 = CreateStage("Stage A", (_, _, _) => Task.CompletedTask, showsOverlay: true);
        var stage2 = CreateStage("Stage B", (_, _, _) => Task.CompletedTask, showsOverlay: true);

        await _runner.RunAsync([stage1, stage2], new PipelineContext(), TestContext.Current.CancellationToken);

        Assert.Contains("Stage A", publishedMessages);
        Assert.Contains("Stage B", publishedMessages);
        Assert.True(publishedMessages.IndexOf("Stage A") < publishedMessages.IndexOf("Stage B"));
    }

    [Fact]
    public async Task RunAsync_SerializesConcurrentCalls()
    {
        // First run holds a gate open so it blocks until we release it.
        var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRunGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var executionOrder = new List<string>();

        var slowStage = CreateStage("Slow", async (_, _, _) =>
        {
            executionOrder.Add("first-start");
            firstRunStarted.SetResult();
            await firstRunGate.Task;
            executionOrder.Add("first-end");
        }, showsOverlay: false);

        var fastStage = CreateStage("Fast", (_, _, _) =>
        {
            executionOrder.Add("second-start");
            secondRunStarted.SetResult();
            return Task.CompletedTask;
        }, showsOverlay: false);

        var ct = TestContext.Current.CancellationToken;
        var firstRun = Task.Run(() => _runner.RunAsync([slowStage], new PipelineContext(), ct), ct);

        // Wait for first run to actually start before launching the second.
        await firstRunStarted.Task.WaitAsync(ct);
        var secondRun = Task.Run(() => _runner.RunAsync([fastStage], new PipelineContext(), ct), ct);

        // Give the second run enough time to attempt to acquire the lock.
        await Task.Delay(50, ct);

        // Second run should not have started while first holds the lock.
        Assert.DoesNotContain("second-start", executionOrder);

        // Release the first run.
        firstRunGate.SetResult();
        await Task.WhenAll(firstRun, secondRun);

        // Both ran, and first completed entirely before second started.
        Assert.Equal(["first-start", "first-end", "second-start"], executionOrder);
    }

    [Fact]
    public async Task RunAsync_DoesNotClearFeedback_WhenCancelledBeforeAcquiringLock()
    {
        // Block the lock with a long-running first call.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lockHeld = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holdingStage = CreateStage("Holding", async (_, _, _) =>
        {
            lockHeld.SetResult();
            await gate.Task;
        }, showsOverlay: false);

        var holdingCt = TestContext.Current.CancellationToken;
        _ = Task.Run(() => _runner.RunAsync([holdingStage], new PipelineContext(), holdingCt), holdingCt);
        await lockHeld.Task.WaitAsync(TestContext.Current.CancellationToken);

        // Now try a second run on the same runner, but cancel it before it can acquire the lock.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Inject pre-cancelled token — WaitAsync on _runner's held lock should throw immediately.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _runner.RunAsync([CreateStage("Never", (_, _, _) => Task.CompletedTask)], new PipelineContext(), cts.Token));

        // This test only verifies the second runner's behavior: because it never acquires the lock
        // (the token is already cancelled), it must not call Clear on the feedback service. Any Clear
        // invocation from the first, long-running runner is intentionally not asserted here.
        _feedbackService.DidNotReceive().Clear();

        gate.SetResult(); // Let the first run finish cleanly.
    }

    private static IPipelineStage CreateStage(
        string displayName,
        Func<PipelineContext, IProgress<(int current, int total)>?, CancellationToken, Task> execute,
        bool showsOverlay = true,
        bool isIndeterminate = true)
    {
        var stage = Substitute.For<IPipelineStage>();
        stage.DisplayName.Returns(displayName);
        stage.IsIndeterminate.Returns(isIndeterminate);
        stage.ShowsOverlay.Returns(showsOverlay);
        stage.ExecuteAsync(
            Arg.Any<PipelineContext>(),
            Arg.Any<IProgress<(int current, int total)>?>(),
            Arg.Any<CancellationToken>())
            .Returns(ci => execute(
                ci.Arg<PipelineContext>(),
                ci.Arg<IProgress<(int current, int total)>?>(),
                ci.Arg<CancellationToken>()));
        return stage;
    }
}
