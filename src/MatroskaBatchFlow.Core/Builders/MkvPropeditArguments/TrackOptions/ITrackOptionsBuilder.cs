using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Builders.MkvPropeditArguments.TrackOptions;
public interface ITrackOptionsBuilder
{
    /// <summary>
    /// Sets the unique identifier for the track. This also represents the track 
    /// number in the Matroska file.
    /// </summary>
    ITrackOptionsBuilder SetTrackId(int trackId);

    /// <summary>
    /// Sets the type of track to be used for the current track options.
    /// </summary>
    ITrackOptionsBuilder SetTrackType(TrackType trackType);

    /// <summary>
    /// Sets the language for the track options.
    /// </summary>
    ITrackOptionsBuilder WithLanguage(string language);

    /// <summary>
    /// Sets the name for the track options.
    /// </summary>
    ITrackOptionsBuilder WithName(string name);

    /// <summary>
    /// Sets the Matroska <c>FlagDefault</c> element.
    /// </summary>
    ITrackOptionsBuilder WithIsDefault(bool isDefault);

    /// <summary>
    /// Sets the Matroska <c>FlagForced</c> element.
    /// </summary>
    ITrackOptionsBuilder WithIsForced(bool isForced);

    /// <summary>
    /// Sets the Matroska <c>FlagEnabled</c> element.
    /// </summary>
    ITrackOptionsBuilder WithIsEnabled(bool isEnabled);

    /// <summary>
    /// Sets the Matroska <c>FlagCommentary</c> element.
    /// </summary>
    ITrackOptionsBuilder WithIsCommentary(bool isCommentary);

    /// <summary>
    /// Builds and returns an array of command-line arguments based on the specified track options.
    /// </summary>
    string[] Build();
}
