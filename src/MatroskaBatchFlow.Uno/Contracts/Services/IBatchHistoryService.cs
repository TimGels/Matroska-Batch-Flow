using System.Collections.ObjectModel;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Keeps the results of finished batches, newest first, and saves them on this device so they survive a restart.
/// </summary>
/// <remarks>
/// Members must be called on the UI thread, because <see cref="Runs"/> is bound by the Results page.
/// Saving is best effort: a run stays listed for this session when it cannot be written to disk.
/// </remarks>
public interface IBatchHistoryService
{
    /// <summary>
    /// Gets the saved runs, newest first.
    /// </summary>
    ReadOnlyObservableCollection<BatchRunRecord> Runs { get; }

    /// <summary>
    /// Loads the saved runs from disk. Runs added before loading finished stay on top.
    /// </summary>
    /// <returns>A task that completes when the runs are loaded or loading failed.</returns>
    Task LoadAsync();

    /// <summary>
    /// Adds a finished run as the newest, drops the oldest runs beyond the limit and saves the history.
    /// </summary>
    /// <param name="run">The finished run.</param>
    /// <returns>A task that completes when the history is saved or saving failed.</returns>
    Task AddAsync(BatchRunRecord run);

    /// <summary>
    /// Removes all saved runs, including the file on disk. The media files themselves are not touched.
    /// </summary>
    /// <returns>A task that completes when the history is removed or removing failed.</returns>
    Task ClearAsync();
}
