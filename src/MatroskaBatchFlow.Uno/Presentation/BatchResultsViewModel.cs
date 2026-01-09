using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Services.Processing;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// ViewModel for displaying batch processing results.
/// </summary>
public partial class BatchResultsViewModel : ObservableObject
{
    private readonly IBatchReportStore _batchReportStore;

    public ReadOnlyObservableCollection<FileProcessingReport> FileReports => currentBatch.FileReports;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileReports))]
    private BatchExecutionReport currentBatch;

    [ObservableProperty] 
    private int total;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SucceededTotal))] 
    private int succeeded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SucceededTotal))] 
    private int warnings;

    [ObservableProperty] 
    private int failed;

    [ObservableProperty] 
    private int running;

    [ObservableProperty] 
    private int pending;

    [ObservableProperty] 
    private int skipped;

    [ObservableProperty] 
    private int canceled;

    public int SucceededTotal => Succeeded + Warnings;

    public BatchResultsViewModel(IBatchReportStore batchReportStore)
    {
        _batchReportStore = batchReportStore;
        CurrentBatch = _batchReportStore.ActiveBatch;

        SetupEventHandlers();
        RefreshAfterBatchSwap();
    }

    private void SetupEventHandlers()
    {
        _batchReportStore.PropertyChanged += OnStorePropertyChanged;
        CurrentBatch.PropertyChanged += OnBatchReportPropertyChanged;
    }

    private void OnStorePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(IBatchReportStore.ActiveBatch))
        {
            // Switch batch subscription.
            CurrentBatch.PropertyChanged -= OnBatchReportPropertyChanged;
            CurrentBatch = _batchReportStore.ActiveBatch;
            CurrentBatch.PropertyChanged += OnBatchReportPropertyChanged;

            RefreshAfterBatchSwap();
        }
    }

    /// <summary>
    /// Refreshes all properties after the active batch has been swapped in the store.
    /// </summary>
    private void RefreshAfterBatchSwap()
    {
        OnPropertyChanged(nameof(CurrentBatch));
        OnPropertyChanged(nameof(FileReports));

        // Copy all counters from the current batch.
        Total = CurrentBatch.Total;
        Succeeded = CurrentBatch.Succeeded;
        Warnings = CurrentBatch.Warnings;
        Failed = CurrentBatch.Failed;
        Running = CurrentBatch.Running;
        Pending = CurrentBatch.Pending;
        Skipped = CurrentBatch.Skipped;
        Canceled = CurrentBatch.Canceled;
    }

    /// <summary>
    /// Handles the <see cref="BatchExecutionReport.PropertyChanged"/>. Updates the corresponding properties
    /// of the current batch based on the property that changed. Synchronizes only the changed property to avoid unnecessary updates.
    /// </summary>
    /// <param name="sender">The source of the event. This parameter is typically the batch execution report.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed.</param>
    private void OnBatchReportPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        switch (eventArgs.PropertyName)
        {
            case nameof(BatchExecutionReport.Total):
                Total = CurrentBatch.Total;
                break;
            case nameof(BatchExecutionReport.Failed):
                Failed = CurrentBatch.Failed;
                break;
            case nameof(BatchExecutionReport.Running):
                Running = CurrentBatch.Running;
                break;
            case nameof(BatchExecutionReport.Pending):
                Pending = CurrentBatch.Pending;
                break;
            case nameof(BatchExecutionReport.Skipped):
                Skipped = CurrentBatch.Skipped;
                break;
            case nameof(BatchExecutionReport.Canceled):
                Canceled = CurrentBatch.Canceled;
                break;
            case nameof(BatchExecutionReport.Succeeded):
                Succeeded = CurrentBatch.Succeeded;
                break;
            case nameof(BatchExecutionReport.Warnings):
                Warnings = CurrentBatch.Warnings;
                break;
        }
    }
}
