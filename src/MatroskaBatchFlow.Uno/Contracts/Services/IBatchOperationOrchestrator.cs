namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Orchestrates batch-mutating file operations (import, remove, revalidate)
/// and manages overlay feedback state throughout each pipeline.
/// </summary>
public interface IBatchOperationOrchestrator
{
    /// <summary>
    /// Imports files by scanning them with MediaInfo, refreshing stale metadata,
    /// initializing track configurations, and adding them to the batch.
    /// Duplicate files are filtered and reported to the user.
    /// </summary>
    /// <param name="files">The files to import into the current batch.</param>
    Task ImportFilesAsync(FileInfo[] files);

    /// <summary>
    /// Removes the specified files from the current batch.
    /// </summary>
    /// <param name="files">The scanned file info objects to remove.</param>
    Task RemoveFilesAsync(IEnumerable<ScannedFileInfo> files);

    /// <summary>
    /// Forces a re-validation of the current batch against current validation settings.
    /// </summary>
    Task RevalidateAsync();
}
