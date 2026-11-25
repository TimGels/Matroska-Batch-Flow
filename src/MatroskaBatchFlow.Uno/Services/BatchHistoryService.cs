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
public partial class BatchHistoryService : ObservableObject, IBatchHistoryService
{
    private const int LegacyFormatVersion = 1;
    private const int TimestampFormatVersion = 2;
    private const int FormatVersion = 3;
    private static readonly HistoryHeader NeverCleared = new(LegacyFormatVersion);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ObservableCollection<BatchRunRecord> _runs = [];
    private readonly List<PendingRun> _unsavedRuns = [];
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly ILogger<BatchHistoryService> _logger;
    private Task? _loadTask;
    private bool _loaded;
    private HistoryHeader? _lastSeenHeader;

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
    [ObservableProperty]
    public partial bool HasUnreadableHistory { get; private set; }

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
            var (runs, header) = await ReadForUpdateAsync();
            var removed = Math.Max(0, runs.Length - maxRuns);
            var kept = runs.Take(maxRuns).ToArray();
            if (removed > 0)
            {
                ReplaceRuns(runs.Take(previousLimit).ToArray());
                if (removed != confirmedRemovals)
                    return removed;

                // Trim before saving the lower limit, so a failed trim cannot take effect on the next save.
                await Task.Run(() => Write(kept, header));
                _unsavedRuns.RemoveAll(pending => pending.Order == ClearOrder.Known);
                HasUnreadableHistory = false;
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

        var lastSeen = _lastSeenHeader;
        var headerAtStart = ReadHeaderAtAddStartAsync();
        await _fileLock.WaitAsync();
        try
        {
            var observed = await headerAtStart;
            var order = observed is null ? ClearOrder.Unknown : ClearOrder.Known;
            _unsavedRuns.Insert(0, new PendingRun(run, observed ?? lastSeen, order));
            BatchRunRecord[]? kept = null;
            try
            {
                using var storageLock = await FileLock.AcquireAsync(_filePath);

                // Save every run when the limit cannot be read: an unknown limit must not remove runs, and runs kept
                // only in memory would be lost when the app closes.
                var maxRuns = await TryReadPersistedMaxRunsAsync();
                var (runs, header) = await ReadForUpdateAsync();
                kept = runs.Take(maxRuns ?? runs.Length).ToArray();
                await Task.Run(() => Write(kept, header));
                _unsavedRuns.RemoveAll(pending => pending.Order == ClearOrder.Known);
                HasUnreadableHistory = false;
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
                ReplaceRuns(kept is null ? _runs : kept);
            }
        }
        finally
        {
            ReleaseFileLock();
        }
    }

