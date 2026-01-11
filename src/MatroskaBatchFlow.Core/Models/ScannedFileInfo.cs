using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Represents the information of a scanned media file including its file path.
/// </summary>
public sealed record ScannedFileInfo
{
    private string? _path;

    /// <summary>
    /// Stable identifier to correlate with processing results.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Path of the scanned file. Automatically normalized to fully-qualified path.
    /// </summary>
    public string Path
    {
        get => _path ?? string.Empty;
        init => _path = string.IsNullOrWhiteSpace(value) ? value : System.IO.Path.GetFullPath(value);
    }

    /// <summary>
    /// Resulting details from the MediaInfo scan.
    /// </summary>
    public MediaInfoResult Result { get; }

    /// <summary>
    /// Number of audio tracks in this file. Pre-computed for performance.
    /// </summary>
    public int AudioTrackCount { get; }

    /// <summary>
    /// Number of video tracks in this file. Pre-computed for performance.
    /// </summary>
    public int VideoTrackCount { get; }

    /// <summary>
    /// Number of subtitle tracks in this file. Pre-computed for performance.
    /// </summary>
    public int SubtitleTrackCount { get; }

    /// <summary>
    /// Initializes a new instance with auto-computed track counts.
    /// </summary>
    /// <param name="result">The MediaInfo scan result.</param>
    /// <param name="path">The file path (will be normalized to full path).</param>
    public ScannedFileInfo(MediaInfoResult result, string path)
    {
        Result = result;
        Path = path;

        var tracks = result?.Media?.Track ?? [];
        AudioTrackCount = tracks.Count(t => t.Type == TrackType.Audio);
        VideoTrackCount = tracks.Count(t => t.Type == TrackType.Video);
        SubtitleTrackCount = tracks.Count(t => t.Type == TrackType.Text);
    }

    /// <summary>
    /// Checks if a specific track exists in this file.
    /// </summary>
    /// <param name="trackType">Type of track to check.</param>
    /// <param name="trackIndex">Zero-based index of the track.</param>
    /// <returns>True if the track exists, false otherwise.</returns>
    public bool HasTrack(TrackType trackType, int trackIndex)
    {
        if (trackIndex < 0) return false;

        return trackType switch
        {
            TrackType.Audio => trackIndex < AudioTrackCount,
            TrackType.Video => trackIndex < VideoTrackCount,
            TrackType.Text => trackIndex < SubtitleTrackCount,
            _ => false
        };
    }
}
