using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Keeps the results of finished batches in a JSON file in the local application data folder.
/// </summary>
public partial class BatchHistoryService : IBatchHistoryService
{
    /// <summary>
    /// The number of runs that are kept; older runs are dropped.
    /// </summary>
    public const int MaxRuns = 20;

    /// <summary>
    /// The version of the file format. A file with another version is ignored.
    /// </summary>
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ObservableCollection<BatchRunRecord> _runs = [];
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;
    private readonly ILogger<BatchHistoryService> _logger;
    private Task? _loadTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchHistoryService"/> class.
    /// </summary>
    /// <param name="filePath">The path of the history file.</param>
    /// <param name="logger">The logger.</param>
    public BatchHistoryService(string filePath, ILogger<BatchHistoryService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
        _logger = logger;
        Runs = new ReadOnlyObservableCollection<BatchRunRecord>(_runs);
    }

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<BatchRunRecord> Runs { get; }

    /// <inheritdoc/>
    public Task LoadAsync() => _loadTask ??= LoadCoreAsync();

    /// <inheritdoc/>
    public async Task AddAsync(BatchRunRecord run)
    {
        ArgumentNullException.ThrowIfNull(run);

        _runs.Insert(0, run);
        while (_runs.Count > MaxRuns)
        {
            _runs.RemoveAt(_runs.Count - 1);
        }

        // Saving before the saved runs are loaded would overwrite them.
        await LoadAsync();
        await SaveAsync();
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        await LoadAsync();
        _runs.Clear();
        await SaveAsync();
    }

    private async Task LoadCoreAsync()
    {
        IReadOnlyList<BatchRunRecord> saved;
        try
        {
            saved = await Task.Run(Read);
        }
        catch (Exception ex)
        {
            LogLoadFailed(ex, _filePath);
            return;
        }

        foreach (var run in saved.OrderByDescending(run => run.RecordedAt))
        {
            if (_runs.Count >= MaxRuns)
                break;

            if (_runs.Any(existing => existing.Id == run.Id))
                continue;

            _runs.Add(run);
        }

        LogLoaded(_runs.Count, _filePath);
    }

    /// <summary>
    /// Writes the current runs to disk, or removes the file when there are none.
    /// </summary>
    /// <remarks>
    /// The runs are read after the lock is taken, so the last save always writes the latest history.
    /// </remarks>
    private async Task SaveAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var runs = _runs.ToArray();
            await Task.Run(() => Write(runs));
        }
        catch (Exception ex)
        {
            LogSaveFailed(ex, _filePath);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private IReadOnlyList<BatchRunRecord> Read()
    {
        if (!File.Exists(_filePath))
            return Array.Empty<BatchRunRecord>();

        var document = JsonSerializer.Deserialize<BatchHistoryDocument>(File.ReadAllText(_filePath), SerializerOptions);
        if (document is null || document.Version != FormatVersion)
        {
            LogUnsupportedFile(document?.Version, _filePath);
            return Array.Empty<BatchRunRecord>();
        }

        return (document.Runs ?? Array.Empty<BatchRunRecord>())
            .Where(run => run?.Files is not null)
            .Select(run => run with { Files = run.Files.Where(file => file?.Path is not null).Select(Normalize).ToArray() })
            .ToArray();
    }

    private void Write(BatchRunRecord[] runs)
    {
        if (runs.Length == 0)
        {
            File.Delete(_filePath);
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
