using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Tracks which track types are available in a specific file.
/// </summary>
public sealed class FileTrackAvailability
{
    /// <summary>
    /// Path to the file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this file has any audio tracks.
    /// </summary>
    public bool HasAudioTracks { get; set; }

    /// <summary>
    /// Whether this file has any video tracks.
    /// </summary>
    public bool HasVideoTracks { get; set; }

    /// <summary>
    /// Whether this file has any subtitle tracks.
    /// </summary>
    public bool HasSubtitleTracks { get; set; }

    /// <summary>
    /// Number of audio tracks in this file.
    /// </summary>
    public int AudioTrackCount { get; set; }

    /// <summary>
    /// Number of video tracks in this file.
    /// </summary>
    public int VideoTrackCount { get; set; }

    /// <summary>
    /// Number of subtitle tracks in this file.
    /// </summary>
    public int SubtitleTrackCount { get; set; }

    /// <summary>
    /// Checks if a specific track exists in this file.
    /// </summary>
    /// <param name="trackType">Type of track to check.</param>
    /// <param name="trackIndex">Zero-based index of the track.</param>
    /// <returns>True if the track exists, false otherwise.</returns>
    public bool HasTrack(TrackType trackType, int trackIndex)
    {
        return trackType switch
        {
            TrackType.Audio => trackIndex < AudioTrackCount,
            TrackType.Video => trackIndex < VideoTrackCount,
            TrackType.Text => trackIndex < SubtitleTrackCount,
            _ => false
        };
    }
}
