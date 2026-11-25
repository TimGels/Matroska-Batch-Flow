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
    private JsonObject? _pendingChanges;

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
    public Task UpdateAsync(Action<T> applyChanges) => UpdateCoreAsync(applyChanges, restoreOnFailure: null);

    /// <summary>
    /// Updates the current value like <see cref="UpdateAsync(Action{T})"/>, but abandons the change if it cannot be
    /// persisted.
    /// </summary>
    /// <remarks>
    /// If persistence fails, <paramref name="restoreOnFailure"/> restores the cached values and the values it restores
    /// are not retried. Both happen before the next update can start, so a queued update cannot persist the abandoned
    /// change.
    /// </remarks>
    /// <param name="applyChanges">A delegate that defines the changes to apply to the current value.</param>
    /// <param name="restoreOnFailure">A delegate that restores the values changed by <paramref name="applyChanges"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task UpdateAsync(Action<T> applyChanges, Action<T> restoreOnFailure) =>
        UpdateCoreAsync(applyChanges, restoreOnFailure);

    private async Task UpdateCoreAsync(Action<T> applyChanges, Action<T>? restoreOnFailure)
    {
        await _lock.WaitAsync();
        try
        {
            var before = JsonSerializer.SerializeToNode(_value, SerializerOptions)!.AsObject();
            applyChanges(_value);
            var after = JsonSerializer.SerializeToNode(_value, SerializerOptions)!.AsObject();

            var pendingChanges = _pendingChanges?.DeepClone().AsObject() ?? new JsonObject();
            CopyChanges(pendingChanges, before, after);

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
                    ApplyChanges(saved, pendingChanges);
                    await WriteAsync(saved);
                }

                _pendingChanges = null;
            }
            catch (Exception ex)
            {
                // Restore before releasing the lock, so a queued update cannot persist the rejected values.
                if (restoreOnFailure is not null)
                {
                    try
                    {
                        RestoreValues(pendingChanges, restoreOnFailure);
                    }
                    catch (Exception restoreException)
                    {
                        throw new AggregateException(ex, restoreException);
                    }
                }

                _pendingChanges = pendingChanges.Count == 0 ? null : pendingChanges;
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Restores cached values after a failed update and removes them from the pending changes.
    /// </summary>
    /// <param name="pendingChanges">The pending changes to update.</param>
    /// <param name="restoreChanges">A delegate that restores the cached values.</param>
    private void RestoreValues(JsonObject pendingChanges, Action<T> restoreChanges)
    {
        var before = JsonSerializer.SerializeToNode(_value, SerializerOptions)!.AsObject();
        restoreChanges(_value);
        var after = JsonSerializer.SerializeToNode(_value, SerializerOptions)!.AsObject();
        RemoveChanges(pendingChanges, before, after);
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
    /// Copies the values changed by the update into the pending changes object.
    /// </summary>
    /// <param name="target">The pending changes to update.</param>
    /// <param name="before">The cached settings before the delegate ran.</param>
    /// <param name="after">The cached settings after the delegate ran.</param>
    private static void CopyChanges(JsonObject target, JsonObject before, JsonObject after)
    {
        foreach (var (name, value) in after)
        {
            var previous = before[name];
            if (JsonNode.DeepEquals(previous, value))
                continue;

            if (value is JsonObject changed && previous is JsonObject original)
            {
                if (target[name] is JsonObject pending)
                {
                    CopyChanges(pending, original, changed);
                }
                else if (target.ContainsKey(name))
                {
                    target[name] = value.DeepClone();
                }
                else
                {
                    var pendingObject = new JsonObject();
                    CopyChanges(pendingObject, original, changed);
                    target[name] = pendingObject;
                }
            }
            else
            {
                target[name] = value?.DeepClone();
            }
        }
    }

    /// <summary>
    /// Removes pending values changed by <paramref name="after"/> from the pending changes object.
    /// </summary>
    /// <param name="target">The pending changes to update.</param>
    /// <param name="before">The cached settings before the rollback.</param>
    /// <param name="after">The cached settings after the rollback.</param>
    private static void RemoveChanges(JsonObject target, JsonObject before, JsonObject after)
    {
        foreach (var (name, value) in after)
        {
            var previous = before[name];
            if (JsonNode.DeepEquals(previous, value))
                continue;

            if (previous is JsonObject original && value is JsonObject changed && target[name] is JsonObject pending)
            {
                RemoveChanges(pending, original, changed);
                if (pending.Count == 0)
                {
                    target.Remove(name);
                }
            }
            else
            {
                target.Remove(name);
            }
        }
    }

    /// <summary>
    /// Applies the pending changed values over the settings read from disk.
    /// </summary>
    /// <param name="target">The saved settings to update.</param>
    /// <param name="changes">The pending changes to apply.</param>
    private static void ApplyChanges(JsonObject target, JsonObject changes)
    {
        foreach (var (name, value) in changes)
        {
            if (value is JsonObject changed)
            {
                if (target[name] is not JsonObject saved)
                {
                    saved = new JsonObject();
                    target[name] = saved;
                }

                ApplyChanges(saved, changed);
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
