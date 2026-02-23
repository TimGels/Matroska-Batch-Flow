using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;
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

        await _runner.RunAsync([abortingStage, subsequentStage], context);

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

        await _runner.RunAsync([stage1, stage2], context);

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

        await _runner.RunAsync([abortingStage], context);

        _feedbackService.Received(1).Clear();
    }

    private static IPipelineStage CreateStage(
        string displayName,
        Func<PipelineContext, IProgress<(int current, int total)>?, CancellationToken, Task> execute,
        bool showsOverlay = true)
    {
        var stage = Substitute.For<IPipelineStage>();
        stage.DisplayName.Returns(displayName);
        stage.IsIndeterminate.Returns(true);
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
