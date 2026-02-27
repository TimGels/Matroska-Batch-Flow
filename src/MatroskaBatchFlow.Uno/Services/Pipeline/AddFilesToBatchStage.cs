using MatroskaBatchFlow.Core.Services.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// Pipeline stage that adds scanned files to the batch via <see cref="IFileListAdapter"/>.
/// Reads <see cref="PipelineContextKeys.ScannedFiles"/> from the context.
/// </summary>
public sealed class AddFilesToBatchStage(IFileListAdapter fileListAdapter) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Adding files to batch\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => true;

    /// <inheritdoc />
    public bool ShowsOverlay => false;

    /// <inheritdoc />
    public Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        if (!context.TryGet<List<ScannedFileInfo>>(PipelineContextKeys.ScannedFiles, out var scannedFiles) || scannedFiles.Count == 0)
            return Task.CompletedTask;

        ct.ThrowIfCancellationRequested();
        fileListAdapter.AddFiles(scannedFiles);

        return Task.CompletedTask;
    }
}
