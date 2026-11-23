using System.ComponentModel;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// ViewModel for displaying batch processing results.
/// </summary>
public partial class BatchResultsViewModel : ObservableObject
{
    private static readonly FileResultFilter[] FilterOrder =
    [
        FileResultFilter.Failed,
        FileResultFilter.Warnings,
        FileResultFilter.Canceled,
        FileResultFilter.Updated,
        FileResultFilter.Unchanged
    ];

    private readonly IBatchReportStore _batchReportStore;
    private IReadOnlyList<FileResultItem> _files = Array.Empty<FileResultItem>();

    /// <summary>
    /// The batch and counts that <see cref="_files"/> was built for.
    /// </summary>
    private (BatchExecutionReport? Batch, BatchResultCounts Counts) _filesSource;

    /// <summary>
    /// Gets the batch whose results are shown.
    /// </summary>
    [ObservableProperty]
    public partial BatchExecutionReport CurrentBatch { get; private set; }

    /// <summary>
    /// Gets the number of files per result.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Outcome))]
    [NotifyPropertyChangedFor(nameof(HasBatch))]
    [NotifyPropertyChangedFor(nameof(HasNoBatch))]
    [NotifyPropertyChangedFor(nameof(HasUnchanged))]
    [NotifyPropertyChangedFor(nameof(HasCanceled))]
    public partial BatchResultCounts Counts { get; private set; }

    /// <summary>
    /// Gets when the first file started, or <see langword="null"/> when no file started.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTiming))]
    public partial DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// Gets the time from the start of the first file to the end of the last file, or <see langword="null"/> when no file finished.
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan? Elapsed { get; private set; }

    /// <summary>
    /// Gets the filters that show at least one file. The first option shows all files.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFilter))]
    public partial IReadOnlyList<FileResultFilterOption> FilterOptions { get; private set; } = Array.Empty<FileResultFilterOption>();

    /// <summary>
    /// Gets or sets the filter that selects which files are listed.
    /// </summary>
    [ObservableProperty]
    public partial FileResultFilterOption? SelectedFilter { get; set; }

    /// <summary>
    /// Gets the listed files: files that need attention first, then in processing order.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFiles))]
    public partial IReadOnlyList<FileResultItem> VisibleFiles { get; private set; } = Array.Empty<FileResultItem>();

    public BatchOutcome Outcome => Counts.Outcome;

    public bool HasBatch => Outcome != BatchOutcome.None;

    public bool HasNoBatch => Outcome == BatchOutcome.None;

    public bool HasUnchanged => Counts.Unchanged > 0;

    public bool HasCanceled => Counts.Canceled > 0;

    public bool HasTiming => StartedAt.HasValue;

    public bool HasFiles => VisibleFiles.Count > 0;

    /// <summary>
    /// Gets whether the filter is useful: it is hidden when every file has the same result.
    /// </summary>
    public bool ShowFilter => FilterOptions.Count > 2;

    public BatchResultsViewModel(IBatchReportStore batchReportStore)
    {
        _batchReportStore = batchReportStore;
        CurrentBatch = _batchReportStore.ActiveBatch;

        _batchReportStore.PropertyChanged += OnStorePropertyChanged;
        CurrentBatch.PropertyChanged += OnBatchReportPropertyChanged;
        Refresh();
    }

    private void OnStorePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName != nameof(IBatchReportStore.ActiveBatch))
            return;

        CurrentBatch.PropertyChanged -= OnBatchReportPropertyChanged;
        CurrentBatch = _batchReportStore.ActiveBatch;
        CurrentBatch.PropertyChanged += OnBatchReportPropertyChanged;
        Refresh();
    }

    private void OnBatchReportPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        // Progress of the running file does not change any result.
        if (eventArgs.PropertyName is nameof(BatchExecutionReport.OverallProgress) or nameof(BatchExecutionReport.CurrentFile))
            return;

        Refresh();
    }

    partial void OnSelectedFilterChanged(FileResultFilterOption? value) => ApplyFilter();

    /// <summary>
    /// Recounts the results. Builds the file list once the batch finished, keeping the selected filter when it still applies.
    /// </summary>
    /// <remarks>
    /// The processing overlay covers the page while the batch runs, so the list is not rebuilt for every file.
    /// The batch raises several changes when a file finishes; the list is only rebuilt when the batch or its counts changed.
    /// </remarks>
    private void Refresh()
    {
        var counts = BatchResultCounts.From(CurrentBatch.FileReports);
        Counts = counts;
        UpdateTiming();

        if (_filesSource == (CurrentBatch, counts))
            return;

        _filesSource = (CurrentBatch, counts);
        var finished = counts.Outcome is not (BatchOutcome.None or BatchOutcome.InProgress);

        _files = finished
            ? CurrentBatch.FileReports.Select(report => new FileResultItem(report)).OrderBy(item => item.SortRank).ToArray()
            : Array.Empty<FileResultItem>();

        var selected = SelectedFilter?.Filter ?? FileResultFilter.All;
        var options = new List<FileResultFilterOption>();
        if (finished)
        {
            options.Add(new(FileResultFilter.All, counts.Total));
            options.AddRange(FilterOrder.Where(f => counts.CountFor(f) > 0).Select(f => new FileResultFilterOption(f, counts.CountFor(f))));
        }

        FilterOptions = options;

        var nextSelection = FilterOptions.FirstOrDefault(option => option.Filter == selected) ?? FilterOptions.FirstOrDefault();
        if (nextSelection == SelectedFilter)
        {
            ApplyFilter();
        }
        else
        {
            SelectedFilter = nextSelection;
        }
    }

    private void ApplyFilter()
    {
        var filter = SelectedFilter?.Filter ?? FileResultFilter.All;
        VisibleFiles = filter == FileResultFilter.All ? _files : _files.Where(item => item.Matches(filter)).ToArray();
    }

    private void UpdateTiming()
    {
        DateTimeOffset? started = null;
        DateTimeOffset? finished = null;

        foreach (var report in CurrentBatch.FileReports)
        {
            if (report.StartedAt is { } start && (started is null || start < started))
                started = start;

            if (report.FinishedAt is { } end && (finished is null || end > finished))
                finished = end;
        }

        StartedAt = started;
        Elapsed = started is { } from && finished is { } to && to >= from ? to - from : null;
    }
}
