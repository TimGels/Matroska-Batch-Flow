namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Decides when a running mkvpropedit process may be stopped after cancellation was requested.
/// </summary>
/// <remarks>
/// mkvpropedit reports progress only while it reads the file, for example to calculate track statistics.
/// Stopping it while the last reported progress is below 100% leaves the file unchanged. Outside that window
/// it may be writing, so a stop request is held back and the process is allowed to finish on its own.
/// A request made before reading starts is applied as soon as progress below 100% is reported.
/// <para>
/// Stop requests and progress reports arrive on different threads, so all state changes are synchronized.
/// </para>
/// </remarks>
internal sealed class MkvPropeditStopGate : IDisposable
{
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _stopSource = new();
    private bool _isReading;
    private bool _isStopRequested;
    private bool _isDisposed;

    /// <summary>
    /// Gets a token that is canceled when the process should be stopped.
    /// </summary>
    public CancellationToken StopToken => _stopSource.Token;

    /// <summary>
    /// Requests that the process is stopped as soon as that is safe.
    /// </summary>
    public void RequestStop()
    {
        lock (_lock)
        {
            _isStopRequested = true;
            StopIfSafe();
        }
    }

    /// <summary>
    /// Records a progress percentage reported by mkvpropedit.
    /// </summary>
    /// <param name="percent">The reported percentage.</param>
    public void ReportProgress(int percent)
    {
        lock (_lock)
        {
            _isReading = percent < 100;
            StopIfSafe();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _stopSource.Dispose();
        }
    }

    private void StopIfSafe()
    {
        // Canceling inside the lock ensures progress cannot move past the read phase while the stop is applied.
        if (_isStopRequested && _isReading && !_isDisposed && !_stopSource.IsCancellationRequested)
        {
            _stopSource.Cancel();
        }
    }
}
