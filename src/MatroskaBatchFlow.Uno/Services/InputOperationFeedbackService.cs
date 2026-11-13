using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Services;

/// <inheritdoc />
public sealed class InputOperationFeedbackService : IInputOperationFeedbackService
{
    private OperationOverlayState _currentState = OperationOverlayState.Inactive;

    public OperationOverlayState CurrentState => _currentState;

    public event EventHandler? StateChanged;

    public void Publish(OperationOverlayState state)
    {
        _currentState = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Publish(OperationOverlayState.Inactive);
    }
}
