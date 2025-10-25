namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Represents the status of a processing operation.
/// </summary>
public enum ProcessingStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    SucceededWithWarnings = 3,
    Failed = 4,
    Skipped = 5,
    Canceled = 6
}
