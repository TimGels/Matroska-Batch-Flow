namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// LoggerMessage definitions for <see cref="FilterDuplicateFilesStage"/>.
/// </summary>
public sealed partial class FilterDuplicateFilesStage
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipped {DuplicateCount} duplicate file(s)")]
    private partial void LogDuplicatesSkipped(int duplicateCount);
}
