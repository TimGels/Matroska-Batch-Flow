namespace MatroskaBatchFlow.Uno.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IStorageItem"/>.
/// </summary>
public static class StorageItemExtensions
{
    /// <summary>
    /// Converts the specified <see cref="IStorageItem"/> to a <see cref="FileInfo"/> object.
    /// </summary>
    /// <param name="storageItem">The storage item to convert. Must not be <see langword="null"/>.</param>
    /// <returns>A <see cref="FileInfo"/> object representing the file at the path specified by the <paramref
    /// name="storageItem"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <paramref name="storageItem"/> has a <see cref="IStorageItem.Path"/> that is <see
    /// langword="null"/>, empty, or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="storageItem"/> is <see langword="null"/>.</exception>
    public static FileInfo ToFileInfo(this IStorageItem storageItem)
    {
        ArgumentNullException.ThrowIfNull(storageItem);

        if (string.IsNullOrWhiteSpace(storageItem.Path))
            throw new InvalidOperationException("Storage item has no valid path");

        return new FileInfo(storageItem.Path);
    }

    /// <summary>
    /// Converts an enumerable of <see cref="IStorageItem"/> objects to an array of <see cref="FileInfo"/> objects.
    /// </summary>
    /// <param name="storageItems">The collection of <see cref="IStorageItem"/> objects to convert.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="storageItems"/> is <see langword="null"/>.</exception>
    public static FileInfo[] ToFileInfo(this IEnumerable<IStorageItem> storageItems)
    {
        ArgumentNullException.ThrowIfNull(storageItems);
        return [.. storageItems.Select(item => item.ToFileInfo())];
    }
}
