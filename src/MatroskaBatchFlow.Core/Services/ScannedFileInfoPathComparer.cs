namespace MatroskaBatchFlow.Core.Services;

using MatroskaBatchFlow.Core.Models;

/// <summary>
/// Provides a platform-aware comparer for <see cref="ScannedFileInfo"/> instances for equality based on their <see cref="ScannedFileInfo.Path"/> property using the appropriate string
/// comparison for the current operating system.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Windows: Case-insensitive</description></item>
/// <item><description>Other platforms: Case-sensitive</description></item>
/// </list>
/// </remarks>
/// <param name="platformService">The platform service to determine the operating system.</param>
public sealed class ScannedFileInfoPathComparer(IPlatformService platformService) : IScannedFileInfoPathComparer
{
    /// <summary>
    /// Specifies the string comparison method to use for file system paths, based on the current operating system.
    /// </summary>
    /// <remarks> 
    /// Windows uses case-insensitive comparison, while other platforms usually use case-sensitive comparison.
    /// Not perfect, but should work for most scenarios.
    /// </remarks>
    private readonly StringComparison _pathComparison = platformService.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

    /// <inheritdoc/>
    public bool Equals(ScannedFileInfo? x, ScannedFileInfo? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return string.Equals(x.Path, y.Path, _pathComparison);
    }

    /// <inheritdoc/>
    public int GetHashCode(ScannedFileInfo obj)
    {
        return obj?.Path?.GetHashCode(_pathComparison) ?? 0;
    }
}
