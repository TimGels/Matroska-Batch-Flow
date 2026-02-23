using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Services;

/// <inheritdoc />
public sealed class InputOperationFeedbackService : IInputOperationFeedbackService
{
    private InputOperationOverlayState _currentState = InputOperationOverlayState.Inactive;

    public InputOperationOverlayState CurrentState => _currentState;

    public event EventHandler? StateChanged;

    public void Publish(InputOperationOverlayState state)
    {
        _currentState = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Publish(InputOperationOverlayState.Inactive);
    }
}
