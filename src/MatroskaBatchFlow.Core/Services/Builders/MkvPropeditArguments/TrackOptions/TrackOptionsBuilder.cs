using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;
internal class TrackOptionsBuilder : ITrackOptionsBuilder
{
    private readonly TrackOptions _trackOptions = new();

    /// <inheritdoc />
    /// <param name="trackId">The id to assign to the track. Must be a 
    /// non-negative integer.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder SetTrackId(int trackId)
    {
        _trackOptions.TrackId = trackId;
        return this;
    }

    /// <inheritdoc />
    /// <param name="trackType">The type of track to set. This value determines 
    /// the behavior and characteristics of the track.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder SetTrackType(TrackType trackType)
    {
        _trackOptions.TrackType = trackType;
        return this;
    }

    /// <inheritdoc />
    /// <param name="language"> The language code to assign to the track options. 
    /// If <see langword="null"/>, the language will not be set.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder WithLanguage(string? language = null)
    {
        _trackOptions.Language = language;
        return this;
    }

    /// <inheritdoc />
    /// <param name="name">The name to assign to the track options. 
    /// If <see langword="null"/>, the name will not be set.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/>.</returns>
    public ITrackOptionsBuilder WithName(string? name = null)
    {
        _trackOptions.Name = name;
        return this;
    }

    /// <inheritdoc />
    /// <remarks> Set if that track (<see cref="TrackType.Audio"/>, <see cref="TrackType.Video"/> 
    /// or <see cref="TrackType.Text"/>) is eligible for automatic selection by the player. See 
    /// Matroska specifications for more details.</remarks>
    /// <param name="isDefault"> The default value is <see langword="false"/>.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder WithIsDefault(bool isDefault = false)
    {
        _trackOptions.IsDefault = isDefault;
        return this;
    }

    /// <inheritdoc />
    /// <remarks> Applies only to <see cref="TrackType.Text"/>. Set if that track is eligible for 
    /// automatic selection by the player if it matches the user's language preference, even if the 
    /// user's preferences would normally not enable subtitles with the selected audio track; this can 
    /// be used for tracks containing only translations of foreign-language audio or onscreen text.
    /// See Matroska specifications for more details.
    /// </remarks>
    /// <param name="isForced">The default value is <see langword="false"/>.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder WithIsForced(bool isForced = false)
    {
        _trackOptions.IsForced = isForced;
        return this;
    }

    /// <inheritdoc />
    /// <remarks>Set to <see langword="true"/> if the track is usable. See Matroska specification 
    /// for more details.</remarks>
    /// <param name="isEnabled">A value indicating whether tracking is usable. The default is 
    /// <see langword="true"/>.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder WithIsEnabled(bool isEnabled = true)
    {
        _trackOptions.IsEnabled = isEnabled;
        return this;
    }

    /// <inheritdoc />
    /// <remarks>Set to <see langword="true"/> if and only if that track contains commentary. 
    /// See Matroska specification for more details.</remarks>
    /// <param name="isCommentary"><see langword="true"/> to mark that the track contains 
    /// commentary; otherwise, <see langword="false"/>.</param>
    /// <returns><see cref="ITrackOptionsBuilder"/></returns>
    public ITrackOptionsBuilder WithIsCommentary(bool isCommentary = false)
    {
        _trackOptions.IsCommentary = isCommentary;
        return this;
    }


    /// <inheritdoc />
    /// <remarks>This method constructs a list of arguments related to a track in the Matroska 
    /// segment (file) for mkvpropedit, incorporating options provided. The resulting array can be 
    /// used to execute the tool with the desired configuration.</remarks>
    /// <returns>An array of strings representing the command-line arguments for editing a track.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the track ID or track type is not specified 
    /// in the track options.</exception>
    public string[] Build()
    {
        if (_trackOptions.TrackId is null)
            throw new InvalidOperationException("Track ID must be specified.");
        if (_trackOptions.TrackType is null)
            throw new InvalidOperationException("Track type must be specified.");

        var t = _trackOptions;
        var args = new List<string>();
        // e.g., "track:v1" for video track 1.
        string selector = $"track:{t.TrackType.Value.GetMatroskaTrackTypePrefix()}{t.TrackId}";

        // Begin editing this track.
        args.Add("--edit");
        args.Add(selector);

        void AddSet(string key, string value)
        {
            args.Add("--set");
            args.Add($"{key}={value}");
        }

        if (t.Language is not null)
            AddSet("language", t.Language);

        if (t.Name is not null)
            AddSet("name", t.Name);

        if (t.IsDefault.HasValue)
            AddSet("flag-default", t.IsDefault.Value ? "1" : "0");

        if (t.IsForced.HasValue)
            AddSet("flag-forced", t.IsForced.Value ? "1" : "0");

        if (t.IsEnabled.HasValue)
            AddSet("flag-enabled", t.IsEnabled.Value ? "1" : "0");

        return [.. args];
    }
}
