namespace MatroskaBatchFlow.Uno.Services;
public static class StorageItemConverter
{
    /// <summary>
    /// Converts an <see cref="IStorageItem"/> object to a <see cref="FileInfo"/> object.
    /// </summary>
    /// <param name="storageItem"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static FileInfo ToFileInfo(this IStorageItem storageItem)
    {
        ArgumentNullException.ThrowIfNull(storageItem);

        if (string.IsNullOrWhiteSpace(storageItem.Path))
            throw new InvalidOperationException("Storage item has no valid path");

        return new FileInfo(storageItem.Path);
    }

    /// <summary>
    /// Converts an array of <see cref="IStorageItem"/> objects to an array of <see cref="FileInfo"/> objects.
    /// </summary>
    /// <param name="storageItems"></param>
    /// <returns></returns>
    public static FileInfo[] ToFileInfo(this IStorageItem[] storageItems)
    {
        ArgumentNullException.ThrowIfNull(storageItems);
        return storageItems.Select(item => item.ToFileInfo()).ToArray();
    }

    /// <summary>
    /// Converts a <see cref="FileInfo"/> object to an <see cref="IStorageItem"/> asynchronously.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static async Task<IStorageFile> ToStorageFileAsync(this FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        if (!fileInfo.Exists)
            throw new FileNotFoundException("File does not exist", fileInfo.FullName);

        return await StorageFile.GetFileFromPathAsync(fileInfo.FullName);
    }

    /// <summary>
    /// Converts an array of <see cref="FileInfo"/> objects to an array of <see cref="IStorageItem"/> objects asynchronously.
    /// </summary>
    /// <param name="fileInfos"></param>
    /// <returns></returns>
    public static async Task<IStorageFile[]> ToStorageFileInfoAsync(this FileInfo[] fileInfos)
    {
        ArgumentNullException.ThrowIfNull(fileInfos);
        var tasks = fileInfos.Select(fileInfo => fileInfo.ToStorageFileAsync());
        return await Task.WhenAll(tasks);
    }
}
