namespace MatroskaBatchFlow.Uno.Utilities;

/// <summary>
/// Coordinates access to a file that several app instances can read and write.
/// </summary>
internal static class FileLock
{
    private const int MaxAttempts = 20;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Opens the <c>.lock</c> file next to <paramref name="filePath"/> exclusively, waiting briefly while another
    /// instance holds it.
    /// </summary>
    /// <param name="filePath">The path of the file to protect.</param>
    /// <returns>The open lock file. Dispose it to release the lock.</returns>
    /// <exception cref="IOException">Another instance kept the lock for too long.</exception>
    public static async Task<FileStream> AcquireAsync(string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

        // Keep the lock file in place: deleting it would let another process lock a different file.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return new FileStream(filePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < MaxAttempts)
            {
                await Task.Delay(RetryDelay);
            }
        }
    }
}
