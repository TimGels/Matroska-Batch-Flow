using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Behavior;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Contracts.ViewModels;
using MatroskaBatchFlow.Uno.Extensions;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class InputViewModel : ObservableObject, IFilesDropped, INavigationAware, IDisposable
{
    [ObservableProperty]
    private ObservableCollection<ScannedFileViewModel> selectedFiles = [];

    private readonly IFileListAdapter _fileListAdapter;
    private readonly IFileScanner _fileScanner;
    private readonly IFileValidationEngine _fileValidator;
    private readonly IFileProcessingEngine _fileProcessingRuleEngine;
    private readonly IBatchConfiguration _batchConfig;
    private readonly IBatchTrackCountSynchronizer _BatchTrackCountSynchronizer;
    private readonly IFilePickerDialogService _filePickerDialogService;

    public bool CanSelectAll => _batchConfig.FileList.Count > SelectedFiles.Count;
    public ObservableCollection<ScannedFileViewModel> FileList => _fileListAdapter.ScannedFileViewModels;

    public ICommand RemoveSelected { get; }
    public ICommand RemoveAll { get; }
    public ICommand ClearSelection { get; }
    public ICommand SelectAll { get; }
    public ICommand AddFilesCommand { get; }

    public InputViewModel(
        IFileListAdapter fileListAdapter,
        IFileScanner fileScanner,
        IFileValidationEngine fileValidator,
        IFileProcessingEngine fileProcessingRuleEngine,
        IBatchConfiguration batchConfig,
        IBatchTrackCountSynchronizer batchConfigurationTrackInitializer,
        IFilePickerDialogService filePickerDialogService
        )
    {
        _fileListAdapter = fileListAdapter;
        _fileScanner = fileScanner;
        _fileValidator = fileValidator;
        _fileProcessingRuleEngine = fileProcessingRuleEngine;
        _batchConfig = batchConfig;
        _BatchTrackCountSynchronizer = batchConfigurationTrackInitializer;
        _filePickerDialogService = filePickerDialogService;
        RemoveSelected = new RelayCommand(RemoveSelectedFiles);
        RemoveAll = new RelayCommand(RemoveAllFiles);
        ClearSelection = new RelayCommand(ClearFileSelection);
        SelectAll = new RelayCommand(SelectAllFiles);
        AddFilesCommand = new AsyncRelayCommand(AddFilesAsync);

        _batchConfig.FileList.CollectionChanged += BatchConfigFileList_CollectionChanged;
        SelectedFiles.CollectionChanged += SelectedFiles_CollectionChanged;
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

        // Scan the files to get their information.
        IEnumerable<ScannedFileInfo> newFiles = await _fileScanner.ScanAsync(files.ToFileInfo());
        if (!newFiles.Any())
            return;

        // Combine existing files with new files.
        List<ScannedFileInfo> combinedFiles = [.. _batchConfig.FileList, .. newFiles];
        if (combinedFiles.Count == 0)
            return;

        // Validate the combined list of files.
        List<FileValidationResult> validationResults = [.. _fileValidator.Validate(combinedFiles)];
        if (HandleValidationErrors(validationResults))
            return;

        // Synchronize track counts for the first file in the new files.
        _BatchTrackCountSynchronizer.SynchronizeTrackCount(
            newFiles.First(),
            TrackType.Audio,
            TrackType.Video,
            TrackType.Text
        );

        // Apply processing rules to the new files.
        foreach (var file in newFiles)
        {
            _fileProcessingRuleEngine.Apply(file, _batchConfig);
        }

        // Add files via the adapter to keep everything in sync.
        _fileListAdapter.AddFiles(newFiles);
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
            _fileListAdapter.RemoveFile(file.File);
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
            _fileListAdapter.RemoveFile(file.File);
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
    /// Processes validation results and handles any errors by displaying a dialog message.
    /// </summary>
    /// <remarks>This method filters the provided validation results for errors and, if any are found, sends a
    /// dialog message containing the error details using the <see cref="WeakReferenceMessenger"/>. If no errors are
    /// present, the method returns <see langword="false"/> without sending a message.</remarks>
    /// <param name="results">A collection of <see cref="FileValidationResult"/> objects representing the validation results to process.</param>
    /// <returns><see langword="true"/> if one or more errors were found in the validation results; otherwise, <see
    /// langword="false"/>.</returns>
    private static bool HandleValidationErrors(IEnumerable<FileValidationResult> results)
    {
        var errors = results.Where(r => r.Severity == FileValidationSeverity.Error).ToList();
        if (errors.Count == 0)
            return false;

        // If there are errors, send a dialog message with the error details.
        var errorMessages = errors.Select(e => e.Message);
        WeakReferenceMessenger.Default.Send(
            new DialogMessage(
                "Validation Errors",
                string.Join(Environment.NewLine, errorMessages)
            )
        );

        return true;
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

        GC.SuppressFinalize(this);
    }
}
