namespace MatroskaBatchFlow.Uno.Models;

/// <summary>The batch state currently relevant to the Apply changes footer.</summary>
public enum BatchAvailabilityReason
{
    NoFiles,
    Busy,
    UnsupportedFiles,
    BlockingChecks,
    ValidationBlocked,
    NoChanges,
    ReadyWithAdvisories,
    Ready
}

/// <summary>The highest-priority batch status, independent of how the UI describes it.</summary>
/// <param name="Reason">The state to present.</param>
/// <param name="Count">Number of unsupported files or distinct blocking checks, according to the reason; otherwise zero.</param>
public sealed record BatchAvailability(BatchAvailabilityReason Reason, int Count = 0);
