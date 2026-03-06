using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Stores per-file track configuration.
/// </summary>
public sealed class FileTrackConfiguration
{
    /// <summary>
    /// Path to the file this configuration applies to.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Audio track values for this file.
    /// </summary>
    public ObservableCollection<FileTrackValues> AudioTracks { get; set; } = [];

    /// <summary>
    /// Video track values for this file.
    /// </summary>
    public ObservableCollection<FileTrackValues> VideoTracks { get; set; } = [];

    /// <summary>
    /// Subtitle track values for this file.
    /// </summary>
    public ObservableCollection<FileTrackValues> SubtitleTracks { get; set; } = [];

    /// <summary>
    /// Gets the track list for a specific track type.
    /// </summary>
    /// <param name="trackType">The track type to retrieve.</param>
    /// <returns>Observable collection of track values for the specified type.</returns>
    public ObservableCollection<FileTrackValues> GetTrackListForType(TrackType trackType)
    {
        return trackType switch
        {
            TrackType.Audio => AudioTracks,
            TrackType.Video => VideoTracks,
            TrackType.Text => SubtitleTracks,
            _ => []
        };
    }
}
