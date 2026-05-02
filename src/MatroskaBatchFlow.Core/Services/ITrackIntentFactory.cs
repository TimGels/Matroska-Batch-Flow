using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Creates track intents from scanned track information.
/// </summary>
/// <remarks>
/// This factory is responsible for converting raw MediaInfo track data
/// into <see cref="TrackIntent"/> objects with properly resolved
/// language codes and initialized property configs.
/// </remarks>
public interface ITrackIntentFactory
{
    /// <summary>
    /// Creates a track intent from scanned track information.
    /// </summary>
    /// <param name="scannedTrackInfo">The scanned track information from MediaInfo.</param>
    /// <param name="trackType">The type of track (Audio, Video, Text).</param>
    /// <param name="index">The zero-based index of the track.</param>
    /// <returns>A <see cref="TrackIntent"/> with property configs initialized from the scanned track.</returns>
    TrackIntent Create(
        MediaInfoResult.MediaInfo.TrackInfo scannedTrackInfo,
        TrackType trackType,
        int index);
}