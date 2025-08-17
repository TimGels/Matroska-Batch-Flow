namespace MatroskaBatchFlow.Uno.Behavior;

public interface IFilesDropped
{
    Task OnFilesDroppedAsync(IStorageItem[] files);
}
