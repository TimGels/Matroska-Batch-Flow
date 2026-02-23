namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// LoggerMessage definitions for <see cref="PipelineRunner"/>.
/// </summary>
public sealed partial class PipelineRunner
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Pipeline stage '{StageName}' starting ({StageNumber}/{TotalStages})")]
    private partial void LogStageStarting(string stageName, int stageNumber, int totalStages);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Pipeline stage '{StageName}' completed")]
    private partial void LogStageCompleted(string stageName);
}
