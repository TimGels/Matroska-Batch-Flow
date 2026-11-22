namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// The batch state currently relevant to the Apply changes footer.
/// </summary>
public enum BatchAvailabilityReason
{
    /// <summary>
    /// The batch has no files.
    /// </summary>
    NoFiles,

    /// <summary>
    /// A batch operation, such as an import or processing, is running.
    /// </summary>
    Busy,

    /// <summary>
    /// Unsupported files block the batch. <see cref="BatchAvailability.Count"/> is the number of unsupported files.
    /// </summary>
    UnsupportedFiles,

    /// <summary>
    /// Blocking checks other than the file format block the batch. <see cref="BatchAvailability.Count"/> is the number of distinct checks.
    /// </summary>
    BlockingChecks,

    /// <summary>
    /// Validation blocks the batch, but no unsupported files or blocking checks could be counted.
    /// </summary>
    ValidationBlocked,

    /// <summary>
    /// Nothing blocks the batch, but no changes have been configured.
    /// </summary>
    NoChanges,

    /// <summary>
    /// Changes can be applied. Some checks have findings, but none of them block the batch.
    /// </summary>
    ReadyWithAdvisories,

    /// <summary>
    /// Changes can be applied and no checks have findings.
    /// </summary>
    Ready
}

/// <summary>
/// The highest-priority batch status, independent of how the UI describes it.
/// </summary>
/// <param name="Reason">The state to present.</param>
/// <param name="Count">Number of unsupported files or distinct blocking checks, according to the reason; otherwise zero.</param>
public sealed record BatchAvailability(BatchAvailabilityReason Reason, int Count = 0);
