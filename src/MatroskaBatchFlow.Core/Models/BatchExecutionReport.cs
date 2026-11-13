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
    private readonly HashSet<Guid> _reportIds = [];

    /// <summary>
    /// Set while <see cref="AddFileReports"/> runs, so the summary is recomputed once instead of after every report.
    /// </summary>
    private bool _isAddingRange;

    private int _total;
    private int _succeeded;
    private int _warnings;
    private int _failed;
    private int _running;
    private int _pending;
    private int _skipped;
    private int _canceled;
    private int _completed;
    private FileProcessingReport? _currentFile;
    private double _overallProgress;

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

    /// <summary>
    /// Gets the number of files that finished processing: succeeded, failed or skipped. Canceled files are not included.
    /// </summary>
    public int Completed => _completed;

    /// <summary>
    /// Gets the file that is currently being processed, or <see langword="null"/> if no file is running.
    /// </summary>
    public FileProcessingReport? CurrentFile => _currentFile;

    /// <summary>
    /// Gets the overall progress of the batch between 0 and 1, including the progress of the current file.
    /// </summary>
    /// <remarks>Canceled files do not count as progress, so a canceled batch stays below 1.</remarks>
    public double OverallProgress => _overallProgress;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchExecutionReport"/> class.
    /// </summary>
    public BatchExecutionReport()
    {
        FileReports = new ReadOnlyObservableCollection<FileProcessingReport>(_fileReports);
        _fileReports.CollectionChanged += (_, e) =>
        {
            if (!_isAddingRange
                && e.Action is NotifyCollectionChangedAction.Add
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
        if (!_reportIds.Add(report.Id))
            return false;

        _fileReports.Add(report);
        return true;
    }

    /// <summary>
    /// Adds the reports that are not in the batch yet, and recomputes the summary properties once afterwards.
    /// </summary>
    /// <remarks>Use this for many reports at once: adding them one by one recomputes the summary after every report.</remarks>
    /// <param name="reports">The reports to add.</param>
    /// <returns>The reports that were added; reports with an Id that already exists are skipped.</returns>
    public IReadOnlyList<FileProcessingReport> AddFileReports(IEnumerable<FileProcessingReport> reports)
    {
        var added = new List<FileProcessingReport>();

        _isAddingRange = true;
        try
        {
            foreach (var report in reports)
            {
                if (TryAddFileReport(report))
                    added.Add(report);
            }
        }
        finally
        {
            _isAddingRange = false;
            Recompute();
        }

        return added;
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

        _reportIds.Clear();
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
        else if (eventArgs.PropertyName is nameof(FileProcessingReport.Progress) && ReferenceEquals(sender, _currentFile))
        {
            UpdateOverallProgress();
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
        Set(ref _completed, _fileReports.Count(r => r.Status is not (ProcessingStatus.Pending or ProcessingStatus.Running or ProcessingStatus.Canceled)), nameof(Completed));
        Set(ref _currentFile, _fileReports.FirstOrDefault(r => r.Status == ProcessingStatus.Running), nameof(CurrentFile));
        UpdateOverallProgress();
    }

    /// <summary>
    /// Updates <see cref="OverallProgress"/> from the completed files and the progress of the current file.
    /// </summary>
    private void UpdateOverallProgress()
    {
        var currentFileFraction = (_currentFile?.Progress ?? 0) / 100d;
        var progress = _total == 0 ? 0d : Math.Clamp((_completed + currentFileFraction) / _total, 0d, 1d);
        Set(ref _overallProgress, progress, nameof(OverallProgress));
    }

    /// <summary>
    /// Sets the field to the specified value and raises the PropertyChanged event if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the backing field.</typeparam>
    /// <param name="field">The backing field to set.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">The name of the property to raise the event for.</param>
    private void Set<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
