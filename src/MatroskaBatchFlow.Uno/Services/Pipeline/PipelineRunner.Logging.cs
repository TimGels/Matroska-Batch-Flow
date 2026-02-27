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

    [LoggerMessage(Level = LogLevel.Information, Message = "Pipeline aborted after stage '{StageName}' — no further stages will run")]
    private partial void LogPipelineAborted(string stageName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A pipeline run is already in progress; the new run will wait until it completes")]
    private partial void LogRunQueued();
}
