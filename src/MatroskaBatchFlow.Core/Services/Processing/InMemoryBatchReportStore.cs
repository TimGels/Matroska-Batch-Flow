using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// In-memory store for batch execution reports.
/// </summary>
public class InMemoryBatchReportStore : IBatchReportStore
{
    private readonly ObservableCollection<BatchExecutionReport> _batches = [];
    private BatchExecutionReport _activeBatch = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public BatchExecutionReport ActiveBatch
    {
        get => _activeBatch;
        private set
        {
            if (value == _activeBatch)
                return;

            _activeBatch = value;
            OnPropertyChanged(nameof(ActiveBatch));
        }
    }

    public ReadOnlyObservableCollection<BatchExecutionReport> Batches { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBatchReportStore"/> class.
    /// </summary>
    public InMemoryBatchReportStore()
    {
        Batches = new ReadOnlyObservableCollection<BatchExecutionReport>(_batches);
    }

    /// <inheritdoc/>
    public void SetActiveBatch(BatchExecutionReport batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ActiveBatch = batch;
    }

    /// <inheritdoc/>
    public BatchExecutionReport CreateBatch()
    {
        var batch = new BatchExecutionReport();
        _batches.Add(batch);
        return batch;
    }

    /// <inheritdoc/>
    public BatchExecutionReport? GetBatchById(Guid batchId)
    {
        foreach (var batch in _batches)
        {
            if (batch.Id == batchId)
            {
                return batch;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public void RemoveBatch(BatchExecutionReport batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        _batches.Remove(batch);
        batch.Clear();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        ActiveBatch = new BatchExecutionReport();
        foreach (var batch in _batches)
        {
            batch.Clear();
        }
        _batches.Clear();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
