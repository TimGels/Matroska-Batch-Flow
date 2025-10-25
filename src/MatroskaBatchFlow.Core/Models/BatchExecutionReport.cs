using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Represents a summary report of the execution status of a batch processing operation,
/// including individual file processing reports and aggregated statistics.
/// </summary>
public class BatchExecutionReport : INotifyPropertyChanged
{
    private readonly ObservableCollection<FileProcessingReport> _fileReports = [];

    private int _total;
    private int _succeeded;
    private int _warnings;
    private int _failed;
    private int _running;
    private int _pending;
    private int _skipped;
    private int _canceled;

    public ReadOnlyObservableCollection<FileProcessingReport> FileReports { get; }

    public Guid Id { get; } = Guid.NewGuid();

    public int Total => _total;
    public int Succeeded => _succeeded;
    public int Warnings => _warnings;
    public int Failed => _failed;
    public int Running => _running;
    public int Pending => _pending;
    public int Skipped => _skipped;
    public int Canceled => _canceled;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchExecutionReport"/> class.
    /// </summary>
    public BatchExecutionReport()
    {
        FileReports = new ReadOnlyObservableCollection<FileProcessingReport>(_fileReports);
        _fileReports.CollectionChanged += (_, e) =>
        {
            if (e.Action is NotifyCollectionChangedAction.Add
                or NotifyCollectionChangedAction.Remove
                or NotifyCollectionChangedAction.Reset
                or NotifyCollectionChangedAction.Replace)
            {
                Recompute();
            }

            if (e.NewItems is not null)
            {
                foreach (FileProcessingReport r in e.NewItems)
                {
                    r.PropertyChanged += FileReportChanged;
                }
            }

            if (e.OldItems is not null)
            {
                foreach (FileProcessingReport r in e.OldItems)
                {
                    r.PropertyChanged -= FileReportChanged;
                }
            }
        };
    }

    /// <summary>
    /// Attempts to add a new file processing report to the collection.
    /// </summary>
    /// <param name="report">The file processing report to add.</param>
    /// <returns><see langword="true"/> if the report was added; <see langword="false"/> 
    /// if a report with the same Id already exists.</returns>
    public bool TryAddFileReport(FileProcessingReport report)
    {
        if (_fileReports.Any(r => r.Id == report.Id))
        {
            return false;
        }

        _fileReports.Add(report);
        {
            return true;
        }
    }

    /// <summary>
    /// Clears all file reports from the collection and detaches associated event handlers.
    /// </summary>
    public void Clear()
    {
        if (_fileReports.Count == 0)
            return;

        foreach (var report in _fileReports)
        {
            report.PropertyChanged -= FileReportChanged;
        }

        _fileReports.Clear();
    }

    /// <summary>
    /// Gets a file processing report by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the file processing report.</param>
    /// <returns>The file processing report, or <see langword="null"/> if not found.</returns>
    public FileProcessingReport? GetFileReportById(Guid id)
    {
        return _fileReports.FirstOrDefault(r => r.Id == id);
    }

    /// <summary>
    /// Handles the PropertyChanged event of a FileProcessingReport.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="eventArgs">The event data.</param>
    private void FileReportChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(FileProcessingReport.Status))
        {
            Recompute();
        }
    }

    /// <summary>
    /// Recomputes all summary properties based on the current state of the file reports.
    /// </summary>
    private void Recompute()
    {
        Set(ref _total, _fileReports.Count, nameof(Total));
        Set(ref _succeeded, _fileReports.Count(r => r.Status == ProcessingStatus.Succeeded), nameof(Succeeded));
        Set(ref _warnings, _fileReports.Count(r => r.Status == ProcessingStatus.SucceededWithWarnings), nameof(Warnings));
        Set(ref _failed, _fileReports.Count(r => r.Status == ProcessingStatus.Failed), nameof(Failed));
        Set(ref _running, _fileReports.Count(r => r.Status == ProcessingStatus.Running), nameof(Running));
        Set(ref _pending, _fileReports.Count(r => r.Status == ProcessingStatus.Pending), nameof(Pending));
        Set(ref _skipped, _fileReports.Count(r => r.Status == ProcessingStatus.Skipped), nameof(Skipped));
        Set(ref _canceled, _fileReports.Count(r => r.Status == ProcessingStatus.Canceled), nameof(Canceled));
    }

    /// <summary>
    /// Sets the field to the specified value and raises the PropertyChanged event if the value has changed.
    /// </summary>
    /// <param name="field">The backing field to set.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">The name of the property to raise the event for.</param>
    private void Set(ref int field, int value, string propertyName)
    {
        if (field == value)
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
