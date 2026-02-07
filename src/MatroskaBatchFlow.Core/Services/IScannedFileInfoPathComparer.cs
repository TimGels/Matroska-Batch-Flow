namespace MatroskaBatchFlow.Core.Services;

using MatroskaBatchFlow.Core.Models;

/// <summary>
/// Defines a comparer for <see cref="ScannedFileInfo"/> instances based on their file paths.
/// </summary>
public interface IScannedFileInfoPathComparer : IEqualityComparer<ScannedFileInfo>
{
    /// <summary>
    /// Determines whether two file paths are equal using platform-appropriate path comparison.
    /// </summary>
    /// <param name="path1">The first path to compare.</param>
    /// <param name="path2">The second path to compare.</param>
    /// <returns><see langword="true"/> if the paths are equal; otherwise, <see langword="false"/>.</returns>
    bool PathEquals(string? path1, string? path2);
}
