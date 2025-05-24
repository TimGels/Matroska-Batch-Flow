namespace MatroskaBatchFlow.Core.Services;

public interface IFileScanner
{
    Task<IEnumerable<string>> ScanAsync();
    Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync();
}
