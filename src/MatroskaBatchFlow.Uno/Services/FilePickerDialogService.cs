using MatroskaBatchFlow.Uno.Contracts.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MatroskaBatchFlow.Uno.Services;

public class FilePickerDialogService : IFilePickerDialogService
{
    private static readonly string[] AllowedFileTypes = [".mkv"];

    /// <summary>
    /// Displays a file picker dialog that allows the user to select multiple files with a specific file type filter.
    /// </summary>
    /// <returns>A read-only list of <see cref="StorageFile"/> objects representing the files selected by the user. If no files
    /// are selected, an empty list is returned.</returns>
    public async Task<IReadOnlyList<StorageFile>> PickFilesAsync()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        foreach (var fileType in AllowedFileTypes)
        {
            picker.FileTypeFilter.Add(fileType);
        }

        nint hWnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hWnd);

        var files = await picker.PickMultipleFilesAsync();
        return files ?? [];
    }
}
