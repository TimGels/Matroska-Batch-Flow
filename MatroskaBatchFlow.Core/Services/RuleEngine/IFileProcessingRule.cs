namespace MatroskaBatchFlow.Core.Services.RuleEngine
{
    /// <summary>
    /// Represents a rule that processes a scanned file and updates the batch configuration accordingly.
    /// </summary>
    internal interface IFileProcessingRule
    {
        /// <summary>
        /// Applies the rule to the given scanned file and batch configuration.
        /// </summary>
        /// <param name="scannedFile">The scanned file information.</param>
        /// <param name="batchConfig">The batch configuration to update.</param>
        void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig);
    }
}
