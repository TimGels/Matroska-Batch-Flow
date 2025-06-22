namespace MatroskaBatchFlow.Core.Services.TrackNamingRuleEngine
{
    /// <summary>
    /// Applies a set of rules to process scanned file information and update batch configuration.
    /// </summary>
    public class FileProcessingRuleEngine(IEnumerable<IFileProcessingRule> rules): IFileProcessingRuleEngine
    {
        private readonly List<IFileProcessingRule> _rules = [.. rules];

        /// <summary>
        /// Applies all registered rules to the scanned file and batch configuration.
        /// </summary>
        public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
        {
            foreach (var rule in _rules)
            {
                rule.Apply(scannedFile, batchConfig);
            }
        }
    }
}
