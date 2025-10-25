using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Orchestrates the processing of scannedFiles using mkvpropedit based on <see cref="IBatchConfiguration"/>' settings.
/// </summary>
public interface IFileProcessingOrchestrator
{
    /// <summary>
    /// Processes a single video scannedFile.
    /// </summary>
    /// <param name="fileReport">The <see cref="FileProcessingReport"/> representing the scannedFile to be processed.</param>
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while processing the scannedFile. The operation will be
    /// canceled if the token is triggered.</param>
    /// <returns>A <see cref="FileProcessingReport"/> of the processing result.</returns>
    public Task<FileProcessingReport> ProcessFileAsync(FileProcessingReport fileReport, CancellationToken ct = default);

    /// <summary>
    /// Processes a collection of <see cref="ScannedFileInfo"/> objects asynchronously.
    /// </summary>
    /// <param name="scannedFiles">A collection of <see cref="ScannedFileInfo"/> objects representing the scannedFiles to be processed.</param>
    /// <param name="ct">An optional <see cref="CancellationToken"/> to observe while processing the scannedFiles. The operation will be
    /// canceled if the token is triggered.</param>
    /// <returns>A list of <see cref="FileProcessingReport"/> objects representing the results of processing each scannedFile.</returns>
    public Task<List<FileProcessingReport>> ProcessAllAsync(IEnumerable<ScannedFileInfo> scannedFiles, CancellationToken ct = default);
}
