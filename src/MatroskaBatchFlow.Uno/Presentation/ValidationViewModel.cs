using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Contracts.ViewModels;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Presents one check at a time, with its scanned evidence and relevant navigation.
/// </summary>
public sealed partial class ValidationViewModel : ObservableObject, INavigationAware, IDisposable
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IValidationStateService _validationState;
    private readonly ILanguageProvider _languageProvider;
    private readonly INavigationService _navigationService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private string? _focusedPath;
    private FileCheckVisit _visit = new();

    /// <summary>
    /// The checks for the whole batch, built from the latest validation results.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoFindings))]
    public partial FileCheckSummary FileChecks { get; set; } = new(0, []);

    /// <summary>
    /// The check whose details are shown, or null when there are no checks.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCheck))]
    [NotifyPropertyChangedFor(nameof(CanReviewTrackSettings))]
    [NotifyPropertyChangedFor(nameof(ShowValidationSettings))]
    [NotifyPropertyChangedFor(nameof(ShowDiagnostics))]
    [NotifyPropertyChangedFor(nameof(VisibleFiles))]
    [NotifyPropertyChangedFor(nameof(FocusedFile))]
    [NotifyPropertyChangedFor(nameof(CanRemoveUnsupported))]
    [NotifyPropertyChangedFor(nameof(RemovalCount))]
    [NotifyCanExecuteChangedFor(nameof(ReviewTrackSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveUnsupportedCommand))]
    public partial FileCheckGroup? SelectedCheck { get; set; }

    /// <summary>
    /// The checks in the list: all checks, or only those involving the filtered file.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckSections))]
    public partial IReadOnlyList<FileCheckGroup> VisibleGroups { get; set; } = [];

    /// <summary>
    /// Gets the visible checks grouped for the list: Blocking before Advisory, then one section per family, ordered by track position.
    /// </summary>
    public IReadOnlyList<FileCheckSection> CheckSections => VisibleGroups
        .GroupBy(check => check.IsBlocking)
        .SelectMany(category => category.GroupBy(check => check.FamilyKey)
            .Select((family, index) => new FileCheckSection(
                index == 0 ? category.Count() : 0, family.OrderBy(check => check.TrackPosition).ToArray())))
        .ToArray();

    /// <summary>
    /// Gets or sets the number of unsupported files that were just removed from the batch, or zero when there is nothing to confirm.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRemovalNotice))]
    public partial int RemovedFileCount { get; set; }

    /// <summary>
    /// Gets whether the page confirms that unsupported files were removed.
    /// </summary>
    public bool HasRemovalNotice => RemovedFileCount > 0;

    /// <summary>
    /// The scanned values of the selected check, one row per value, with their files.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<FileCheckValue> comparisonRows = [];

    /// <summary>
    /// Gets whether the page is filtered to one file.
    /// </summary>
    public bool IsFileScoped => _focusedPath is not null;

    /// <summary>
    /// Gets the name of the file the page is filtered to, or null when it shows the whole batch.
    /// </summary>
    public string? FocusedFileName => Path.GetFileName(_focusedPath);

    /// <summary>
    /// Gets the filtered file's entry in the selected check, or null when the page is not filtered.
    /// </summary>
    public FileCheckFile? FocusedFile => IsFileScoped
        ? SelectedCheck?.Files.FirstOrDefault(file => file.FilePath == _focusedPath) : null;

    /// <summary>
    /// Gets whether the selected check lists a reason per file instead of comparing scanned values.
    /// </summary>
    public bool ShowDiagnostics => SelectedCheck is { HasComparison: false };

    /// <summary>
    /// Files in scope for diagnostics and removal. Comparison rows always describe the batch, regardless of this filter.
    /// </summary>
    public IReadOnlyList<FileCheckFile> VisibleFiles => IsFileScoped
        ? FocusedFile is { } file ? [file] : []
        : SelectedCheck?.Files ?? [];

    /// <summary>
    /// Gets whether the selected check is the unsupported file check, whose files can be removed from the batch.
    /// </summary>
    public bool CanRemoveUnsupported => SelectedCheck?.IsFormatCheck == true;

    /// <summary>
    /// Gets the number of files the remove button would remove, or zero when it is not available.
    /// </summary>
    public int RemovalCount => CanRemoveUnsupported ? VisibleFiles.Count : 0;

    partial void OnSelectedCheckChanged(FileCheckGroup? value) => ComparisonRows = value is null ? []
        : FileChecks.GetComparisonRows(value, _batchConfiguration.FileList.Select(file => file.Path));

    /// <summary>
    /// Gets whether a check is selected.
    /// </summary>
    public bool HasSelectedCheck => SelectedCheck is not null;

    /// <summary>
    /// Gets whether no check has findings, so the page shows its empty state.
    /// </summary>
    public bool HasNoFindings => !FileChecks.HasFindings;

    /// <summary>
    /// Gets whether the selected check compares audio, video or subtitle tracks, so its track settings can be opened.
    /// </summary>
    public bool CanReviewTrackSettings => SelectedCheck?.TrackType is TrackType.Audio or TrackType.Video or TrackType.Text;

    /// <summary>
    /// Gets whether the page offers to open the validation settings, which it does for blocking track checks.
    /// </summary>
    public bool ShowValidationSettings => CanReviewTrackSettings && SelectedCheck!.IsBlocking;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationViewModel"/> class.
    /// </summary>
    /// <param name="batchConfiguration">The batch whose files are checked.</param>
    /// <param name="validationState">Provides the latest validation results and reports when they change.</param>
    /// <param name="languageProvider">Resolves scanned language tags to display names.</param>
    /// <param name="navigationService">Opens the track and settings pages.</param>
    /// <param name="orchestrator">Removes unsupported files from the batch and validates it again.</param>
    public ValidationViewModel(
        IBatchConfiguration batchConfiguration,
        IValidationStateService validationState,
        ILanguageProvider languageProvider,
        INavigationService navigationService,
        IBatchOperationOrchestrator orchestrator)
    {
        _batchConfiguration = batchConfiguration;
        _validationState = validationState;
        _languageProvider = languageProvider;
        _navigationService = navigationService;
        _orchestrator = orchestrator;
        _validationState.StateChanged += OnValidationStateChanged;
        OnValidationStateChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Rebuilds the checks from the latest validation results and restores the selection against them.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void OnValidationStateChanged(object? sender, EventArgs e)
    {
        SaveVisit();
        RemovedFileCount = 0;
        FileChecks = FileCheckSummary.Create(
            _validationState.CurrentResults, _batchConfiguration.FileList.Count, _languageProvider);
        RestoreVisit();
    }

    /// <summary>
    /// Saves the file filter, selected check and expanded values, so they can be restored after the checks change.
    /// </summary>
    private void SaveVisit()
    {
        _visit.FilePath = _focusedPath;
        _visit.Check = SelectedCheck?.Topic;
        _visit.ExpandedValues = ComparisonRows.Where(row => row.IsExpanded)
            .Select(row => row.RawValue).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Resolves the saved file, topic and expanded values against the latest validation results.
    /// </summary>
    /// <remarks>
    /// If the file no longer participates in a check, return to batch scope. If the topic disappeared,
    /// select the first remaining topic without transferring expansion state from a different check.
    /// </remarks>
    private void RestoreVisit()
    {
        var path = _visit.FilePath;
        var scoped = path is null ? FileChecks.Groups : FileChecks.Groups
            .Where(group => group.Files.Any(file => file.FilePath == path)).ToArray();
        _focusedPath = scoped.Count == 0 ? null : path;
        VisibleGroups = IsFileScoped ? scoped : FileChecks.Groups;
        var match = VisibleGroups.FirstOrDefault(group => group.Topic == _visit.Check);
        SelectedCheck = match ?? VisibleGroups.FirstOrDefault();
        foreach (var row in ComparisonRows)
            row.IsExpanded = match is not null && _visit.ExpandedValues.Contains(row.RawValue);
        OnPropertyChanged(nameof(IsFileScoped));
        OnPropertyChanged(nameof(FocusedFileName));
        OnPropertyChanged(nameof(FocusedFile));
        OnPropertyChanged(nameof(VisibleFiles));
        OnPropertyChanged(nameof(RemovalCount));
    }

    /// <summary>
    /// Clears the file filter and shows all checks, keeping the selected check and expanded values.
    /// </summary>
    [RelayCommand]
    private void ShowAllChecks()
    {
        SaveVisit();
        _visit.FilePath = null;
        RestoreVisit();
    }

    /// <summary>
    /// Removes only the unsupported inputs in the current scope from the batch, not from disk.
    /// </summary>
    /// <returns>A task that completes when the files have been removed and the batch has been validated again.</returns>
    [RelayCommand(CanExecute = nameof(CanRemoveUnsupported))]
    private async Task RemoveUnsupportedAsync()
    {
        if (!CanRemoveUnsupported)
            return;

        var paths = VisibleFiles.Select(file => file.FilePath).ToHashSet(StringComparer.Ordinal);
        var files = _batchConfiguration.FileList.Where(file => paths.Contains(file.Path)).ToArray();
        await _orchestrator.RemoveFilesAsync(files);
        int removed = files.Length - _batchConfiguration.FileList.Count(file => paths.Contains(file.Path));
        if (removed > 0)
        {
            RemovedFileCount = removed;
        }
    }

    /// <summary>
    /// Opens the Batch Validation section of the Settings page.
    /// </summary>
    [RelayCommand]
    private void OpenValidationSettings() =>
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!, SettingsViewModel.ValidationSection);

    /// <summary>
    /// Applies the scope chosen by the caller, or keeps the current selection when opened from the navigation bar.
    /// </summary>
    /// <param name="parameter">
    /// A <see cref="FileCheckVisit"/> from an entry point that chooses a scope, such as a file's check count on the Input page;
    /// otherwise, <see langword="null"/>.
    /// </param>
    public void OnNavigatedTo(object parameter)
    {
        SaveVisit();
        if (parameter is FileCheckVisit visit)
        {
            _visit = visit;
        }
        RemovedFileCount = 0;
        RestoreVisit();
    }

    /// <summary>
    /// Called when the page is left. The selection is kept for the next visit, so there is nothing to do.
    /// </summary>
    public void OnNavigatedFrom()
    {
    }

    /// <summary>
    /// Opens the batch editor for the selected check's track type, with the check's track position selected.
    /// </summary>
    /// <exception cref="InvalidOperationException">The selected check has no audio, video or subtitle track type.</exception>
    [RelayCommand(CanExecute = nameof(CanReviewTrackSettings))]
    private void ReviewTrackSettings()
    {
        string pageKey = SelectedCheck?.TrackType switch
        {
            TrackType.Audio => typeof(AudioViewModel).FullName!,
            TrackType.Video => typeof(VideoViewModel).FullName!,
            TrackType.Text => typeof(SubtitleViewModel).FullName!,
            _ => throw new InvalidOperationException("The selected check has no track settings page.")
        };
        _navigationService.NavigateTo(pageKey, new TrackCheckInspection(SelectedCheck!.TrackPosition));
    }

    /// <summary>
    /// Disposes of the resources used by the <see cref="ValidationViewModel"/> instance.
    /// </summary>
    public void Dispose() => _validationState.StateChanged -= OnValidationStateChanged;
}
