using MatroskaBatchFlow.Core.Services.Processing;

namespace MatroskaBatchFlow.Core.UnitTests.Services.Processing;

/// <summary>
/// Contains unit tests for the <see cref="MkvPropeditStopGate"/> class.
/// </summary>
public class MkvPropeditStopGateTests
{
    [Fact]
    public void RequestStop_StopsImmediately_WhileReading()
    {
        using var gate = new MkvPropeditStopGate();
        gate.ReportProgress(30);

        gate.RequestStop();

        Assert.True(gate.StopToken.IsCancellationRequested);
    }

    [Fact]
    public void RequestStop_DoesNotStop_AfterReadingFinished()
    {
        using var gate = new MkvPropeditStopGate();
        gate.ReportProgress(30);
        gate.ReportProgress(100);

        gate.RequestStop();

        Assert.False(gate.StopToken.IsCancellationRequested);
    }

    [Fact]
    public void RequestStop_DoesNotStop_WhenNoProgressWasReported()
    {
        using var gate = new MkvPropeditStopGate();

        gate.RequestStop();

        Assert.False(gate.StopToken.IsCancellationRequested);
    }

    [Fact]
    public void ReportProgress_AppliesEarlierStopRequest_WhenReadingStarts()
    {
        using var gate = new MkvPropeditStopGate();
        gate.RequestStop();

        gate.ReportProgress(0);

        Assert.True(gate.StopToken.IsCancellationRequested);
    }
}
