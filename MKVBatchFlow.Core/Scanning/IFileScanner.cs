namespace MKVBatchFlow.Core.Scanning;

public interface IFileScanner
{
    Task<IEnumerable<string>> ScanAsync();
    Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync();
}
