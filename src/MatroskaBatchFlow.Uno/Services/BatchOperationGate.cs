using System.Diagnostics.CodeAnalysis;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Services;

/// <inheritdoc />
public sealed class BatchOperationGate : IBatchOperationGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc />
    public bool IsBusy => _semaphore.CurrentCount == 0;

    /// <inheritdoc />
    public event EventHandler? IsBusyChanged;

    /// <inheritdoc />
    public async Task<IDisposable> EnterAsync(CancellationToken ct = default)
    {
        // Continues on the caller's context, so IsBusyChanged is raised on the UI thread.
        await _semaphore.WaitAsync(ct);
        return Entered();
    }

    /// <inheritdoc />
    public bool TryEnter([NotNullWhen(true)] out IDisposable? handle)
    {
        handle = _semaphore.Wait(0) ? Entered() : null;
        return handle is not null;
    }

    private Handle Entered()
    {
        IsBusyChanged?.Invoke(this, EventArgs.Empty);
        return new Handle(this);
    }

    private void Leave()
    {
        _semaphore.Release();
        IsBusyChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Leaves the gate once, however often it is disposed.
    /// </summary>
    private sealed class Handle(BatchOperationGate gate) : IDisposable
    {
        private int _isDisposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
                gate.Leave();
        }
    }
}
