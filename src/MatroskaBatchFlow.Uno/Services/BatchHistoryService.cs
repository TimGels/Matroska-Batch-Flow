using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Utilities;
using static MatroskaBatchFlow.Core.Models.UserSettings;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Keeps the results of finished batches in a JSON file in the local application data folder.
/// </summary>
public partial class BatchHistoryService : IBatchHistoryService
{
    /// <summary>
    /// The current version of the file format. A file with an unsupported version must not be overwritten.
    /// </summary>
    private const int LegacyFormatVersion = 1;
    private const int FormatVersion = 2;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ObservableCollection<BatchRunRecord> _runs = [];
    private readonly List<BatchRunRecord> _unsavedRuns = [];
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly ILogger<BatchHistoryService> _logger;
    private Task? _loadTask;
    private bool _loaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchHistoryService"/> class.
    /// </summary>
    /// <param name="filePath">The path of the history file.</param>
    /// <param name="userSettings">The user settings that hold how many runs are kept.</param>
    /// <param name="logger">The logger.</param>
    public BatchHistoryService(string filePath, IWritableSettings<UserSettings> userSettings, ILogger<BatchHistoryService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = Path.GetFullPath(filePath);
        _userSettings = userSettings;
        _logger = logger;
        Runs = new ReadOnlyObservableCollection<BatchRunRecord>(_runs);
    }

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<BatchRunRecord> Runs { get; }

    /// <inheritdoc/>
    public int MaxRuns => Math.Clamp(
        _userSettings.Value.BatchHistory?.MaxRuns ?? BatchHistorySettings.DefaultMaxRuns,
        BatchHistorySettings.MinMaxRuns,
        BatchHistorySettings.MaxMaxRuns);

    /// <inheritdoc/>
    public bool HasUnsavedRuns { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? HasUnsavedRunsChanged;

    /// <inheritdoc/>
    public async Task<int> SetMaxRunsAsync(int maxRuns, int confirmedRemovals = 0)
    {
        maxRuns = Math.Clamp(maxRuns, BatchHistorySettings.MinMaxRuns, BatchHistorySettings.MaxMaxRuns);
        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await FileLock.AcquireAsync(_filePath);
            var previousLimit = await ReadPersistedMaxRunsAsync();
            var (runs, clearedAt) = await ReadMergedAsync();
            var removed = Math.Max(0, runs.Length - maxRuns);
            if (removed > 0 && removed != confirmedRemovals)
                return removed;

            // Trim before saving the lower limit: a saved limit without the trimmed history would remove runs the next
            // time the history is saved, even though the change failed.
            var kept = runs.Take(maxRuns).ToArray();
            if (removed > 0)
            {
                await Task.Run(() => Write(kept, clearedAt));
                _unsavedRuns.Clear();
            }

            ReplaceRuns(kept);
            if (maxRuns != previousLimit)
            {
                try
                {
                    await _userSettings.UpdateAsync(
                        settings => settings.BatchHistory.MaxRuns = maxRuns,
                        settings => settings.BatchHistory.MaxRuns = previousLimit);
                }
                catch (Exception ex)
                {
                    throw new BatchHistoryLimitSaveException(removed > 0, ex);
                }
            }

            LogLimitChanged(previousLimit, maxRuns, removed);
            return 0;
        }
        finally
        {
            ReleaseFileLock();
        }
    }

    /// <inheritdoc/>
    public Task LoadAsync()
    {
        if (_loadTask is null || (_loadTask.IsCompleted && !_loaded))
        {
            _loadTask = LoadCoreAsync();
        }

        return _loadTask;
    }

