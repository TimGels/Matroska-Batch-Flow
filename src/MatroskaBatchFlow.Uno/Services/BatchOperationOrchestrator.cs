using MatroskaBatchFlow.Core.Services.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Services.Pipeline;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Orchestrates batch-mutating file operations by composing pipeline stages
/// and delegating execution to <see cref="IPipelineRunner"/>.
/// </summary>
/// <param name="runner">The pipeline runner that executes stages sequentially.</param>
/// <param name="filterDuplicatesStage">Stage: filters duplicate files from the input set.</param>
/// <param name="scanFilesStage">Stage: scans files with MediaInfo.</param>
/// <param name="refreshStaleMetadataStage">Stage: refreshes metadata for stale files.</param>
/// <param name="initTrackConfigStage">Stage: initializes track configurations and applies processing rules.</param>
/// <param name="addFilesStage">Stage: adds scanned files to the batch.</param>
/// <param name="removeFilesStage">Stage: removes files from the batch.</param>
/// <param name="validateStage">Stage: triggers revalidation.</param>
/// <param name="logger">The logger for recording orchestration operations.</param>
public sealed partial class BatchOperationOrchestrator(
    IPipelineRunner runner,
    FilterDuplicateFilesStage filterDuplicatesStage,
    ScanFilesStage scanFilesStage,
    RefreshStaleMetadataStage refreshStaleMetadataStage,
    InitializeTrackConfigStage initTrackConfigStage,
    AddFilesToBatchStage addFilesStage,
    RemoveFilesFromBatchStage removeFilesStage,
    ValidateStage validateStage,
    ILogger<BatchOperationOrchestrator> logger) : IBatchOperationOrchestrator
{
    /// <inheritdoc />
    public async Task ImportFilesAsync(FileInfo[] files)
    {
        if (files is null or { Length: 0 })
            return;

        LogImportingFiles(files.Length);

        var context = new PipelineContext();
        context.Set(PipelineContextKeys.InputFiles, files);

        await runner.RunAsync(
        [
            filterDuplicatesStage,
            scanFilesStage,
            refreshStaleMetadataStage,
            initTrackConfigStage,
            addFilesStage,
            validateStage
        ], context);
    }

    /// <inheritdoc />
    public async Task RemoveFilesAsync(IEnumerable<ScannedFileInfo> files)
    {
        var filesToRemove = files.ToList();
        if (filesToRemove.Count == 0)
            return;

        var context = new PipelineContext();
        context.Set(PipelineContextKeys.FilesToRemove, filesToRemove);

        await runner.RunAsync(
        [
            removeFilesStage,
            validateStage
        ], context);
    }

    /// <inheritdoc />
    public async Task RevalidateAsync()
    {
        var context = new PipelineContext();

        await runner.RunAsync(
        [
            validateStage
        ], context);
    }
}
