using System.Diagnostics.CodeAnalysis;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Ensures that operations which change or process the batch never run at the same time.
/// </summary>
/// <remarks>
/// Pipeline runs (import, removal, revalidation) and batch processing all enter this gate. Processing builds each file's
/// arguments from the live configuration, so it must not overlap a pipeline run that changes files or track intents.
/// Enter and leave the gate on the UI thread; <see cref="IsBusyChanged"/> is raised on the thread that enters or leaves it.
/// </remarks>
public interface IBatchOperationGate
{
    /// <summary>
    /// Gets whether an operation currently holds the gate.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Raised after an operation enters or leaves the gate.
    /// </summary>
    event EventHandler? IsBusyChanged;

    /// <summary>
    /// Waits until the gate is free and enters it.
    /// </summary>
    /// <param name="ct">A token to stop waiting.</param>
    /// <returns>A handle that leaves the gate when disposed.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="ct"/> is canceled before the gate is entered.</exception>
    Task<IDisposable> EnterAsync(CancellationToken ct = default);

    /// <summary>
    /// Enters the gate if it is free, without waiting.
    /// </summary>
    /// <param name="handle">A handle that leaves the gate when disposed, or <see langword="null"/> if the gate is busy.</param>
    /// <returns><see langword="true"/> if the gate was entered; otherwise, <see langword="false"/>.</returns>
    bool TryEnter([NotNullWhen(true)] out IDisposable? handle);
}
