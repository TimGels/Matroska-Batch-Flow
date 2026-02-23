using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

public interface IFileScanner
{
    Task<IEnumerable<ScannedFileInfo>> ScanAsync(FileInfo[] files, IProgress<(int current, int total)>? progress);
    Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync();
}
