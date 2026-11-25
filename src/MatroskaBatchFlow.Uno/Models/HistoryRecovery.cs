namespace MatroskaBatchFlow.Uno.Models;

/// <summary>A choice about the exact previously saved results missing from repaired history.</summary>
public sealed record HistoryRecoveryDecision(ImmutableHashSet<Guid> MissingIds, bool RestoreMissing);

public enum HistoryRecoveryStatus
{
    Recovered,
    Failed,
    ChooseMissingResults,
    ConfirmUnreadableReset
}

public sealed record HistoryRecoveryResult(HistoryRecoveryStatus Status)
{
    public ImmutableHashSet<Guid> MissingIds { get; init; } = [];
}
