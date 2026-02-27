using MatroskaBatchFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Pipeline stage that scans input files with MediaInfo to extract metadata.
/// Reads <see cref="PipelineContextKeys.InputFiles"/> and writes <see cref="PipelineContextKeys.ScannedFiles"/>.
/// </summary>
public sealed partial class ScanFilesStage(
    IFileScanner fileScanner,
    ILogger<ScanFilesStage> logger) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Scanning files\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => false;

    /// <inheritdoc />
    public bool ShowsOverlay => true;

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        if (!context.TryGet<FileInfo[]>(PipelineContextKeys.InputFiles, out var files) || files.Length == 0)
        {
            context.Set(PipelineContextKeys.ScannedFiles, new List<ScannedFileInfo>());
            return;
        }

        ct.ThrowIfCancellationRequested();

        LogScanningFiles(files.Length);
        var scannedFiles = (await fileScanner.ScanAsync(files, progress)).ToList();
        LogFilesScanned(scannedFiles.Count);

        context.Set(PipelineContextKeys.ScannedFiles, scannedFiles);
    }
}
