namespace MatroskaBatchFlow.Uno.Models;

public sealed record InputOperationOverlayState(string Message, bool IsIndeterminate, int Current, int Total, bool BlocksInput)
{
    /// <summary>
    /// Gets whether the overlay should be visible. Derived from <see cref="Message"/>.
    /// </summary>
    public bool IsActive => !string.IsNullOrEmpty(Message);

    /// <summary>
    /// A sentinel value representing an inactive (hidden) overlay with no message or progress.
    /// </summary>
    public static InputOperationOverlayState Inactive { get; } = new(string.Empty, true, 0, 0, false);
}
