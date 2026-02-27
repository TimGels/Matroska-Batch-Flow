using System.Diagnostics.CodeAnalysis;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Shared data bag passed between pipeline stages.
/// Stages write outputs for downstream stages to consume.
/// </summary>
/// <remarks>
/// This is a per-run object, create a new instance for each pipeline execution.
/// This class is not thread-safe. The <see cref="IPipelineRunner"/> executes stages sequentially,
/// so concurrent access is not expected.
/// </remarks>
public sealed class PipelineContext
{
    private readonly Dictionary<string, object> _data = [];

    /// <summary>
    /// When set to <see langword="true"/>, signals the <see cref="IPipelineRunner"/>
    /// to stop executing subsequent stages. A stage sets this when continuing
    /// the pipeline would be pointless (e.g., all input was filtered out).
    /// </summary>
    public bool IsAborted { get; set; }

    /// <summary>
    /// Stores a value in the context under the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The context key.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string key, T value) where T : notnull
        => _data[key] = value;

    /// <summary>
    /// Retrieves a value from the context by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The context key.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the context.</exception>
    /// <exception cref="InvalidCastException">Thrown when the stored value cannot be cast to <typeparamref name="T"/>.</exception>
    public T Get<T>(string key)
        => (T)_data[key];

    /// <summary>
    /// Attempts to retrieve a value from the context by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The context key.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found and the value is of the expected type; otherwise, <see langword="false"/>.</returns>
    public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        if (_data.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}
