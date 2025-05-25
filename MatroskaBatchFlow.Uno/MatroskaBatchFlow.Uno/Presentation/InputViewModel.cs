using System.Collections.ObjectModel;
using System.Diagnostics;
using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Behavior;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Presentation;
[Bindable]
public partial class InputViewModel : ObservableObject, IFilesDropped
{
    [ObservableProperty]
    private ObservableCollection<ScannedFileInfo> fileList = [];

    [ObservableProperty]
    private ObservableCollection<ScannedFileInfo> selectedFiles = [];

    private IFileScanner _fileScanner;

    public ICommand RemoveSelected { get; }

    public InputViewModel(IStringLocalizer localizer, IOptions<AppConfig> appInfo, IFileScanner fileScanner)
    {
        _fileScanner = fileScanner;
        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFiles);
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
        foreach (var file in scannedFileInfos)
        {
            Debug.Print($"File dropped: {file.Path}");
            FileList.Add(file);
        }
    }
}
