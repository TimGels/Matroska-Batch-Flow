using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

public interface IFileScanner
{
    Task<IEnumerable<ScannedFileInfo>> ScanAsync(FileInfo[] files);
    Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync();
}
