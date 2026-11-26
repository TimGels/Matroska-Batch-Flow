namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// A choice about the exact previously saved results missing from repaired history.
/// </summary>
/// <param name="MissingIds">The missing results the user decided about. The choice only applies while exactly these results are missing.</param>
/// <param name="RestoreMissing">Whether to save the missing results again; otherwise, the repaired history is kept without them.</param>
public sealed record HistoryRecoveryDecision(ImmutableHashSet<Guid> MissingIds, bool RestoreMissing);

/// <summary>
/// The outcome of an attempt to recover the batch history.
/// </summary>
public enum HistoryRecoveryStatus
{
    /// <summary>
    /// The history was read again or replaced with the listed results, so recovery is no longer required.
    /// </summary>
    Recovered,

    /// <summary>
    /// Reading or saving the history failed. The user can try again.
    /// </summary>
    Failed,

    /// <summary>
    /// The repaired history omits previously listed results. The user must choose whether to restore
    /// <see cref="HistoryRecoveryResult.MissingIds"/>.
    /// </summary>
    ChooseMissingResults,

    /// <summary>
    /// The history file became unreadable again after the user chose about missing results. The user must confirm
    /// replacing it with the listed results.
    /// </summary>
    ConfirmUnreadableReset
}

/// <summary>
/// The result of an attempt to recover the batch history.
/// </summary>
/// <param name="Status">The outcome, or the decision the user must make next.</param>
public sealed record HistoryRecoveryResult(HistoryRecoveryStatus Status)
{
    /// <summary>
    /// Gets the previously listed results that the repaired history omits, for
    /// <see cref="HistoryRecoveryStatus.ChooseMissingResults"/>; otherwise, an empty set.
    /// </summary>
    public ImmutableHashSet<Guid> MissingIds { get; init; } = [];
}
