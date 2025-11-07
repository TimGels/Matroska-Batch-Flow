using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Utilities;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Provides read/write access to a JSON-backed settings file for a specified type.
/// Allows thread-safe updates and persistence of settings.
/// </summary>
/// <typeparam name="T">The settings type. Must be a class with a parameterless constructor.</typeparam>
public class WritableJsonSettings<T> : IWritableSettings<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Is modified in UpdateAsync method.")]
    private T _value;

    /// <summary>
    /// Initializes a new instance of <see cref="WritableJsonSettings{T}"/> using the specified file path.
    /// If no file path is provided, a default path is used based on the type name in the local application data folder.
    /// </summary>
    /// <param name="filePath">
    /// The path to the JSON settings file. If null, defaults to <c>{LocalAppData}/{TypeName}.json</c>.
    /// </param>
    public WritableJsonSettings(string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !Path.IsPathRooted(filePath))
        {
            _filePath = Path.Combine(AppPathHelper.GetLocalAppDataFolder(), $"{typeof(T).Name}.json");
        }
        else
        {
            _filePath = filePath;
        }

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
            applyChanges(_value);
            await SaveAsync();
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
                return new T();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<T>(json, SerializerOptions);

            if (settings is null)
            {
                return new T();
            }

            return settings;
        }
        catch (Exception)
        {
            // TODO: Add logging and let the user know about the error.
            return new T();
        }
    }

    /// <summary>
    /// Saves the current settings value to the JSON file asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous save operation.</returns>
    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_value, SerializerOptions);
        var directoryPath = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        await File.WriteAllTextAsync(_filePath, json);
    }
}
