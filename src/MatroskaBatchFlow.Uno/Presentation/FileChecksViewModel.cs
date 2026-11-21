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
public sealed partial class FileChecksViewModel : ObservableObject, INavigationAware, IDisposable
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IValidationStateService _validationState;
    private readonly ILanguageProvider _languageProvider;
    private readonly INavigationService _navigationService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private string? _focusedPath;
    private FileCheckVisit _visit = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoFindings))]
    private FileCheckSummary fileChecks = new(0, []);

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
    private FileCheckGroup? selectedCheck;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckSections))]
    private IReadOnlyList<FileCheckGroup> visibleGroups = [];

    public IReadOnlyList<FileCheckSection> CheckSections => VisibleGroups
        .GroupBy(check => check.IsBlocking)
        .SelectMany(category => category.GroupBy(check => check.FamilyKey)
            .Select((family, index) => new FileCheckSection(
                index == 0 ? category.Count() : 0, family.OrderBy(check => check.TrackPosition).ToArray())))
        .ToArray();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRemovalNotice))]
    public partial int RemovedFileCount { get; set; }

    public bool HasRemovalNotice => RemovedFileCount > 0;

    [ObservableProperty]
    private IReadOnlyList<FileCheckValue> comparisonRows = [];

    public bool IsFileScoped => _focusedPath is not null;
    public string? FocusedFileName => Path.GetFileName(_focusedPath);
    public FileCheckFile? FocusedFile => IsFileScoped
        ? SelectedCheck?.Files.FirstOrDefault(file => file.FilePath == _focusedPath) : null;
    public bool ShowDiagnostics => SelectedCheck is { HasComparison: false };
    /// <summary>Files in scope for diagnostics and removal. Comparison rows always describe the batch, regardless of this filter.</summary>
    public IReadOnlyList<FileCheckFile> VisibleFiles => IsFileScoped
        ? FocusedFile is { } file ? [file] : []
        : SelectedCheck?.Files ?? [];
    public bool CanRemoveUnsupported => SelectedCheck?.IsFormatCheck == true;
    public int RemovalCount => CanRemoveUnsupported ? VisibleFiles.Count : 0;

    partial void OnSelectedCheckChanged(FileCheckGroup? value) => ComparisonRows = value is null ? []
        : FileChecks.GetComparisonRows(value, _batchConfiguration.FileList.Select(file => file.Path));

    public bool HasSelectedCheck => SelectedCheck is not null;
    public bool HasNoFindings => !FileChecks.HasFindings;
    public bool CanReviewTrackSettings => SelectedCheck?.TrackType is TrackType.Audio or TrackType.Video or TrackType.Text;
    public bool ShowValidationSettings => CanReviewTrackSettings && SelectedCheck!.IsBlocking;
    public FileChecksViewModel(
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

    private void OnValidationStateChanged(object? sender, EventArgs e)
    {
        SaveVisit();
        RemovedFileCount = 0;
        FileChecks = FileCheckSummary.Create(
            _validationState.CurrentResults, _batchConfiguration.FileList.Count, _languageProvider);
        RestoreVisit();
    }

    private void SaveVisit()
    {
        _visit.FilePath = _focusedPath;
        _visit.Check = SelectedCheck?.Topic;
        _visit.ExpandedValues = ComparisonRows.Where(row => row.IsExpanded)
            .Select(row => row.RawValue).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Resolves the saved file, topic and expanded values against the latest validation results.</summary>
    /// <remarks>If the file no longer participates in a check, return to batch scope. If the topic disappeared,
    /// select the first remaining topic without transferring expansion state from a different check.</remarks>
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

    [RelayCommand]
    private void ShowAllChecks()
    {
        SaveVisit();
        _visit.FilePath = null;
        RestoreVisit();
    }

    /// <summary>Removes only the unsupported inputs in the current scope from the batch, not from disk.</summary>
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

    [RelayCommand]
    private void OpenValidationSettings() =>
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!, SettingsViewModel.ValidationSection);

    public void OnNavigatedTo(object parameter)
    {
        SaveVisit();
        _visit = parameter as FileCheckVisit ?? new FileCheckVisit(parameter as string);
        RemovedFileCount = 0;
        RestoreVisit();
    }

    public void OnNavigatedFrom()
    {
    }

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
        _navigationService.NavigateTo(pageKey, new TrackCheckInspection(SelectedCheck!.TrackPosition, IsFileScoped));
    }

    public void Dispose() => _validationState.StateChanged -= OnValidationStateChanged;
}
