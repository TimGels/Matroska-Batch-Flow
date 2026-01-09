using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Creates track configurations from scanned track information.
/// </summary>
/// <remarks>
/// This factory is responsible for converting raw MediaInfo track data
/// into <see cref="TrackConfiguration"/> objects with properly resolved
/// language codes and initialized properties.
/// </remarks>
public interface ITrackConfigurationFactory
{
    /// <summary>
    /// Creates a track configuration from scanned track information.
    /// </summary>
    /// <param name="scannedTrackInfo">The scanned track information from MediaInfo.</param>
    /// <param name="trackType">The type of track (Audio, Video, Text).</param>
    /// <param name="index">The zero-based index of the track.</param>
    /// <returns>A <see cref="TrackConfiguration"/> with properties initialized from the scanned track.</returns>
    TrackConfiguration Create(
        MediaInfoResult.MediaInfo.TrackInfo scannedTrackInfo,
        TrackType trackType,
        int index);
}
