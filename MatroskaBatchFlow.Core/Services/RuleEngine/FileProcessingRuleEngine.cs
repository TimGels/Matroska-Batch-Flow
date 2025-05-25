namespace MatroskaBatchFlow.Core.Services.RuleEngine
{
    /// <summary>
    /// Applies a set of rules to process scanned file information and update batch configuration.
    /// </summary>
    internal class FileProcessingRuleEngine
    {
        private readonly List<IFileProcessingRule> _rules;

        public FileProcessingRuleEngine(IEnumerable<IFileProcessingRule> rules)
        {
            _rules = new List<IFileProcessingRule>(rules);
        }

        /// <summary>
        /// Applies all registered rules to the scanned file and batch configuration.
        /// </summary>
        public void ApplyRules(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
        {
            foreach (var rule in _rules)
            {
                rule.Apply(scannedFile, batchConfig);
            }
        }
    }
}
