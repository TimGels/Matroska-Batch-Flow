using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;
internal class TrackOptionsBuilder : ITrackOptionsBuilder
{
    private readonly TrackOptions _trackOptions = new();
    public ITrackOptionsBuilder SetTrackId(int trackId)
    {
        _trackOptions.TrackId = trackId;
        return this;
    }
    public ITrackOptionsBuilder SetTrackType(TrackType trackType)
    {
        _trackOptions.TrackType = trackType;
        return this;
    }
    public ITrackOptionsBuilder WithLanguage(string language)
    {
        _trackOptions.Language = language;
        return this;
    }
    public ITrackOptionsBuilder WithName(string name)
    {
        _trackOptions.Name = name;
        return this;
    }
    public ITrackOptionsBuilder WithIsDefault(bool isDefault)
    {
        _trackOptions.IsDefault = isDefault;
        return this;
    }
    public ITrackOptionsBuilder WithIsForced(bool isForced)
    {
        _trackOptions.IsForced = isForced;
        return this;
    }
    public ITrackOptionsBuilder WithIsEnabled(bool isEnabled)
    {
        _trackOptions.IsEnabled = isEnabled;
        return this;
    }
    public ITrackOptionsBuilder WithIsCommentary(bool isCommentary)
    {
        _trackOptions.IsCommentary = isCommentary;
        return this;
    }

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
