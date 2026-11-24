using System.Collections.Specialized;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Contracts.ViewModels;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// ViewModel for displaying the results of the last batch and of earlier batches in the history.
/// </summary>
public partial class BatchResultsViewModel : ObservableObject, INavigationAware
{
    private static readonly FileResultFilter[] FilterOrder =
    [
        FileResultFilter.Failed,
        FileResultFilter.Interrupted,
        FileResultFilter.Warnings,
        FileResultFilter.Canceled,
        FileResultFilter.Updated,
        FileResultFilter.Unchanged
    ];

    private readonly IBatchHistoryService _history;
    /// <summary>
    /// Gets the saved runs, newest first.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRuns))]
    public partial IReadOnlyList<BatchRunSummary> Runs { get; private set; } = Array.Empty<BatchRunSummary>();

    /// <summary>
    /// Gets or sets the run whose results are shown.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Counts))]
    [NotifyPropertyChangedFor(nameof(Outcome))]
    [NotifyPropertyChangedFor(nameof(HasBatch))]
    [NotifyPropertyChangedFor(nameof(HasNoBatch))]
    [NotifyPropertyChangedFor(nameof(StartedAt))]
    [NotifyPropertyChangedFor(nameof(Elapsed))]
    [NotifyPropertyChangedFor(nameof(HasTiming))]
    public partial BatchRunSummary? SelectedRun { get; set; }

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

    [ObservableProperty]
    public partial bool ClearHistoryFailed { get; private set; }

    /// <summary>
    /// Gets the number of files per result in the selected run.
    /// </summary>
    public BatchResultCounts Counts => SelectedRun?.Counts ?? default;

    /// <summary>
    /// Gets when the first file of the selected run started, or <see langword="null"/> when no file started.
    /// </summary>
    public DateTimeOffset? StartedAt => SelectedRun?.StartedAt;

    /// <summary>
    /// Gets how long the selected run took, or <see langword="null"/> when no file finished.
    /// </summary>
    public TimeSpan? Elapsed => SelectedRun?.Elapsed;

    public BatchOutcome Outcome => Counts.Outcome;

    public bool HasRuns => Runs.Count > 0;

    public bool HasBatch => Outcome != BatchOutcome.None;

    public bool HasNoBatch => Outcome == BatchOutcome.None;

    public bool HasTiming => StartedAt.HasValue;

    public bool HasFiles => VisibleFiles.Count > 0;

    /// <summary>
    /// Gets whether the filter is useful: it is hidden when every file has the same result.
    /// </summary>
    public bool ShowFilter => FilterOptions.Count > 2;

    public BatchResultsViewModel(IBatchHistoryService history)
    {
        _history = history;
        ((INotifyCollectionChanged)_history.Runs).CollectionChanged += OnHistoryChanged;
        RefreshRuns(selectNewest: true);
    }

    /// <summary>
    /// Loads the saved runs again if loading them failed before, for example while the file was locked.
    /// </summary>
    /// <param name="parameter">The navigation parameter. Not used.</param>
    public void OnNavigatedTo(object parameter) => _ = _history.LoadAsync();

    public void OnNavigatedFrom()
    {
    }

    private void OnHistoryChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        var newRunAdded = eventArgs is { Action: NotifyCollectionChangedAction.Add, NewStartingIndex: 0 };
        RefreshRuns(selectNewest: newRunAdded);
    }

    /// <summary>
    /// Rebuilds the run picker. Shows the newest run when it was just added; otherwise keeps the shown run while it is still saved.
    /// </summary>
    private void RefreshRuns(bool selectNewest)
    {
        var existing = Runs.ToDictionary<BatchRunSummary, BatchRunRecord>(summary => summary.Run, ReferenceEqualityComparer.Instance);
        var runs = _history.Runs
            .Select(run => existing.TryGetValue(run, out var summary) ? summary : new BatchRunSummary(run))
            .ToArray();

        var previous = SelectedRun;
        var next = selectNewest || previous is null
            ? runs.FirstOrDefault()
            : runs.FirstOrDefault(summary => ReferenceEquals(summary, previous)) ?? runs.FirstOrDefault();

        // A ComboBox clears its selection through the two-way binding when its items change, so select afterwards.
        Runs = runs;
        SelectedRun = next;
    }

    partial void OnSelectedRunChanged(BatchRunSummary? value)
    {
        var counts = Counts;
        var options = new List<FileResultFilterOption>();
        if (counts.Total > 0)
        {
            options.Add(new(FileResultFilter.All, counts.Total));
            options.AddRange(FilterOrder.Where(f => counts.CountFor(f) > 0).Select(f => new FileResultFilterOption(f, counts.CountFor(f))));
        }

        var selected = SelectedFilter?.Filter ?? FileResultFilter.All;
        FilterOptions = options;

        var nextSelection = options.FirstOrDefault(option => option.Filter == selected) ?? options.FirstOrDefault();
        if (nextSelection == SelectedFilter)
        {
            ApplyFilter();
        }
        else
        {
            SelectedFilter = nextSelection;
        }
    }

    partial void OnSelectedFilterChanged(FileResultFilterOption? value) => ApplyFilter();

    /// <summary>
    /// Removes all saved runs from this device.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        ClearHistoryFailed = false;
        ClearHistoryFailed = !await _history.ClearAsync();
    }

    private void ApplyFilter()
    {
        var filter = SelectedFilter?.Filter ?? FileResultFilter.All;
        var files = SelectedRun?.Files ?? Array.Empty<FileResultItem>();
        VisibleFiles = filter == FileResultFilter.All ? files : files.Where(item => item.Matches(filter)).ToArray();
    }
}
