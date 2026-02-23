using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Services;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

public class InputOperationFeedbackServiceTests
{
    [Fact]
    public void Publish_UpdatesCurrentSnapshotAndRaisesStateChanged()
    {
        var service = new InputOperationFeedbackService();
        var raised = false;
        service.StateChanged += (_, _) => raised = true;

        var state = new InputOperationOverlayState(
            "Revalidating files\u2026",
            true,
            0,
            0,
            true);

        service.Publish(state);

        Assert.True(raised);
        Assert.Equal(state, service.CurrentState);
    }

    [Fact]
    public void Clear_ResetsToInactiveState()
    {
        var service = new InputOperationFeedbackService();

        service.Publish(new InputOperationOverlayState(
            "Removing files\u2026",
            true,
            0,
            0,
            true));

        service.Clear();

        Assert.Equal(InputOperationOverlayState.Inactive, service.CurrentState);
    }

    [Fact]
    public void LateSubscriber_CanHydrateFromCurrentSnapshot()
    {
        var service = new InputOperationFeedbackService();
        var expected = new InputOperationOverlayState(
            "Applying track configuration\u2026",
            false,
            2,
            5,
            true);

        service.Publish(expected);

        InputOperationOverlayState? observed = null;
        service.StateChanged += (_, _) => observed = service.CurrentState;

        Assert.Equal(expected, service.CurrentState);
        Assert.Null(observed);
    }
}