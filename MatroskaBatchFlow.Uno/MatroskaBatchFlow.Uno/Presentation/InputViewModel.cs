using System.Collections.ObjectModel;
using System.Diagnostics;
using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Behavior;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.UI.Xaml.Data;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;

namespace MatroskaBatchFlow.Uno.Presentation;
[Bindable]
public partial class InputViewModel : ObservableObject, IFilesDropped
{
    [ObservableProperty]
    private ObservableCollection<ScannedFileInfo> fileList = [];

    [ObservableProperty]
    private ObservableCollection<ScannedFileInfo> selectedFiles = [];

    private IFileScanner _fileScanner;

    private readonly IFileValidator _fileValidator;

    public ICommand RemoveSelected { get; }

    public InputViewModel(IStringLocalizer localizer, IOptions<AppConfig> appInfo, IFileScanner fileScanner, IFileValidator fileValidator)
    {
        _fileScanner = fileScanner;
        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFiles);
        _fileValidator = fileValidator;
    }

    private Task RemoveSelectedFiles()
    {
        // Convert SelectedFiles to an array to make a copy to avoid modifying the collection while iterating.
        foreach (ScannedFileInfo file in SelectedFiles.ToArray())
            FileList.Remove(file);

        return Task.CompletedTask;
    }

    /// <summary>
    /// This method is called when files are dropped on the ListView.
    /// </summary>
    /// <param name="files"></param>
    public async void OnFilesDropped(IStorageItem[] files)
    {
        IEnumerable<ScannedFileInfo> scannedFileInfos = await _fileScanner.ScanAsync(StorageItemConverter.ToFileInfo(files));
        var errorMessages = new List<string>();

        foreach (var result in _fileValidator.Validate(scannedFileInfos))
        {
            Debug.Print($"Validation: {result.Severity} - {result.Message}");
            if (result.Severity == FileValidationSeverity.Error)
            {
                errorMessages.Add(result.Message);
            }
        }

        if (errorMessages.Count > 0)
        {
            WeakReferenceMessenger.Default.Send(
                new DialogMessage(
                    "Validation Errors",
                    string.Join(Environment.NewLine, errorMessages)
                )
            );
        }

        foreach (var file in scannedFileInfos)
        {
            Debug.Print($"File dropped: {file.Path}");
            FileList.Add(file);
        }
    }
}
