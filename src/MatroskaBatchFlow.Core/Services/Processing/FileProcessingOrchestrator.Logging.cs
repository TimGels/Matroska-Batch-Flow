using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// LoggerMessage definitions for <see cref="FileProcessingOrchestrator"/>.
/// </summary>
public partial class FileProcessingOrchestrator
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting batch processing for {FileCount} file(s)")]
    private partial void LogBatchStart(int fileCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch processing completed in {Duration}ms. Succeeded: {Succeeded}, Failed: {Failed}, Canceled: {Canceled}")]
    private partial void LogBatchCompleted(long duration, int succeeded, int failed, int canceled);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing canceled before start for file: {FilePath}")]
    private partial void LogProcessingCanceledBeforeStart(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting processing for file: {FilePath}")]
    private partial void LogFileProcessingStart(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping file (no modifications requested): {FilePath}")]
    private partial void LogFileSkipped(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "mkvpropedit failed for {FilePath}: {Error}")]
    private partial void LogMkvpropeditFailed(string filePath, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "mkvpropedit completed with warnings for {FilePath}: {Warnings}")]
    private partial void LogMkvpropeditWarnings(string filePath, string warnings);

    [LoggerMessage(Level = LogLevel.Debug, Message = "File processing completed successfully: {FilePath}")]
    private partial void LogFileProcessingCompleted(string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing canceled for file: {FilePath}")]
    private partial void LogProcessingCanceled(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error processing file: {FilePath}")]
    private partial void LogUnexpectedError(Exception ex, string filePath);
}
