using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing;

/// <summary>
/// Defines the contract for applying file processing rules of scanned files to batch configuration.
/// </summary>
public interface IFileProcessingEngine
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig);
}
