using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing;

/// <summary>
/// Applies a set of rules to process scanned file information and update batch configuration.
/// </summary>
public class FileProcessingEngine(IEnumerable<IFileProcessingRule> rules) : IFileProcessingEngine
{
    private readonly List<IFileProcessingRule> _rules = [.. rules];

    /// <summary>
    /// Applies all registered rules to the scanned file and batch configuration.
    /// </summary>
    /// <remarks>
    /// Unsupported files cannot be edited, so they never seed defaults.
    /// </remarks>
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile is not { IsSupported: true })
            return;

        foreach (var rule in _rules)
        {
            rule.Apply(scannedFile, batchConfig);
        }
    }
}
