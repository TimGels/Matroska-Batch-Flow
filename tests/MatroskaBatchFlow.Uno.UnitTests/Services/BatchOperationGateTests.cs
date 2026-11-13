using MatroskaBatchFlow.Uno.Services;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

/// <summary>
/// Contains unit tests for the <see cref="BatchOperationGate"/> class.
/// </summary>
public class BatchOperationGateTests
{
    [Fact]
    public void TryEnter_Fails_WhileHeld_AndSucceeds_AfterRelease()
    {
        // Arrange
        var gate = new BatchOperationGate();
        var busyStates = new List<bool>();
        gate.IsBusyChanged += (_, _) => busyStates.Add(gate.IsBusy);

        // Act & Assert
        Assert.True(gate.TryEnter(out var first));
        Assert.False(gate.TryEnter(out var second));
        Assert.Null(second);

        first.Dispose();
        Assert.True(gate.TryEnter(out var third));
        third.Dispose();

        Assert.Equal([true, false, true, false], busyStates);
    }

    [Fact]
    public void Dispose_LeavesTheGateOnlyOnce()
    {
        // Arrange
        var gate = new BatchOperationGate();
        Assert.True(gate.TryEnter(out var first));
        first.Dispose();
        Assert.True(gate.TryEnter(out var second));

        // Act - disposing the old handle again must not release the gate held by the second handle
        first.Dispose();

        // Assert
        Assert.True(gate.IsBusy);
        Assert.False(gate.TryEnter(out _));
        second.Dispose();
    }
}
