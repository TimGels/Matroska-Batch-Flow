using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Keeps the results of finished batches, newest first, and saves them on this device so they survive a restart.
/// </summary>
/// <remarks>
/// Members must be called on the UI thread, because <see cref="Runs"/> is bound by the Results page.
/// Saving is best effort: a run stays listed for this session when it cannot be written to disk.
/// </remarks>
public interface IBatchHistoryService : INotifyPropertyChanged
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
    /// Gets whether <see cref="Runs"/> includes results kept only in memory. Failed saves are retried, but a run
    /// that cannot be ordered against a clear stays in this session only. Closing the app loses unsaved results.
    /// </summary>
    bool HasUnsavedRuns { get; }

    /// <summary>
    /// Raised after <see cref="HasUnsavedRuns"/> changes.
    /// </summary>
    event EventHandler? HasUnsavedRunsChanged;

    /// <summary>
    /// Saves how many runs are kept, but only removes saved runs if the confirmed count still matches.
    /// </summary>
    /// <remarks>
    /// The history is trimmed before the limit is saved. If saving the limit then fails, the prior history is
    /// restored when possible and <see cref="Runs"/> is not changed. If that rollback also fails, recovery is required.
    /// </remarks>
    /// <exception cref="BatchHistoryLimitSaveException">
    /// The limit could not be saved; <see cref="BatchHistoryLimitSaveException.HistoryTrimmed"/> reports whether
    /// confirmed runs were already removed.
    /// </exception>
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
    /// Adds a finished run as the newest, drops the oldest runs beyond the saved limit and saves the history.
    /// If the limit cannot be read, every run is saved.
    /// </summary>
    /// <param name="run">The finished run.</param>
    /// <returns>A task that completes when the history is saved or saving failed.</returns>
    Task AddAsync(BatchRunRecord run);

    /// <summary>
    /// Removes only the confirmed runs in one write, even if every listed run was selected.
    /// </summary>
    /// <param name="removal">The runs captured when the removal was confirmed. Runs that are no longer listed are ignored.</param>
    /// <returns>The outcome and how many listed runs were removed.</returns>
    /// <exception cref="ArgumentException"><paramref name="removal"/> contains no runs.</exception>
    Task<HistoryRemovalResult> RemoveAsync(HistoryRemoval removal);

    /// <summary>
    /// Gets whether an unreadable history file or its pending reconciliation still requires recovery.
    /// </summary>
    bool HasUnreadableHistory { get; }

    /// <summary>
    /// Rechecks history under the lock. A readable file missing previously listed results requires a decision about
    /// those exact IDs.
    /// </summary>
    /// <param name="decision">The choice about the results from <see cref="HistoryRecoveryStatus.ChooseMissingResults"/>, or <see langword="null"/> before one is asked for.</param>
    /// <returns>The outcome, or the decision the user must make before trying again.</returns>
    Task<HistoryRecoveryResult> ResetUnreadableHistoryAsync(HistoryRecoveryDecision? decision = null);

    /// <summary>
    /// Removes all saved runs from this device. The media files themselves are not touched.
    /// </summary>
    /// <remarks>
    /// The empty history retains a unique clear marker so other instances do not restore earlier runs, even
    /// if the system clock changes.
    /// </remarks>
    /// <returns>Whether the history was successfully removed.</returns>
    Task<bool> ClearAsync();
}
