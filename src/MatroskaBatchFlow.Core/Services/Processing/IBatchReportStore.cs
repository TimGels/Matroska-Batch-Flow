using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Defines a store for managing batch execution reports.
/// </summary>
public interface IBatchReportStore: INotifyPropertyChanged
{
    /// <summary>
    /// Gets the currently active batch execution report.
    /// </summary>
    BatchExecutionReport ActiveBatch { get; }

    /// <summary>
    /// Gets the read-only collection of all batch execution reports.
    /// </summary>
    ReadOnlyObservableCollection<BatchExecutionReport> Batches { get; }

    /// <summary>
    /// Sets the specified batch as the active batch.
    /// </summary>
    /// <param name="batch">The batch to set as active. Cannot be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="batch"/> is <see langword="null"/>.</exception>
    void SetActiveBatch(BatchExecutionReport batch);

    /// <summary>
    /// Creates a new batch execution report and adds it to the store.
    /// </summary>
    /// <returns>The newly created batch execution report.</returns>
    BatchExecutionReport CreateBatch();

    /// <summary>
    /// Resets the store by clearing all batches.
    /// </summary>
    void Reset();

    /// <summary>
    /// Retrieves a batch execution report by its unique identifier.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch.</param>
    /// <returns>The batch execution report if found; otherwise, <see langword="null"/>.</returns>
    BatchExecutionReport? GetBatchById(Guid batchId);

    /// <summary>
    /// Removes the specified batch execution report from the store.
    /// </summary>
    /// <param name="batch">The batch to remove. Cannot be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="batch"/> is <see langword="null"/>.</exception>
    void RemoveBatch(BatchExecutionReport batch);
}
