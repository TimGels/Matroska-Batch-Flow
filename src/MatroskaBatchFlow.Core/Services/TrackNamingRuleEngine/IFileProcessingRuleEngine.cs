namespace MatroskaBatchFlow.Core.Services.TrackNamingRuleEngine
{
    /// <summary>
    /// Defines the contract for applying file processing rules of scanned files to batch configuration.
    /// </summary>
    public interface IFileProcessingRuleEngine
    {
        public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig);
    }
}
