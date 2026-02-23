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
    private ValidationNotificationState validationNotifications = new();

    [ObservableProperty]
    private InputOperationOverlayState overlayState = InputOperationOverlayState.Inactive;

    private bool _isValidationInfoBarOpen;

    /// <summary>
    /// Gets or sets whether the validation InfoBar is open.
    /// When set to false, clears all validation notifications.
    /// </summary>
    public bool IsValidationInfoBarOpen
    {
        get => _isValidationInfoBarOpen;
        set
        {
            if (SetProperty(ref _isValidationInfoBarOpen, value))
            {
                // When user closes the InfoBar, clear notifications
                if (!value && ValidationNotifications.HasNotifications)
                {
                    ValidationNotifications.Clear();
                    NotifyValidationPropertiesChanged();
                }
            }
        }
    }

    private readonly IFileListAdapter _fileListAdapter;
    private readonly IBatchConfiguration _batchConfig;
    private readonly IFilePickerDialogService _filePickerDialogService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationStateService _validationStateService;
    private readonly IInputOperationFeedbackService _inputOperationFeedbackService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private readonly ILogger<InputViewModel> _logger;

    public bool CanSelectAll => _batchConfig.FileList.Count > SelectedFiles.Count;
    public ObservableCollection<ScannedFileViewModel> FileList => _fileListAdapter.ScannedFileViewModels;

    // Validation UI properties
    public bool HasValidationNotifications => ValidationNotifications.HasNotifications;
    public InfoBarSeverity ValidationInfoBarSeverity => ValidationNotifications.HighestSeverity;
    public string ValidationSummaryTitle => ValidationNotifications.SummaryTitle;
    public string ValidationSummaryMessage => ValidationNotifications.SummaryMessage;
    public bool HasErrors => ValidationNotifications.HasErrors;
    public bool HasWarnings => ValidationNotifications.HasWarnings;
    public bool HasInfoMessages => ValidationNotifications.HasInfoMessages;
    public IEnumerable<ValidationNotificationItem> ValidationErrors => ValidationNotifications.Errors;
    public IEnumerable<ValidationNotificationItem> ValidationWarnings => ValidationNotifications.Warnings;
    public IEnumerable<ValidationNotificationItem> ValidationInfoMessages => ValidationNotifications.InfoMessages;

    /// <summary>
    /// Gets the visibility of the validation InfoBar.
    /// Returns Visible when there are notifications, Collapsed otherwise.
    /// </summary>
    public Visibility ValidationInfoBarVisibility =>
        HasValidationNotifications ? Visibility.Visible : Visibility.Collapsed;

    public ICommand RemoveSelected { get; }
    public ICommand RemoveAll { get; }
    public ICommand ClearSelection { get; }
    public ICommand SelectAll { get; }
    public ICommand AddFilesCommand { get; }
    public ICommand ShowValidationDetailsCommand { get; }


    private readonly NotifyCollectionChangedEventHandler _validationNotificationsChangedHandler;

    public InputViewModel(
        IFileListAdapter fileListAdapter,
        IBatchConfiguration batchConfig,
        IFilePickerDialogService filePickerDialogService,
        IWritableSettings<UserSettings> userSettings,
        IValidationStateService validationStateService,
        IInputOperationFeedbackService inputOperationFeedbackService,
        IBatchOperationOrchestrator orchestrator,
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
        _logger = logger;

        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFilesAsync);
        RemoveAll = new AsyncRelayCommand(RemoveAllFilesAsync);
        ClearSelection = new RelayCommand(ClearFileSelection);
        SelectAll = new RelayCommand(SelectAllFiles);
        AddFilesCommand = new AsyncRelayCommand(AddFilesAsync);
        ShowValidationDetailsCommand = new RelayCommand(ShowValidationDetails);

        _batchConfig.FileList.CollectionChanged += BatchConfigFileList_CollectionChanged;
        SelectedFiles.CollectionChanged += SelectedFiles_CollectionChanged;
        _validationStateService.StateChanged += OnValidationStateChanged;
        _inputOperationFeedbackService.StateChanged += OnInputOperationFeedbackStateChanged;

        // Store the handler for later unsubscription.
        _validationNotificationsChangedHandler = (s, e) => NotifyValidationPropertiesChanged();
        ValidationNotifications.AllNotifications.CollectionChanged += _validationNotificationsChangedHandler;

        OverlayState = _inputOperationFeedbackService.CurrentState ?? InputOperationOverlayState.Inactive;
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
    /// Handles validation state changes from the <see cref="IValidationStateService"/>.
    /// Updates the UI notification state with the latest validation results.
    /// </summary>
    private void OnValidationStateChanged(object? sender, EventArgs e)
    {
        ValidationNotifications.Clear();

        if (_validationStateService.HasResults)
        {
            var notifications = _validationStateService.CurrentResults.Select(r => new ValidationNotificationItem
            {
                Severity = r.Severity,
                FilePath = r.ValidatedFilePath,
                Message = r.Message
            });

            ValidationNotifications.AddNotifications(notifications);
        }

        IsValidationInfoBarOpen = _validationStateService.HasResults;
    }

    private void OnInputOperationFeedbackStateChanged(object? sender, EventArgs e)
    {
        OverlayState = _inputOperationFeedbackService.CurrentState ?? InputOperationOverlayState.Inactive;
    }

    /// <summary>
    /// Shows a modal dialog with detailed validation results.
    /// </summary>
    private void ShowValidationDetails()
    {
        WeakReferenceMessenger.Default.Send(new ShowValidationDetailsMessage());
    }

    /// <summary>
    /// Notifies all validation-related property changes for UI binding updates.
    /// </summary>
    private void NotifyValidationPropertiesChanged()
    {
        OnPropertyChanged(nameof(HasValidationNotifications));
        OnPropertyChanged(nameof(ValidationInfoBarSeverity));
        OnPropertyChanged(nameof(ValidationSummaryTitle));
        OnPropertyChanged(nameof(ValidationSummaryMessage));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasInfoMessages));
        OnPropertyChanged(nameof(ValidationErrors));
        OnPropertyChanged(nameof(ValidationWarnings));
        OnPropertyChanged(nameof(ValidationInfoMessages));
        OnPropertyChanged(nameof(ValidationInfoBarVisibility));
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
        // Implementation for when the view model is navigated to.
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
        ValidationNotifications.AllNotifications.CollectionChanged -= _validationNotificationsChangedHandler;

        GC.SuppressFinalize(this);
    }
}
