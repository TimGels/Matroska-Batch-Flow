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
    /// Gets how many runs are kept, as set in the user settings.
    /// </summary>
    int MaxRuns { get; }

    /// <summary>
    /// Saves how many runs are kept, but only removes saved runs if the confirmed count still matches.
    /// </summary>
    /// <param name="maxRuns">The number of runs to keep. It is clamped to the allowed range.</param>
    /// <param name="confirmedRemovals">The number of removals the user confirmed, or zero without confirmation.</param>
    /// <returns>Zero if applied, or the number of removals that now requires confirmation.</returns>
    Task<int> SetMaxRunsAsync(int maxRuns, int confirmedRemovals = 0);

    /// <summary>
    /// Loads the saved runs from disk. After a failed load, the next call tries again.
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
    /// Removes all saved runs from this device. The media files themselves are not touched.
    /// </summary>
    /// <remarks>
    /// The history file is kept without runs, but with the time it was cleared, so other app instances do not save
    /// their earlier runs again.
    /// </remarks>
    /// <returns>Whether the history was successfully removed.</returns>
    Task<bool> ClearAsync();
}
