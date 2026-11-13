namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// Describes what an <see cref="Presentation.Controls.OperationOverlay"/> shows while a long-running operation is active.
/// </summary>
/// <param name="Message">The main message. The overlay is hidden when it is empty.</param>
/// <param name="IsIndeterminate">Whether the progress bar shows activity without a known amount of progress.</param>
/// <param name="Current">The current item number, shown as "Current / Total".</param>
/// <param name="Total">The total number of items. The counter is hidden when it is 0.</param>
/// <param name="BlocksInput">Whether the overlay blocks pointer input to the content underneath.</param>
public sealed record OperationOverlayState(string Message, bool IsIndeterminate, int Current, int Total, bool BlocksInput)
{
    /// <summary>
    /// Gets an optional line shown below the message, for example the name of the file being processed.
    /// </summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// Gets the progress between 0 and 1. When <see langword="null"/>, progress is derived from
    /// <see cref="Current"/> and <see cref="Total"/>.
    /// </summary>
    public double? Progress { get; init; }

    /// <summary>
    /// Gets whether a cancellation was requested and the operation is stopping.
    /// </summary>
    public bool IsCancelling { get; init; }

    /// <summary>
    /// Gets whether the overlay should be visible. Derived from <see cref="Message"/>.
    /// </summary>
    public bool IsActive => !string.IsNullOrEmpty(Message);

    /// <summary>
    /// Gets the progress as a percentage between 0 and 100, for binding to a progress bar.
    /// </summary>
    public double ProgressPercent => Math.Clamp(Progress ?? (Total > 0 ? (double)Current / Total : 0d), 0d, 1d) * 100d;

    /// <summary>
    /// A sentinel value representing an inactive (hidden) overlay with no message or progress.
    /// </summary>
    public static OperationOverlayState Inactive { get; } = new(string.Empty, true, 0, 0, false);
}
