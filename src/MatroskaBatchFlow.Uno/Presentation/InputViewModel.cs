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

    public bool CanSelectAll => _batchConfig.FileList.Count > SelectedFiles.Count;
    public ObservableCollection<ScannedFileViewModel> FileList => _fileListAdapter.ScannedFileViewModels;

    public ICommand RemoveSelected { get; }
    public ICommand RemoveAll { get; }
    public ICommand ClearSelection { get; }
    public ICommand SelectAll { get; }

    public InputViewModel(
        IFileListAdapter fileListAdapter,
        IFileScanner fileScanner,
        IFileValidationEngine fileValidator,
        IFileProcessingEngine fileProcessingRuleEngine,
        IBatchConfiguration batchConfig,
        IBatchTrackCountSynchronizer batchConfigurationTrackInitializer
        )
    {
        _fileListAdapter = fileListAdapter;
        _fileScanner = fileScanner;
        _fileValidator = fileValidator;
        _fileProcessingRuleEngine = fileProcessingRuleEngine;
        _batchConfig = batchConfig;
        _BatchTrackCountSynchronizer = batchConfigurationTrackInitializer;
        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFiles);
        RemoveAll = new AsyncRelayCommand(RemoveAllFiles);
        ClearSelection = new AsyncRelayCommand(ClearFileSelection);
        SelectAll = new AsyncRelayCommand(SelectAllFiles);

        _batchConfig.FileList.CollectionChanged += BatchConfigFileList_CollectionChanged;
        SelectedFiles.CollectionChanged += SelectedFiles_CollectionChanged;
    }

    /// <summary>
    /// Handles the event when files are dropped, processing and validating the provided files.
    /// </summary>
    /// <remarks>This method scans the dropped files, validates them, and adds them to the file list if they
    /// pass validation. If validation errors are encountered, the method handles them and stops further
    /// processing.</remarks>
    /// <param name="files">An array of <see cref="IStorageItem"/> representing the files that were dropped.</param>
    public async Task OnFilesDropped(IStorageItem[] files)
    {
        if (files == null || files.Length == 0)
            return;

        var newFiles = await _fileScanner.ScanAsync(files.ToFileInfo());
        var combinedFiles = _batchConfig.FileList.Concat(newFiles).ToList();

        // Validate the combined list of files, including both existing and newly added files.
        var validationResults = _fileValidator.Validate(combinedFiles).ToList();
        if (HandleValidationErrors(validationResults))
            return;

        _BatchTrackCountSynchronizer.SynchronizeTrackCount(newFiles.First(), TrackType.Audio, TrackType.Video, TrackType.Text);
        // If validation passed, apply processing rules to the new files.
        foreach (var file in newFiles)
        {
            _fileProcessingRuleEngine.Apply(file, _batchConfig);
        }

        // Add files via the adapter to keep everything in sync
        _fileListAdapter.AddFiles(newFiles);
    }

    /// <summary>
    /// Removes all files currently selected in the <see cref="SelectedFiles"/> collection from the file list.
    /// </summary>
    /// <remarks>This method processes the removal of selected files by iterating over a copy of the <see
    /// cref="SelectedFiles"/> collection.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the operation.</returns>
    private Task RemoveSelectedFiles()
    {
        // Convert SelectedFiles to an array to make a copy to avoid modifying the collection while iterating.
        foreach (ScannedFileViewModel file in SelectedFiles.ToArray())
            _fileListAdapter.RemoveFile(file.File);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all files from the internal file list.
    /// </summary>
    /// <remarks>This method clears the file list by iterating over a copy of the current file collection
    /// to avoid issues with modifying the collection during enumeration.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the operation.</returns>
    private Task RemoveAllFiles()
    {
        // Make array copy to prevent recursive clearing.
        foreach (ScannedFileViewModel file in _fileListAdapter.ScannedFileViewModels.ToArray())
            _fileListAdapter.RemoveFile(file.File);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears the current selection of files.
    /// </summary>
    /// <remarks>This method removes all items from the <see cref="SelectedFiles"/> collection.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the operation.</returns>
    private Task ClearFileSelection()
    {
        SelectedFiles.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Selects all files by adding them to the <see cref="SelectedFiles"/> collection.
    /// </summary>
    /// <returns>A completed <see cref="Task"/> representing the operation.</returns>
    private Task SelectAllFiles()
    {
        // Select all files by adding them to the SelectedFiles collection.
        foreach (var file in _fileListAdapter.ScannedFileViewModels)
        {
            SelectedFiles.Add(file);
        }
        return Task.CompletedTask;
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
