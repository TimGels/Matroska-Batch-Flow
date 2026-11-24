using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Utilities;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Provides read/write access to a JSON-backed settings file for a specified type.
/// Allows thread-safe updates and persistence of settings.
/// </summary>
/// <remarks>
/// Several app instances can share the file: each update only writes the values it changes, under a lock shared
/// with the other instances, so an instance never restores settings that another instance changed in the meantime.
/// </remarks>
/// <typeparam name="T">The settings type. Must be a class with a parameterless constructor.</typeparam>
public partial class WritableJsonSettings<T> : IWritableSettings<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly ILogger<WritableJsonSettings<T>> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Is modified in UpdateAsync method.")]
    private T _value;
    private JsonObject? _pendingChangesBaseline;

    /// <summary>
    /// Initializes a new instance of <see cref="WritableJsonSettings{T}"/> using the specified file path.
    /// If no file path is provided, a default path is used based on the type name in the local application data folder.
    /// </summary>
    /// <param name="logger">The logger instance for recording settings operations.</param>
    /// <param name="filePath">
    /// The path to the JSON settings file. If null, defaults to <c>{LocalAppData}/{TypeName}.json</c>.
    /// </param>
    public WritableJsonSettings(ILogger<WritableJsonSettings<T>> logger, string? filePath = null)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(filePath) || !Path.IsPathRooted(filePath))
        {
            _filePath = Path.Combine(AppPathHelper.GetLocalAppDataFolder(), $"{typeof(T).Name}.json");
        }
        else
        {
            _filePath = filePath;
        }

        LogInitializingSettings(_filePath);
        _value = Load();
    }

    /// <summary>
    /// Gets the current settings value.
    /// </summary>
    public T Value => _value;

    /// <summary>
    /// Gets the file path to the JSON settings file.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Updates the current value by applying the specified changes and persists the updated value asynchronously.
    /// </summary>
    /// <remarks>
    /// Only the values that the changes modify are written over the saved file, so values saved by another app
    /// instance are kept. A missing or unreadable file is replaced by the current value. If persistence fails,
    /// the change is retained for the next update attempt.
    /// <code>
    /// await userSettingsOptions.UpdateAsync(settings =>
    /// {
    ///     settings.property = newValue;
    /// });
    /// </code>
    /// </remarks>
    /// <param name="applyChanges">
    /// A delegate that defines the changes to apply to the current value. The delegate is invoked with the current value as its parameter.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation.
    /// </returns>
    public async Task UpdateAsync(Action<T> applyChanges)
    {
        await _lock.WaitAsync();
        try
        {
            var before = JsonSerializer.SerializeToNode(_value, SerializerOptions)!.AsObject();
            applyChanges(_value);
            var after = JsonSerializer.SerializeToNode(_value, SerializerOptions)!.AsObject();

            try
            {
                using var fileLock = await FileLock.AcquireAsync(_filePath);
                var saved = await ReadSavedObjectAsync();
                if (saved is null)
                {
                    await WriteAsync(after);
                }
                else
                {
                    CopyChanges(saved, _pendingChangesBaseline ?? before, after);
                    await WriteAsync(saved);
                }

                _pendingChangesBaseline = null;
            }
            catch
            {
                _pendingChangesBaseline ??= before;
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T> ReadFromDiskAsync()
    {
        await _lock.WaitAsync();
        try
        {
            using var fileLock = await FileLock.AcquireAsync(_filePath);
            string json;
            try
            {
                json = await File.ReadAllTextAsync(_filePath);
            }
            catch (FileNotFoundException)
            {
                return new T();
            }

            return JsonSerializer.Deserialize<T>(json, SerializerOptions)
                ?? throw new JsonException($"Settings file '{_filePath}' contains no settings object.");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads the settings from the JSON file, or returns a new instance if the file does not exist or is invalid.
    /// </summary>
    /// <returns>The loaded settings instance.</returns>
    private T Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                LogSettingsFileNotFound(_filePath);
                return new T();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<T>(json, SerializerOptions);

            if (settings is null)
            {
                LogDeserializationFailed(_filePath);
                return new T();
            }

            LogSettingsLoaded(_filePath);
            return settings;
        }
        catch (Exception ex)
        {
            LogSettingsLoadFailed(ex, _filePath);
            return new T();
        }
    }

    /// <summary>
    /// Reads the saved settings as a JSON object.
    /// </summary>
    /// <returns>The saved settings, or <see langword="null"/> when the file is missing or does not contain valid settings.</returns>
    private async Task<JsonObject?> ReadSavedObjectAsync()
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(_filePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }

        JsonObject? saved;
        try
        {
            saved = JsonNode.Parse(json) as JsonObject;
            if (saved?.Deserialize<T>(SerializerOptions) is null)
            {
                saved = null;
            }
        }
        catch (JsonException)
        {
            saved = null;
        }

        if (saved is null)
        {
            LogReplacingUnreadableFile(_filePath);
        }

        return saved;
    }

    /// <summary>
    /// Copies the values that differ between <paramref name="before"/> and <paramref name="after"/> into <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The saved settings to update.</param>
    /// <param name="before">The cached settings before the update.</param>
    /// <param name="after">The cached settings after the update.</param>
    private static void CopyChanges(JsonObject target, JsonObject before, JsonObject after)
    {
        foreach (var (name, value) in after)
        {
            var previous = before[name];
            if (JsonNode.DeepEquals(previous, value))
                continue;

            if (value is JsonObject changed && previous is JsonObject original && target[name] is JsonObject saved)
            {
                CopyChanges(saved, original, changed);
            }
            else
            {
                target[name] = value?.DeepClone();
            }
        }
    }

    /// <summary>
    /// Writes the settings next to the file first and then replaces it, so a crash cannot leave a truncated file.
    /// </summary>
    /// <param name="settings">The settings to write.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous write operation.</returns>
    private async Task WriteAsync(JsonObject settings)
    {
        var temporaryPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, settings.ToJsonString(SerializerOptions));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
