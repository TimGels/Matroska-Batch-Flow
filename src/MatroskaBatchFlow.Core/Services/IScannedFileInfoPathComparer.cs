namespace MatroskaBatchFlow.Core.Services;

using MatroskaBatchFlow.Core.Models;

/// <summary>
/// Defines a comparer for <see cref="ScannedFileInfo"/> instances based on their file paths.
/// </summary>
public interface IScannedFileInfoPathComparer : IEqualityComparer<ScannedFileInfo>
{
}
