namespace MatroskaBatchFlow.Core.Enums;

/// <summary>
/// Represents the type of track in a media file.
/// TrackType enum values based on the StreamKind types from MediaInfo.
/// </summary>
public enum TrackType
{
    General,
    Video,
    Audio,
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
    /// Determines if the track type is editable in the application.
    /// </summary>
    /// <param name="trackType"></param>
    /// <returns> True if the track type is editable; otherwise, false.</returns>
    public static bool IsEditable(this TrackType trackType)
    {
        return trackType switch
        {
            TrackType.Audio or TrackType.Video or TrackType.Text => true,
            _ => false
        };
    }
}
