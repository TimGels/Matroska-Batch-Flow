namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Represents the information of a scanned media file including its file path.
/// </summary>
public sealed record ScannedFileInfo
{
    /// <summary>
    /// Stable identifier to correlate with processing results.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Path of the scanned file.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Resulting details from the MediaInfo scan.
    /// </summary>
    public MediaInfoResult Result { get; init; }
}
