using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MKVBatchFlow.Uno.Presentation;

public interface IFilesDropped
{
    void OnFilesDropped(IStorageItem[] files);
}

public partial class InputViewModel : ObservableObject, IFilesDropped
{
    [ObservableProperty]
    private ObservableCollection<IStorageItem> fileList = [];

    [ObservableProperty]
    private ObservableCollection<IStorageItem> selectedFiles = [];

    public InputViewModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo)
    {
        RemoveSelected = new AsyncRelayCommand(RemoveSelectedFiles);
    }

    public ICommand RemoveSelected { get; }

    private Task RemoveSelectedFiles()
    {
        // Convert SelectedFiles to an array to make a copy to avoid modifying the collection while iterating.
        foreach (IStorageItem file in SelectedFiles.ToArray())
            FileList.Remove(file);

        return Task.CompletedTask;
    }

    /// <summary>
    /// This method is called when files are dropped on the ListView.
    /// </summary>
    /// <param name="files"></param>
    public void OnFilesDropped(IStorageItem[] files)
    {
        foreach (var file in files)
        {
            Debug.Print($"File dropped: {file.Path}");
            FileList.Add(file);
        }
    }
}
