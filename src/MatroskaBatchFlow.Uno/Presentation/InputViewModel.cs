using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Behavior;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Contracts.ViewModels;
using MatroskaBatchFlow.Uno.Extensions;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class InputViewModel : ObservableObject, IFilesDropped, INavigationAware, IDisposable
{
    [ObservableProperty]
    private ObservableCollection<ScannedFileViewModel> selectedFiles = [];

    [ObservableProperty]
    private FileCheckSummary fileChecks = new(0, []);

    [ObservableProperty]
    private OperationOverlayState overlayState = OperationOverlayState.Inactive;

    private readonly IFileListAdapter _fileListAdapter;
    private readonly IBatchConfiguration _batchConfig;
    private readonly IFilePickerDialogService _filePickerDialogService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationStateService _validationStateService;
    private readonly IInputOperationFeedbackService _inputOperationFeedbackService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private readonly ILanguageProvider _languageProvider;
    private readonly INavigationService _navigationService;
    private readonly ILogger<InputViewModel> _logger;

    public bool CanSelectAll => _batchConfig.FileList.Count > SelectedFiles.Count;
    public ObservableCollection<ScannedFileViewModel> FileList => _fileListAdapter.ScannedFileViewModels;

    [RelayCommand]
    private void ReviewFileChecks(ScannedFileViewModel? file) =>
        _navigationService.NavigateTo(typeof(FileChecksViewModel).FullName!, new FileCheckVisit(file?.FileInfo.Path));

    public ICommand RemoveSelected { get; }
    public ICommand RemoveAll { get; }
    public ICommand ClearSelection { get; }
    public ICommand SelectAll { get; }
    public ICommand AddFilesCommand { get; }
    public InputViewModel(
        IFileListAdapter fileListAdapter,
        IBatchConfiguration batchConfig,
        IFilePickerDialogService filePickerDialogService,
        IWritableSettings<UserSettings> userSettings,
        IValidationStateService validationStateService,
        IInputOperationFeedbackService inputOperationFeedbackService,
        IBatchOperationOrchestrator orchestrator,
        ILanguageProvider languageProvider,
        INavigationService navigationService,
        ILogger<InputViewModel> logger
        )
    {
        _fileListAdapter = fileListAdapter;
        _batchConfig = batchConfig;
        _filePickerDialogService = filePickerDialogService;
        _userSettings = userSettings;
        _validationStateService = validationStateService;
        _inputOperationFeedbackService = inputOperationFeedbackService;
        _orchestrator = orchestrator;
        _languageProvider = languageProvider;
        _navigationService = navigationService;
        _logger = logger;

        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFilesAsync);
        RemoveAll = new AsyncRelayCommand(RemoveAllFilesAsync);
        ClearSelection = new RelayCommand(ClearFileSelection);
        SelectAll = new RelayCommand(SelectAllFiles);
        AddFilesCommand = new AsyncRelayCommand(AddFilesAsync);

        _batchConfig.FileList.CollectionChanged += BatchConfigFileList_CollectionChanged;
        SelectedFiles.CollectionChanged += SelectedFiles_CollectionChanged;
        _validationStateService.StateChanged += OnValidationStateChanged;
        _inputOperationFeedbackService.StateChanged += OnInputOperationFeedbackStateChanged;

        OverlayState = _inputOperationFeedbackService.CurrentState ?? OperationOverlayState.Inactive;
        OnValidationStateChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Handles the event when files are dropped onto the application.
    /// </summary>
    /// <remarks>This method filters the dropped items to include only <see cref="StorageFile"/> instances. If
    /// any non-file items (e.g., folders) are detected, a dialog message is sent to notify the user, and no further
    /// processing occurs. Valid files are imported asynchronously for further processing.</remarks>
    /// <param name="files">An array of <see cref="IStorageItem"/> objects representing the dropped items. Only files are processed; folders
    /// are ignored.</param>
    /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task OnFilesDroppedAsync(IStorageItem[] files)
    {
        if (files is null or { Length: 0 })
            return;

        // Filter out only StorageFile instances from the dropped items.
        List<StorageFile> storageFiles = [.. files.OfType<StorageFile>().Where(sf => sf is not null)];
        var containsNonFiles = files.Length != storageFiles.Count;

        if (containsNonFiles)
        {
            WeakReferenceMessenger.Default.Send(
                new DialogMessage("Invalid Files", "Only files can be added, not folders.")
            );

            return;
        }

        // Import the files for further processing and validation.
        await _orchestrator.ImportFilesAsync(storageFiles.ToFileInfo());
    }

    /// <summary>
    /// Removes all files currently selected in the <see cref="SelectedFiles"/> collection from the file list.
    /// </summary>
    /// <remarks>This method processes the removal of selected files by iterating over a copy of the <see
    /// cref="SelectedFiles"/> collection.</remarks>
    private async Task RemoveSelectedFilesAsync()
    {
        var filesToRemove = SelectedFiles
            .Select(file => file.FileInfo)
            .ToList();

        if (filesToRemove.Count == 0)
            return;

        await _orchestrator.RemoveFilesAsync(filesToRemove);
    }

    /// <summary>
    /// Removes all files from the internal file list.
    /// </summary>
    private async Task RemoveAllFilesAsync()
    {
        var filesToRemove = _fileListAdapter.ScannedFileViewModels
            .Select(file => file.FileInfo)
            .ToList();

        if (filesToRemove.Count == 0)
            return;

        await _orchestrator.RemoveFilesAsync(filesToRemove);
    }

    /// <summary>
    /// Clears the current selection of files.
    /// </summary>
    /// <remarks>This method removes all items from the <see cref="SelectedFiles"/> collection.</remarks>
    private void ClearFileSelection()
    {
        SelectedFiles.Clear();
    }

    /// <summary>
    /// Selects all files by adding them to the <see cref="SelectedFiles"/> collection.
    /// </summary>
    private void SelectAllFiles()
    {
        // Select all files by adding them to the SelectedFiles collection.
        foreach (var file in _fileListAdapter.ScannedFileViewModels)
        {
            SelectedFiles.Add(file);
        }
    }

    /// <summary>
    /// Opens a file picker dialog to allow the user to select files for import.
    /// </summary>
    /// <remarks>If no files are selected, the method exits without performing any operation.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task AddFilesAsync()
    {
        IReadOnlyList<StorageFile> files = await _filePickerDialogService.PickFilesAsync();

        if (files.Count == 0)
            return;

        await _orchestrator.ImportFilesAsync(files.ToFileInfo());
    }

    /// <summary>
    /// Updates each input's check count from the topics it participates in, not just the mismatches reported against its path.
    /// </summary>
    private void OnValidationStateChanged(object? sender, EventArgs e)
    {
        FileChecks = FileCheckSummary.Create(
            _validationStateService.CurrentResults, _batchConfig.FileList.Count, _languageProvider);
        var checksByFile = FileChecks.Groups
            .SelectMany(group => group.Files.Select(file => (file.FilePath, Group: group)))
            .ToLookup(item => item.FilePath, item => item.Group, StringComparer.Ordinal);

        foreach (var file in FileList)
        {
            var groups = checksByFile[file.FileInfo.Path].ToArray();
            file.CheckTopics = groups.Select(group => group.Topic).ToArray();
            file.IsUnsupported = groups.Any(group => group.IsFormatCheck);
        }
    }

    private void OnInputOperationFeedbackStateChanged(object? sender, EventArgs e)
    {
        OverlayState = _inputOperationFeedbackService.CurrentState ?? OperationOverlayState.Inactive;
    }

    /// <summary>
    /// Handles changes to the <see cref="IBatchConfiguration.FileList"/> collection.
    /// </summary>
    /// <param name="sender">The source of the event, typically the collection that triggered the change.</param>
    /// <param name="eventArgs">The event data containing details about the change to the collection.</param>
    private void BatchConfigFileList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        OnPropertyChanged(nameof(CanSelectAll));
    }

    /// <summary>
    /// Handles changes to the <see cref="SelectedFiles"/> collection.
    /// </summary>
    private void SelectedFiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        OnPropertyChanged(nameof(CanSelectAll));
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
        // Implementation for when the view model is navigated away from.
    }

    /// <summary>
    /// Disposes of the resources used by the <see cref="InputViewModel"/> instance.
    /// </summary>
    public void Dispose()
    {
        _batchConfig.FileList.CollectionChanged -= BatchConfigFileList_CollectionChanged;
        SelectedFiles.CollectionChanged -= SelectedFiles_CollectionChanged;
        _validationStateService.StateChanged -= OnValidationStateChanged;
        _inputOperationFeedbackService.StateChanged -= OnInputOperationFeedbackStateChanged;

        GC.SuppressFinalize(this);
    }
}
