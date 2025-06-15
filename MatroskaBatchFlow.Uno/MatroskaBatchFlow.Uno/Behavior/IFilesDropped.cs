namespace MatroskaBatchFlow.Uno.Behavior;
public interface IFilesDropped
{
    Task OnFilesDropped(IStorageItem[] files);
}
