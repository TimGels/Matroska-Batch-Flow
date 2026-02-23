using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Provides a mechanism for pipeline stages to communicate overlay feedback state to the UI.
/// </summary>
/// <remarks>
/// The <see cref="IPipelineRunner"/> publishes overlay state before and during stage execution.
/// Views subscribe to <see cref="StateChanged"/> to show or hide the overlay control.
/// </remarks>
public interface IInputOperationFeedbackService
{
    /// <summary>
    /// Gets the most recently published overlay state.
    /// </summary>
    InputOperationOverlayState CurrentState { get; }

    /// <summary>
    /// Raised whenever the overlay state changes, including when cleared.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Publishes a new overlay state and raises <see cref="StateChanged"/>.
    /// </summary>
    /// <param name="state">The overlay state to publish.</param>
    void Publish(InputOperationOverlayState state);

    /// <summary>
    /// Resets the overlay state to <see cref="InputOperationOverlayState.Inactive"/> and raises <see cref="StateChanged"/>.
    /// </summary>
    void Clear();
}
