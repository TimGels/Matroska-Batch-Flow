using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using static MatroskaBatchFlow.Core.Models.UserSettings;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Keeps the results of finished batches in a JSON file in the local application data folder.
/// </summary>
public partial class BatchHistoryService : IBatchHistoryService
{
    /// <summary>
    /// The version of the file format. A file with another version must not be overwritten.
    /// </summary>
    private const int FormatVersion = 1;

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
    public int MaxRuns => Math.Clamp(_userSettings.Value.BatchHistory.MaxRuns, BatchHistorySettings.MinMaxRuns, BatchHistorySettings.MaxMaxRuns);

    /// <inheritdoc/>
    public async Task<int> SetMaxRunsAsync(int maxRuns, int confirmedRemovals = 0)
    {
        maxRuns = Math.Clamp(maxRuns, BatchHistorySettings.MinMaxRuns, BatchHistorySettings.MaxMaxRuns);
        await _fileLock.WaitAsync();
        try
        {
            var previousLimit = _userSettings.Value.BatchHistory.MaxRuns;
            if (maxRuns == previousLimit)
                return 0;

            using var storageLock = await AcquireStorageLockAsync();
            var runs = await ReadMergedAsync();
            var removed = Math.Max(0, runs.Length - maxRuns);
            if (removed > 0 && removed != confirmedRemovals)
                return removed;

            var kept = runs.Take(maxRuns).ToArray();
            if (removed > 0)
            {
                await Task.Run(() => Write(kept));
                _unsavedRuns.Clear();
            }

            ReplaceRuns(kept);
            await _userSettings.UpdateAsync(settings => settings.BatchHistory.MaxRuns = maxRuns);
            LogLimitChanged(previousLimit, maxRuns, removed);
            return 0;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task LoadAsync() => _loadTask ??= LoadCoreAsync();

    /// <inheritdoc/>
    public async Task AddAsync(BatchRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        await _fileLock.WaitAsync();
        try
        {
            _unsavedRuns.Insert(0, run);
            try
            {
                using var storageLock = await AcquireStorageLockAsync();
                var runs = await ReadMergedAsync();
                var kept = runs.Take(MaxRuns).ToArray();
                await Task.Run(() => Write(kept));
                _unsavedRuns.Clear();
                ReplaceRuns(kept);
                if (runs.Length > kept.Length)
                {
                    LogOldestRunsRemoved(runs.Length - kept.Length, MaxRuns);
                }
            }
            catch (Exception ex) when (IsStorageFailure(ex))
            {
                LogSaveFailed(ex, _filePath);
                ReplaceRuns(Merge(_runs).Take(MaxRuns).ToArray());
                if (_unsavedRuns.Count > MaxRuns)
                {
                    _unsavedRuns.RemoveRange(MaxRuns, _unsavedRuns.Count - MaxRuns);
                }
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClearAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await AcquireStorageLockAsync();
            await Task.Run(() =>
            {
                // Remove the temporary copy first so a failure leaves the main history available.
                File.Delete(_filePath + ".tmp");
                File.Delete(_filePath);
            });
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
            _fileLock.Release();
        }
    }

    private async Task LoadCoreAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            using var storageLock = await AcquireStorageLockAsync();
            var runs = await ReadMergedAsync();
            ReplaceRuns(runs.Take(MaxRuns).ToArray());
            LogLoaded(_runs.Count, _filePath);
        }
        catch (Exception ex) when (IsStorageFailure(ex))
        {
            LogLoadFailed(ex, _filePath);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<FileStream> AcquireStorageLockAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Keep the sidecar in place: deleting it would let another process lock a different file.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return new FileStream(_filePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
        }
    }

    private async Task<BatchRunRecord[]> ReadMergedAsync() => Merge(await Task.Run(Read)).ToArray();

    private IEnumerable<BatchRunRecord> Merge(IEnumerable<BatchRunRecord> saved) =>
        _unsavedRuns.Concat(saved).DistinctBy(run => run.Id).OrderByDescending(run => run.RecordedAt);

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

    private IReadOnlyList<BatchRunRecord> Read()
    {
        string json;
        try
        {
            json = File.ReadAllText(_filePath);
        }
        catch (FileNotFoundException)
        {
            return Array.Empty<BatchRunRecord>();
        }

        var document = JsonSerializer.Deserialize<BatchHistoryDocument>(json, SerializerOptions);
        if (document is null || document.Version != FormatVersion)
        {
            LogUnsupportedFile(document?.Version, _filePath);
            throw new InvalidDataException("Unsupported batch history format.");
        }

        return (document.Runs ?? Array.Empty<BatchRunRecord>())
            .Where(run => run?.Files is not null)
            .Select(run => run with { Files = run.Files.Where(file => file?.Path is not null).Select(Normalize).ToArray() })
            .ToArray();
    }

    private void Write(BatchRunRecord[] runs)
    {
        // Write next to the file first, so a crash during the write cannot leave a truncated history.
        var temporaryPath = _filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new BatchHistoryDocument(FormatVersion, runs), SerializerOptions));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    /// <summary>
    /// Replaces missing lists from a hand-edited or damaged file with empty ones.
    /// </summary>
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
    private sealed record BatchHistoryDocument(int Version, IReadOnlyList<BatchRunRecord>? Runs);
}