    /// <inheritdoc/>
    public async Task<HistoryRemovalResult> RemoveAsync(HistoryRemoval removal)
    {
        ArgumentNullException.ThrowIfNull(removal);
        if (removal.RunIds.IsEmpty)
            throw new ArgumentException("Select at least one run to remove.", nameof(removal));

        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await FileLock.AcquireAsync(_filePath);
            var (runs, header) = await ReadForUpdateAsync();
            var kept = runs.Where(run => !removal.RunIds.Contains(run.Id)).ToArray();
            var visibleIds = runs.Select(run => run.Id).Concat(_unsavedRuns.Select(pending => pending.Run.Id)).ToHashSet();
            var removed = visibleIds.Count(removal.RunIds.Contains);
            await Task.Run(() => Write(kept, header));
            _unsavedRuns.RemoveAll(pending => removal.RunIds.Contains(pending.Run.Id) || pending.Order == ClearOrder.Known);
            ReplaceRuns(kept);
            HasUnreadableHistory = false;
            LogRunRemoved(removed);
            return new(HistoryRemovalStatus.Removed, removed);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogRemoveFailed(ex, _filePath);
            return new(HistoryRemovalStatus.Failed);
        }
        finally
        {
            ReleaseFileLock();
        }
    }

    /// <inheritdoc/>
    public async Task<HistoryRecoveryResult> ResetUnreadableHistoryAsync(HistoryRecoveryDecision? decision = null)
    {
        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await FileLock.AcquireAsync(_filePath);
            var previouslyUnreadable = HasUnreadableHistory;
            BatchRunRecord[] runs;
            HistoryHeader header;
            try
            {
                (runs, header) = await ReadMergedAsync();
            }
            catch (Exception ex) when (previouslyUnreadable && ex is JsonException or InvalidDataException)
            {
                // A decision about a readable file never authorizes overwriting an unreadable one.
                if (decision is not null)
                    return new(HistoryRecoveryStatus.ConfirmUnreadableReset);

                LogResettingUnreadableHistory(ex, _filePath);
                var recoveredRuns = _runs.Concat(_unsavedRuns.Select(pending => pending.Run))
                    .DistinctBy(run => run.Id)
                    .OrderByDescending(run => run.RecordedAt)
                    .ToArray();
                var resetHeader = new HistoryHeader(FormatVersion, ClearId: Guid.NewGuid());
                await SaveRemainingAsync(recoveredRuns, resetHeader, clearAllPending: true);
                return new(HistoryRecoveryStatus.Recovered);
            }

            var missing = MissingListedIds(runs);
            var restore = false;
            if (missing.Count > 0)
            {
                if (decision is null || !missing.SetEquals(decision.MissingIds))
                    return new(HistoryRecoveryStatus.ChooseMissingResults) { MissingIds = missing.ToImmutableHashSet() };

                restore = decision.RestoreMissing;
                if (restore)
                {
                    runs = runs.Concat(_runs.Where(run => missing.Contains(run.Id)))
                        .OrderByDescending(run => run.RecordedAt)
                        .ToArray();
                }
            }

            if (restore || _unsavedRuns.Any(pending => pending.Order == ClearOrder.Known))
            {
                await SaveRemainingAsync(runs, header);
            }
            else
            {
                ReplaceRuns(runs);
                HasUnreadableHistory = false;
            }

            return new(HistoryRecoveryStatus.Recovered);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogResetFailed(ex, _filePath);
            return new(HistoryRecoveryStatus.Failed);
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

            var header = new HistoryHeader(FormatVersion, ClearId: Guid.NewGuid());
            await Task.Run(() => Write([], header));
            _lastSeenHeader = header;
            var removed = _runs.Count;
            _unsavedRuns.Clear();
            _runs.Clear();
            HasUnreadableHistory = false;
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
            var (runs, _) = await ReadForUpdateAsync();
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
    /// Opens a snapshot before the add waits for either lock. Replacing the history file atomically cannot
    /// change the marker in the open snapshot, and the snapshot does not block a clear.
    /// </summary>
    private async Task<HistoryHeader?> ReadHeaderAtAddStartAsync()
    {
        try
        {
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                bufferSize: 4096, useAsync: true);
            return ValidateHeader(await JsonSerializer.DeserializeAsync<HistoryHeader>(stream));
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return NeverCleared;
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogClearMarkerReadFailed(ex, _filePath);
            return null;
        }
    }

    private HashSet<Guid> MissingListedIds(BatchRunRecord[] merged)
    {
        var unsavedIds = _unsavedRuns.Select(pending => pending.Run.Id).ToHashSet();
        return _runs.Select(run => run.Id).Where(id => !unsavedIds.Contains(id))
            .Except(merged.Select(run => run.Id)).ToHashSet();
    }

    private async Task<(BatchRunRecord[] Runs, HistoryHeader Header)> ReadForUpdateAsync()
    {
        var merged = await ReadMergedAsync();
        if (HasUnreadableHistory && MissingListedIds(merged.Runs).Count > 0)
        {
            throw new InvalidDataException(
                "Repaired history omits listed results. Resolve the conflict in Results before changing history.");
        }

        return merged;
    }

    private async Task SaveRemainingAsync(BatchRunRecord[] runs, HistoryHeader header, bool clearAllPending = false)
    {
        await Task.Run(() => Write(runs, header));
        _lastSeenHeader = header;
        if (clearAllPending)
        {
            _unsavedRuns.Clear();
        }
        else
        {
            _unsavedRuns.RemoveAll(pending => pending.Order == ClearOrder.Known);
        }

        ReplaceRuns(runs);
        HasUnreadableHistory = false;
    }

    private async Task<(BatchRunRecord[] Runs, HistoryHeader Header)> ReadMergedAsync()
    {
        try
        {
            var (saved, header) = await Task.Run(Read);
            _lastSeenHeader = header;
            _unsavedRuns.RemoveAll(pending => pending.Order != ClearOrder.Unknown && pending.Header != header);
            for (var index = 0; index < _unsavedRuns.Count; index++)
            {
                var pending = _unsavedRuns[index];
                if (pending.Order != ClearOrder.Unknown)
                    continue;

                // An unreadable snapshot followed by an unfamiliar clear proves neither order. Keep the result
                // visible, but never write it back across that clear. A subsequent clear can safely remove it.
                var order = header == NeverCleared || pending.Header == header ? ClearOrder.Known : ClearOrder.SessionOnly;
                _unsavedRuns[index] = pending with { Header = header, Order = order };
            }

            return (Merge(_unsavedRuns.Where(pending => pending.Order == ClearOrder.Known), saved), header);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            HasUnreadableHistory = true;
            throw;
        }
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

    private static BatchRunRecord[] Merge(IEnumerable<PendingRun> pending, IEnumerable<BatchRunRecord> saved) =>
        pending.Select(item => item.Run).Concat(saved).DistinctBy(run => run.Id).OrderByDescending(run => run.RecordedAt).ToArray();

    /// <summary>
    /// Updates <see cref="HasUnsavedRuns"/> for the runs an operation left behind, then lets the next operation start.
    /// </summary>
    private void ReleaseFileLock()
    {
        try
        {
            var unsavedIds = _unsavedRuns.Select(pending => pending.Run.Id).ToHashSet();
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
        if (_unsavedRuns.Count > 0)
        {
            runs = Merge(_unsavedRuns, runs);
        }

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
    private (BatchRunRecord[] Runs, HistoryHeader Header) Read()
    {
        string json;
        try
        {
            json = File.ReadAllText(_filePath);
        }
        catch (FileNotFoundException)
        {
            return ([], NeverCleared);
        }

        var document = JsonSerializer.Deserialize<BatchHistoryDocument>(json, SerializerOptions);
        var header = ValidateHeader(document?.GetHeader());
        if (!IsComplete(document?.Runs))
        {
            LogIncompleteFile(_filePath);
            throw new InvalidDataException("The batch history file is incomplete.");
        }

        return (document.Runs.Select(run => run with { Files = run.Files.Select(Normalize).ToArray() }).ToArray(), header);
    }

    private HistoryHeader ValidateHeader(HistoryHeader? header)
    {
        if (header is null || header.Version is not (LegacyFormatVersion or TimestampFormatVersion or FormatVersion))
        {
            LogUnsupportedFile(header?.Version, _filePath);
            throw new InvalidDataException("Unsupported batch history format.");
        }

        if (header.Version == FormatVersion)
        {
            if (header.ClearId is { } id && id != Guid.Empty)
                return new HistoryHeader(FormatVersion, ClearId: id);
        }
        else
        {
            if (header.ClearedAt is { } time && time != default)
                return new HistoryHeader(TimestampFormatVersion, time);

            if (header.Version == LegacyFormatVersion && header.ClearedAt is null)
                return NeverCleared;
        }

        LogIncompleteFile(_filePath);
        throw new InvalidDataException("The batch history file is incomplete.");
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

    private void Write(BatchRunRecord[] runs, HistoryHeader header)
    {
        // Write next to the file first, so a crash during the write cannot leave a truncated history.
        var temporaryPath = _filePath + ".tmp";
        var document = new BatchHistoryDocument(header.Version, runs, header.ClearedAt, header.ClearId);
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions));
        // While holding the sidecar, replace an existing file without invalidating open snapshots.
        // Move(overwrite: true) can fail on Windows even when every reader shares deletion.
        if (File.Exists(_filePath))
        {
            File.Replace(temporaryPath, _filePath, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporaryPath, _filePath);
        }
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
    /// <param name="ClearedAt">The clear marker used by older history files.</param>
    /// <param name="ClearId">The unique identifier of the most recent clear.</param>
    private sealed record BatchHistoryDocument(
        int Version,
        IReadOnlyList<BatchRunRecord>? Runs,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ClearedAt,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? ClearId)
    {
        public HistoryHeader GetHeader() => new(Version, ClearedAt, ClearId);
    }

    private sealed record HistoryHeader(int Version, DateTimeOffset? ClearedAt = null, Guid? ClearId = null);
    private sealed record PendingRun(BatchRunRecord Run, HistoryHeader? Header, ClearOrder Order);
    private enum ClearOrder { Unknown, Known, SessionOnly }
}