    /// <inheritdoc/>
    public async Task AddAsync(BatchRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var addStartedAt = DateTimeOffset.UtcNow;
        await _fileLock.WaitAsync();
        try
        {
            _unsavedRuns.Insert(0, run);
            int? maxRuns = null;
            BatchRunRecord[]? merged = null;
            try
            {
                using var storageLock = await FileLock.AcquireAsync(_filePath);

                // Save every run when the limit cannot be read: an unknown limit must not remove runs, and runs kept
                // only in memory would be lost when the app closes.
                maxRuns = await TryReadPersistedMaxRunsAsync();
                var (runs, clearedAt) = await ReadMergedAsync(run, addStartedAt);
                merged = runs;
                var kept = runs.Take(maxRuns ?? runs.Length).ToArray();
                await Task.Run(() => Write(kept, clearedAt));
                _unsavedRuns.Clear();
                ReplaceRuns(kept);
                if (maxRuns is { } limit && runs.Length > limit)
                {
                    LogOldestRunsRemoved(runs.Length - limit, limit);
                }
            }
            catch (Exception ex) when (IsStorageFailure(ex))
            {
                LogSaveFailed(ex, _filePath);

                // Show the history that was just read when possible, so runs another instance removed do not reappear.
                var runs = merged ?? Merge(_runs).ToArray();
                if (maxRuns is { } persistedLimit)
                {
                    ReplaceRuns(runs.Take(persistedLimit).ToArray());
                    if (_unsavedRuns.Count > persistedLimit)
                    {
                        _unsavedRuns.RemoveRange(persistedLimit, _unsavedRuns.Count - persistedLimit);
                    }
                }
                else
                {
                    ReplaceRuns(runs);
                }
            }
        }
        finally
        {
            ReleaseFileLock();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClearAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await FileLock.AcquireAsync(_filePath);

            // Keep an empty file that records when it was cleared, so other app instances drop their unsaved
            // runs from before the clear instead of saving them again.
            var clearedAt = DateTimeOffset.UtcNow;
            await Task.Run(() => Write([], clearedAt));
            var removed = _runs.Count;
            _unsavedRuns.Clear();
            _runs.Clear();
            LogCleared(removed);
            return true;
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogClearFailed(ex, _filePath);
            return false;
        }
        finally
        {
            ReleaseFileLock();
        }
    }

    private async Task LoadCoreAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await FileLock.AcquireAsync(_filePath);
            var (runs, _) = await ReadMergedAsync();
            _loaded = true;

