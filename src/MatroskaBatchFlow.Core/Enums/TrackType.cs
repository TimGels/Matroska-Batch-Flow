namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Represents the type of track in a media file.
/// TrackType enum values based on the StreamKind types from MediaInfo.
/// </summary>
public enum TrackType
{

    /// <summary>
    /// Represents the "Info" element in the Matroska specification.
    /// </summary>
    General,
    /// <summary>
    /// Representa the "Track" element in the Matroska specification. 
    /// </summary>
    Video,
    /// <summary>
    /// Representa the "Track" element in the Matroska specification. 
    /// </summary>
    Audio,
    /// <summary>
    /// Representa the "Track" element in the Matroska specification. 
    /// </summary>
    Text,
    Other,
    Image,
    Menu
}

/// <summary>
/// Provides extension methods for the <see cref="TrackType"/> enum.
/// </summary>
public static class TrackTypeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="TrackType"/> represents a Matroska 
    /// <see href="https://www.matroska.org/technical/elements.html#Tracks">track element</see>.
    /// </summary>
    /// <param name="trackType">The <see cref="TrackType"/> to evaluate.</param>
    /// <returns><see langword="true"/> if the <paramref name="trackType"/> is an actual Matroska 
    /// Track element; otherwise, <see langword="false"/>.</returns>
    public static bool IsMatroskaTrackElement(this TrackType trackType)
    {
        return trackType switch
        {
            TrackType.Audio or TrackType.Video or TrackType.Text => true,
            _ => false
        };
    }
}
