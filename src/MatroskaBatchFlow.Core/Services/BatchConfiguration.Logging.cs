using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

public partial class BatchConfiguration
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "File marked as stale (needs re-scanning): {FilePath}")]
    private partial void LogFileMarkedAsStale(string filePath);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Stale flag cleared for file: {FilePath}")]
    private partial void LogStaleFlagCleared(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, 
        Message = "Migrated file configuration from {OldFileId} to {NewFileId}")]
    private partial void LogFileConfigurationMigrated(Guid oldFileId, Guid newFileId);

    [LoggerMessage(Level = LogLevel.Debug, 
        Message = "Migration skipped: no configuration found for {OldFileId}")]
    private partial void LogMigrationSkippedNoConfiguration(Guid oldFileId);
}
