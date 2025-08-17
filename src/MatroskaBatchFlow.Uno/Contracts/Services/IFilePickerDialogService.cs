namespace MatroskaBatchFlow.Uno.Contracts.Services;

public interface IFilePickerDialogService
{
    Task<IReadOnlyList<StorageFile>> PickFilesAsync();
}