            // Show every saved run when the limit cannot be read, rather than hiding runs behind a default limit.
            var maxRuns = await TryReadPersistedMaxRunsAsync() ?? runs.Length;
            ReplaceRuns(runs.Take(maxRuns).ToArray());
            LogLoaded(_runs.Count, _filePath);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogLoadFailed(ex, _filePath);
        }
        finally
        {
            ReleaseFileLock();
        }
    }

    /// <summary>
    /// Reads the saved runs and combines them with the runs this instance could not save yet.
    /// </summary>
    /// <remarks>
    /// Unsaved runs from before the history was last cleared are dropped. An added run is exempt only if its add
    /// operation began after the clear.
    /// </remarks>
    /// <param name="addedRun">The run that is being added, or <see langword="null"/>.</param>
    /// <param name="addStartedAt">When the add operation began, or <see langword="null"/>.</param>
    /// <returns>The runs, newest first, and when the history was last cleared.</returns>
    private async Task<(BatchRunRecord[] Runs, DateTimeOffset? ClearedAt)> ReadMergedAsync(
        BatchRunRecord? addedRun = null,
        DateTimeOffset? addStartedAt = null)
    {
        var saved = await Task.Run(Read);
        if (saved.ClearedAt is { } clearedAt)
        {
            var addedBeforeClear = addedRun is not null && addStartedAt is { } startedAt && startedAt <= clearedAt;
            _unsavedRuns.RemoveAll(run => ReferenceEquals(run, addedRun)
                ? addedBeforeClear
                : run.RecordedAt <= clearedAt);
        }

        return (Merge(saved.Runs!).ToArray(), saved.ClearedAt);
    }

    /// <summary>
    /// Reads the saved history limit.
    /// </summary>
    /// <returns>The limit, or <see langword="null"/> when it cannot be read.</returns>
    private async Task<int?> TryReadPersistedMaxRunsAsync()
    {
        try
        {
            return await ReadPersistedMaxRunsAsync();
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogLimitReadFailed(ex);
            return null;
        }
    }

    private async Task<int> ReadPersistedMaxRunsAsync()
    {
        var settings = await _userSettings.ReadFromDiskAsync();
        if (settings.BatchHistory is null)
        {
            throw new InvalidDataException("The saved user settings have no batch history section.");
        }

        var maxRuns = Math.Clamp(settings.BatchHistory.MaxRuns, BatchHistorySettings.MinMaxRuns, BatchHistorySettings.MaxMaxRuns);
        if (_userSettings.Value.BatchHistory is { } cachedHistory)
        {
            cachedHistory.MaxRuns = maxRuns;
        }

        return maxRuns;
    }

    private IEnumerable<BatchRunRecord> Merge(IEnumerable<BatchRunRecord> saved) =>
        _unsavedRuns.Concat(saved).DistinctBy(run => run.Id).OrderByDescending(run => run.RecordedAt);

    /// <summary>
    /// Updates <see cref="HasUnsavedRuns"/> for the runs an operation left behind, then lets the next operation start.
    /// </summary>
    private void ReleaseFileLock()
    {
        try
        {
            var unsavedIds = _unsavedRuns.Select(run => run.Id).ToHashSet();
            var hasUnsavedRuns = _runs.Any(run => unsavedIds.Contains(run.Id));
            if (hasUnsavedRuns != HasUnsavedRuns)
            {
                HasUnsavedRuns = hasUnsavedRuns;
                HasUnsavedRunsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private void ReplaceRuns(IReadOnlyList<BatchRunRecord> runs)
    {
        var keptIds = runs.Select(run => run.Id).ToHashSet();
        for (var index = _runs.Count - 1; index >= 0; index--)
        {
            if (!keptIds.Contains(_runs[index].Id))
            {
                _runs.RemoveAt(index);
            }
        }

        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];
            if (index < _runs.Count && _runs[index].Id == run.Id)
                continue;

            var previous = _runs.FirstOrDefault(existing => existing.Id == run.Id);
            if (previous is not null)
            {
                _runs.Move(_runs.IndexOf(previous), index);
            }
            else
            {
                _runs.Insert(index, run);
            }
        }
    }

    private static bool IsStorageFailure(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException;

    /// <summary>
    /// Reads the history file.
    /// </summary>
    /// <returns>The saved history, or an empty history when there is no file.</returns>
    /// <exception cref="InvalidDataException">
    /// The file has another format version or is incomplete. It must be kept as it is, because saving would drop
    /// the runs that could not be read.
    /// </exception>
    private BatchHistoryDocument Read()
    {
        string json;
        try
        {
            json = File.ReadAllText(_filePath);
        }
        catch (FileNotFoundException)
        {
            return new BatchHistoryDocument(FormatVersion, Array.Empty<BatchRunRecord>());
        }

        var document = JsonSerializer.Deserialize<BatchHistoryDocument>(json, SerializerOptions);
        if (document is null || document.Version is not LegacyFormatVersion and not FormatVersion)
        {
            LogUnsupportedFile(document?.Version, _filePath);
            throw new InvalidDataException("Unsupported batch history format.");
        }

        if (document.Version == FormatVersion
            && (document.ClearedAt is null || document.ClearedAt.Value == default))
        {
            LogIncompleteFile(_filePath);
            throw new InvalidDataException("The batch history file is incomplete.");
        }

        if (!IsComplete(document.Runs))
        {
            LogIncompleteFile(_filePath);
            throw new InvalidDataException("The batch history file is incomplete.");
        }

        return document with
        {
            Runs = document.Runs.Select(run => run with { Files = run.Files.Select(Normalize).ToArray() }).ToArray()
        };
    }

    /// <summary>
    /// Checks that every run and file in the history can be shown and saved again without losing data.
    /// </summary>
    /// <param name="runs">The runs as read from the file.</param>
    /// <returns>Whether the runs are complete.</returns>
    private static bool IsComplete([NotNullWhen(true)] IReadOnlyList<BatchRunRecord>? runs)
    {
        if (runs is null)
            return false;

        var seenRunIds = new HashSet<Guid>();
        foreach (var run in runs)
        {
            if (run is null
                || run.Id == Guid.Empty
                || run.RecordedAt == default
                || run.Files is null)
                return false;

            if (!seenRunIds.Add(run.Id))
                return false;

            foreach (var file in run.Files)
            {
                if (file is null || file.Path is null)
                    return false;

                if (!Enum.IsDefined(file.Status))
                    return false;
            }
        }

        return true;
    }

    private void Write(BatchRunRecord[] runs, DateTimeOffset? clearedAt)
    {
        // Write next to the file first, so a crash during the write cannot leave a truncated history.
        var temporaryPath = _filePath + ".tmp";
        var version = clearedAt is null ? LegacyFormatVersion : FormatVersion;
        var document = new BatchHistoryDocument(version, runs, clearedAt);
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    /// <summary>
    /// Replaces missing lists from a hand-edited or damaged file with empty ones.
    /// </summary>
    /// <param name="file">The file as read from the history file.</param>
    /// <returns>The file with empty lists instead of missing ones.</returns>
    private static FileRunRecord Normalize(FileRunRecord file) => file with
    {
        Errors = file.Errors ?? Array.Empty<string>(),
        Warnings = file.Warnings ?? Array.Empty<string>()
    };

    /// <summary>
    /// The content of the history file.
    /// </summary>
    /// <param name="Version">The version of the file format.</param>
    /// <param name="Runs">The saved runs, newest first.</param>
    /// <param name="ClearedAt">When the history was last cleared, or <see langword="null"/> if it never was.</param>
    private sealed record BatchHistoryDocument(int Version, IReadOnlyList<BatchRunRecord>? Runs, DateTimeOffset? ClearedAt = null);
}
