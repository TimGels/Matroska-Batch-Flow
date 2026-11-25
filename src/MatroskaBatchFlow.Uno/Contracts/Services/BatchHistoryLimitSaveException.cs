namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Indicates that the batch history limit could not be saved after the history was updated.
/// </summary>
public sealed class BatchHistoryLimitSaveException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchHistoryLimitSaveException"/> class.
    /// </summary>
    /// <param name="historyTrimmed">Whether confirmed runs were removed from the saved history.</param>
    /// <param name="innerException">The exception raised while saving the limit.</param>
    public BatchHistoryLimitSaveException(bool historyTrimmed, Exception innerException)
        : base("The batch history limit could not be saved.", innerException)
    {
        HistoryTrimmed = historyTrimmed;
    }

    /// <summary>
    /// Gets whether confirmed runs were removed from the saved history before saving the limit failed.
    /// </summary>
    public bool HistoryTrimmed { get; }
}
