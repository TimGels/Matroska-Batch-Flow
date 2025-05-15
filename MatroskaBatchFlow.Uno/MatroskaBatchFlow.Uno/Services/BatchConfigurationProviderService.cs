namespace MatroskaBatchFlow.Uno.Services;

using MatroskaBatchFlow.Core;

public class BatchConfigurationProviderService
{
    private BatchConfiguration _batchConfiguration;

    public BatchConfigurationProviderService()
    {
        // Initialize the BatchConfiguration with default values
        _batchConfiguration = new BatchConfiguration();
    }

    /// <summary>
    /// Retrieves the current BatchConfiguration instance.
    /// </summary>
    /// <returns>The current BatchConfiguration.</returns>
    public BatchConfiguration GetConfiguration()
    {
        return _batchConfiguration;
    }

    /// <summary>
    /// Updates the current BatchConfiguration instance.
    /// </summary>
    /// <param name="configuration">The new BatchConfiguration to set.</param>
    public void UpdateConfiguration(BatchConfiguration configuration)
    {
        _batchConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Resets the BatchConfiguration to its default state.
    /// </summary>
    public void ResetConfiguration()
    {
        _batchConfiguration = new BatchConfiguration();
    }
}
