namespace MatroskaBatchFlow.Uno.Behavior;
public interface IFilesDropped
{
    void OnFilesDropped(IStorageItem[] files);
}
