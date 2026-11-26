namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// The saved result IDs captured before showing a removal confirmation.
/// </summary>
/// <param name="RunIds">The results to remove. Results added after the confirmation was shown are not included.</param>
public sealed record HistoryRemoval(ImmutableHashSet<Guid> RunIds);

/// <summary>
/// The outcome of removing results from the batch history.
/// </summary>
public enum HistoryRemovalStatus
{
    /// <summary>
    /// None of the captured results remain listed or saved.
    /// </summary>
    Removed,

    /// <summary>
    /// Reading or saving the history failed, or recovery must resolve missing results first. The listed results are
    /// not changed.
    /// </summary>
    Failed
}

/// <summary>
/// The result of removing results from the batch history.
/// </summary>
/// <param name="Status">Whether the results were removed.</param>
/// <param name="RemovedCount">The number of captured results that were still listed and are now removed; zero when removal failed.</param>
public readonly record struct HistoryRemovalResult(HistoryRemovalStatus Status, int RemovedCount = 0);
