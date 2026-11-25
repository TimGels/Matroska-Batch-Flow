namespace MatroskaBatchFlow.Uno.Models;

/// <summary>The saved result IDs captured before showing a removal confirmation.</summary>
public sealed record HistoryRemoval(ImmutableHashSet<Guid> RunIds);

public enum HistoryRemovalStatus
{
    Removed,
    Failed
}

public readonly record struct HistoryRemovalResult(HistoryRemovalStatus Status, int RemovedCount = 0);
