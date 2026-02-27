namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Well-known keys for values stored in <see cref="PipelineContext"/>.
/// </summary>
public static class PipelineContextKeys
{
    /// <summary>
    /// <see cref="FileInfo"/>[] — raw file references to import.
    /// </summary>
    public const string InputFiles = nameof(InputFiles);

    /// <summary>
    /// <see cref="List{ScannedFileInfo}"/> — files after MediaInfo scanning.
    /// </summary>
    public const string ScannedFiles = nameof(ScannedFiles);

    /// <summary>
    /// <see cref="List{ScannedFileInfo}"/> — files to remove from the batch.
    /// </summary>
    public const string FilesToRemove = nameof(FilesToRemove);
}
