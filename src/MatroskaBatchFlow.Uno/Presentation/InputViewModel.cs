using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Behavior;
using MatroskaBatchFlow.Uno.Extensions;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Uno.Extensions.Specialized;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class InputViewModel : ObservableObject, IFilesDropped
{
    [ObservableProperty]
    private ObservableCollection<ScannedFileInfo> selectedFiles = [];

    private IFileScanner _fileScanner;

    private readonly IFileValidationEngine _fileValidator;

    private readonly IFileProcessingEngine _fileProcessingRuleEngine;

    private readonly IBatchConfiguration _batchConfig;

    private readonly IBatchConfigurationTrackInitializer _batchConfigurationTrackInitializer;

    public ICommand RemoveSelected { get; }

    public ObservableCollection<ScannedFileInfo> FileList => _batchConfig.FileList;

    public InputViewModel(
        IFileScanner fileScanner,
        IFileValidationEngine fileValidator,
        IFileProcessingEngine fileProcessingRuleEngine,
        IBatchConfiguration batchConfig,
        IBatchConfigurationTrackInitializer batchConfigurationTrackInitializer
        )
    {
        _fileScanner = fileScanner;
        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFiles);
        _fileValidator = fileValidator;
        _fileProcessingRuleEngine = fileProcessingRuleEngine;
        _batchConfig = batchConfig;
        _batchConfigurationTrackInitializer = batchConfigurationTrackInitializer;
    }

    /// <summary>
    /// Removes all files currently selected in the <see cref="SelectedFiles"/> collection from the <see
    /// cref="FileList"/>.
    /// </summary>
    /// <remarks>This method processes the removal of selected files by iterating over a copy of the <see
    /// cref="SelectedFiles"/> collection.</remarks>
    /// <returns>A completed <see cref="Task"/> representing the operation.</returns>
    private Task RemoveSelectedFiles()
    {
        // Convert SelectedFiles to an array to make a copy to avoid modifying the collection while iterating.
        foreach (ScannedFileInfo file in SelectedFiles.ToArray())
            _batchConfig.FileList.Remove(file);

        return Task.CompletedTask;
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

        _batchConfigurationTrackInitializer.EnsureTrackCount(newFiles.First(), TrackType.Audio, TrackType.Video, TrackType.Text);
        // If validation passed, apply processing rules to the new files.
        foreach (var file in newFiles)
        {
            _fileProcessingRuleEngine.Apply(file, _batchConfig);
        }

        _batchConfig.FileList.AddRange(newFiles);
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
}
