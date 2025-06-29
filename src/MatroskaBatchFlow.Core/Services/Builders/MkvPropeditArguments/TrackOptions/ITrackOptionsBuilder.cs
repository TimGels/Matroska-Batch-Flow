using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;
public interface ITrackOptionsBuilder
{
    ITrackOptionsBuilder SetTrackId(int trackId);
    ITrackOptionsBuilder SetTrackType(TrackType trackType);
    ITrackOptionsBuilder WithLanguage(string language);
    ITrackOptionsBuilder WithName(string name);
    ITrackOptionsBuilder WithIsDefault(bool isDefault);
    ITrackOptionsBuilder WithIsForced(bool isForced);
    ITrackOptionsBuilder WithIsEnabled(bool isEnabled);
    ITrackOptionsBuilder WithIsCommentary(bool isCommentary);
    string[] Build();
}
