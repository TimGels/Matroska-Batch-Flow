using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Pipeline stage that re-scans files previously marked as stale, replacing them with fresh metadata.
/// </summary>
/// <remarks>
/// Stale files are files whose metadata may be outdated (e.g., modified externally since the last scan).
/// This stage performs a single batch re-scan operation, migrates existing configurations, and clears stale flags.
/// If a re-scan fails for any file the error is logged and the stale flag is cleared to prevent repeated attempts.
/// </remarks>
public sealed partial class RefreshStaleMetadataStage(
    IFileScanner fileScanner,
    IBatchConfiguration batchConfig,
    IScannedFileInfoPathComparer pathComparer,
    ILogger<RefreshStaleMetadataStage> logger) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Refreshing stale file metadata\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => true;

    /// <inheritdoc />
    public bool ShowsOverlay => true;

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        var staleFiles = batchConfig.GetStaleFiles().ToList();
        if (staleFiles.Count == 0)
            return;

        ct.ThrowIfCancellationRequested();
        LogRescanningStaleFiles(staleFiles.Count);

        try
        {
            var fileInfos = staleFiles.Select(f => new FileInfo(f.Path)).ToArray();
            var freshScans = (await fileScanner.ScanAsync(fileInfos, null)).ToList();

            foreach (var staleFile in staleFiles)
            {
                var freshScan = freshScans.FirstOrDefault(f => pathComparer.PathEquals(f.Path, staleFile.Path));

                if (freshScan != null)
                {
                    batchConfig.MigrateFileConfiguration(staleFile.Id, freshScan.Id);
                    batchConfig.FileList.Remove(staleFile);
                    batchConfig.FileList.Add(freshScan);
                    batchConfig.ClearStaleFlag(staleFile.Id);

                    LogFileRescanned(staleFile.Path);
                }
                else
                {
                    batchConfig.ClearStaleFlag(staleFile.Id);
                    LogRescanFailedNotFound(staleFile.Path);
                }
            }
        }
        catch (Exception ex)
        {
            LogRescanBatchFailed(staleFiles.Count, ex);

            foreach (var staleFile in staleFiles)
            {
                batchConfig.ClearStaleFlag(staleFile.Id);
            }
        }
    }
}
