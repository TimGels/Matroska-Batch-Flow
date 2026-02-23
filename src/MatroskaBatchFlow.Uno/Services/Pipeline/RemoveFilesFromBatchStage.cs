using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Uno.Contracts.Services;

namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// Pipeline stage that removes specified files from the batch via <see cref="IFileListAdapter"/>.
/// Reads <see cref="PipelineContextKeys.FilesToRemove"/> from the context.
/// </summary>
public sealed class RemoveFilesFromBatchStage(IFileListAdapter fileListAdapter) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Removing files\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => true;

    /// <inheritdoc />
    public bool ShowsOverlay => false;

    /// <inheritdoc />
    public Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        if (!context.TryGet<List<ScannedFileInfo>>(PipelineContextKeys.FilesToRemove, out var filesToRemove) || filesToRemove.Count == 0)
            return Task.CompletedTask;

        ct.ThrowIfCancellationRequested();
        fileListAdapter.RemoveFiles(filesToRemove);

        return Task.CompletedTask;
    }
}
