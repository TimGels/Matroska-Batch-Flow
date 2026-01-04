using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;

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
    /// Audio track configurations for this file.
    /// </summary>
    public ObservableCollection<TrackConfiguration> AudioTracks { get; set; } = [];

    /// <summary>
    /// Video track configurations for this file.
    /// </summary>
    public ObservableCollection<TrackConfiguration> VideoTracks { get; set; } = [];

    /// <summary>
    /// Subtitle track configurations for this file.
    /// </summary>
    public ObservableCollection<TrackConfiguration> SubtitleTracks { get; set; } = [];

    /// <summary>
    /// Gets the track list for a specific track type.
    /// </summary>
    /// <param name="trackType">The track type to retrieve.</param>
    /// <returns>Observable collection of track configurations for the specified type.</returns>
    public ObservableCollection<TrackConfiguration> GetTrackListForType(TrackType trackType)
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
