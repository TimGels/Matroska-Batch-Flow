namespace MatroskaBatchFlow.Core.Services.TrackNamingRuleEngine;

/// <summary>
/// Represents a rule that processes a scanned file and updates the batch configuration accordingly.
/// </summary>
public interface IFileProcessingRule
{
    /// <summary>
    /// Applies the rule to the given scanned file and batch configuration.
    /// </summary>
    /// <param name="scannedFile">The scanned file information.</param>
    /// <param name="batchConfig">The batch configuration to update.</param>
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig);
}
