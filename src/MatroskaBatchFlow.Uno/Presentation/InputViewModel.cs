using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
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
    private readonly IFileScanner _fileScanner;
    private readonly IFileValidationEngine _fileValidator;
    private readonly IFileProcessingEngine _fileProcessingRuleEngine;
    private readonly IBatchConfiguration _batchConfig;
    private readonly IBatchTrackConfigurationInitializer _trackConfigInitializer;
    private readonly IFilePickerDialogService _filePickerDialogService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationSettingsService _validationSettingsService;
    private readonly IPlatformService _platformService;
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
        IFileScanner fileScanner,
        IFileValidationEngine fileValidator,
        IFileProcessingEngine fileProcessingRuleEngine,
        IBatchConfiguration batchConfig,
        IBatchTrackConfigurationInitializer trackConfigInitializer,
        IFilePickerDialogService filePickerDialogService,
        IWritableSettings<UserSettings> userSettings,
        IValidationSettingsService validationSettingsService,
        IPlatformService platformService,
        ILogger<InputViewModel> logger
        )
    {
        _fileListAdapter = fileListAdapter;
        _fileScanner = fileScanner;
        _fileValidator = fileValidator;
        _fileProcessingRuleEngine = fileProcessingRuleEngine;
        _batchConfig = batchConfig;
        _trackConfigInitializer = trackConfigInitializer;
        _filePickerDialogService = filePickerDialogService;
        _userSettings = userSettings;
        _validationSettingsService = validationSettingsService;
        _platformService = platformService;
        _logger = logger;

        RemoveSelected = new RelayCommand(RemoveSelectedFiles);
        RemoveAll = new RelayCommand(RemoveAllFiles);
        ClearSelection = new RelayCommand(ClearFileSelection);
        SelectAll = new RelayCommand(SelectAllFiles);
        AddFilesCommand = new AsyncRelayCommand(AddFilesAsync);
        ShowValidationDetailsCommand = new RelayCommand(ShowValidationDetails);

        _batchConfig.FileList.CollectionChanged += BatchConfigFileList_CollectionChanged;
        SelectedFiles.CollectionChanged += SelectedFiles_CollectionChanged;

        // Store the handler for later unsubscription.
        _validationNotificationsChangedHandler = (s, e) => NotifyValidationPropertiesChanged();
        ValidationNotifications.AllNotifications.CollectionChanged += _validationNotificationsChangedHandler;
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
        await ImportStorageFilesAsync(storageFiles);
    }

    /// <summary>
    /// Asynchronously imports a collection of <see cref="StorageFile"/> objects into the batch configuration.
    /// Scans, validates, synchronizes track counts, applies processing rules, and adds valid files to the file list.
    /// </summary>
    /// <param name="files">
    /// A read-only list of <see cref="StorageFile"/> objects to import. 
    /// Must not be <see langword="null"/>. If empty, the method returns immediately.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    private async Task ImportStorageFilesAsync(IReadOnlyList<StorageFile> files)
    {
        if (files is null or not { Count: > 0 })
            return;

        LogImportingFiles(files.Count);

        // Check for duplicates and filter to unique files only
        var uniqueFiles = FilterDuplicateFiles(files);

        if (uniqueFiles.Count == 0)
            return;

        // Scan the files to get their information.
        IEnumerable<ScannedFileInfo> newFiles = await _fileScanner.ScanAsync(uniqueFiles.ToFileInfo());
        var scannedFiles = newFiles.ToList();
        if (scannedFiles.Count == 0)
            return;

        LogFilesScanned(scannedFiles.Count);

        // Combine existing files with new files.
        List<ScannedFileInfo> combinedFiles = [.. _batchConfig.FileList, .. scannedFiles];
        if (combinedFiles.Count == 0)
            return;

        // Validate the combined list of files using current validation settings from user preferences.
        var currentValidationSettings = _validationSettingsService.GetEffectiveSettings(_userSettings.Value);
        List<FileValidationResult> validationResults = [.. _fileValidator.Validate(combinedFiles, currentValidationSettings)];
        if (HandleValidationResults(validationResults))
            return;

        // Initialize per-file track configurations for all new files
        foreach (var file in scannedFiles)
        {
            _trackConfigInitializer.Initialize(
                file,
                TrackType.Audio,
                TrackType.Video,
                TrackType.Text
            );
        }

        // Apply processing rules to the new files.
        foreach (var file in scannedFiles)
        {
            _fileProcessingRuleEngine.Apply(file, _batchConfig);
        }

        // Add files via the adapter to keep everything in sync.
        _fileListAdapter.AddFiles(scannedFiles);
    }


    /// <summary>
    /// Filters out duplicate files from the provided list and notifies the user if any duplicates are found.
    /// </summary>
    /// <param name="files">The list of files to check for duplicates.</param>
    /// <returns>A list containing only unique files that are not already in the batch configuration.</returns>
    private List<StorageFile> FilterDuplicateFiles(IReadOnlyList<StorageFile> files)
    {
        // Platform-aware comparison. Not perfect, but should be good enough for our purposes.
        var comparer = _platformService.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        // Build lookup of existing paths with appropriate comparer for O(1) lookups.
        // Paths in _batchConfig are assumed to be normalized already.
        var existingPaths = new HashSet<string>(
            _batchConfig.FileList.Select(f => f.Path),
            comparer);

        // Track seen paths in this batch to detect duplicates within the input
        var seenPaths = new HashSet<string>(comparer);
        var duplicates = new List<string>();
        var uniqueFiles = new List<StorageFile>();

        foreach (var file in files)
        {
            var normalizedPath = Path.GetFullPath(file.Path);

            // If the file is already in the existing batch configuration, treat as duplicate.
            if (existingPaths.Contains(normalizedPath))
            {
                duplicates.Add(normalizedPath);
                continue;
            }

            // Track duplicates within the current set of input files.
            var isNewPathInBatch = seenPaths.Add(normalizedPath);
            if (!isNewPathInBatch)
            {
                duplicates.Add(normalizedPath);
            }
            else
            {
                uniqueFiles.Add(file);
            }
        }

        // Show duplicate message if any were found
        if (duplicates.Count > 0)
        {
            LogDuplicatesSkipped(duplicates.Count);

            var duplicateFileNames = string.Join(Environment.NewLine, duplicates.Select(p => Path.GetFileName(p) ?? p));
            var message = duplicates.Count == 1
                ? $"This file is already in the list:{Environment.NewLine}{duplicateFileNames}"
                : $"These {duplicates.Count} files are already in the list:{Environment.NewLine}{duplicateFileNames}";

            WeakReferenceMessenger.Default.Send(new DialogMessage("Duplicate Files", message));
        }

        return uniqueFiles;
    }

    /// <summary>
    /// Removes all files currently selected in the <see cref="SelectedFiles"/> collection from the file list.
    /// </summary>
    /// <remarks>This method processes the removal of selected files by iterating over a copy of the <see
    /// cref="SelectedFiles"/> collection.</remarks>
    private void RemoveSelectedFiles()
    {
        // Convert SelectedFiles to an array to make a copy to avoid modifying the collection while iterating.
        foreach (ScannedFileViewModel file in SelectedFiles.ToArray())
        {
            _fileListAdapter.RemoveFile(file.FileInfo);
        }
    }

    /// <summary>
    /// Removes all files from the internal file list.
    /// </summary>
    /// <remarks>This method clears the file list by iterating over a copy of the current file collection
    /// to avoid issues with modifying the collection during enumeration.</remarks>
    private void RemoveAllFiles()
    {
        // Make array copy to prevent recursive clearing.
        foreach (ScannedFileViewModel file in _fileListAdapter.ScannedFileViewModels.ToArray())
        {
            _fileListAdapter.RemoveFile(file.FileInfo);
        }
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

        await ImportStorageFilesAsync(files);
    }

    /// <summary>
    /// Processes validation results and handles display of notifications.
    /// </summary>
    /// <remarks>
    /// This method processes all validation results (errors, warnings, info) and updates the validation notification state.
    /// Only errors (ValidationSeverity.Error) will block file addition.
    /// </remarks>
    /// <param name="results">A collection of <see cref="FileValidationResult"/> objects representing the validation results to process.</param>
    /// <returns><see langword="true"/> if one or more blocking errors were found; otherwise, <see langword="false"/>.</returns>
    private bool HandleValidationResults(IEnumerable<FileValidationResult> results)
    {
        // Clear previous notifications
        ValidationNotifications.Clear();

        // Convert all results to notification items
        var notifications = results.Select(r => new ValidationNotificationItem
        {
            Severity = r.Severity,
            FilePath = r.ValidatedFilePath,
            Message = r.Message
        });

        // Add all notifications to state
        ValidationNotifications.AddNotifications(notifications);

        // Open InfoBar if there are notifications
        IsValidationInfoBarOpen = ValidationNotifications.HasNotifications;

        // Only errors block file addition
        if (ValidationNotifications.HasErrors)
        {
            LogValidationBlocked(ValidationNotifications.Errors.Count());
            return true;
        }

        return false;
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
        ValidationNotifications.AllNotifications.CollectionChanged -= _validationNotificationsChangedHandler;

        GC.SuppressFinalize(this);
    }
}
