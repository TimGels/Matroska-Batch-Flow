namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="BatchOperationOrchestrator"/>.
/// </summary>
public sealed partial class BatchOperationOrchestrator
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Importing {FileCount} file(s)")]
    private partial void LogImportingFiles(int fileCount);
}
