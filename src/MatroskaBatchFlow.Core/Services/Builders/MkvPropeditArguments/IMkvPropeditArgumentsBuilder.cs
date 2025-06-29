using MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments;
public interface IMkvPropeditArgumentsBuilder
{
    IMkvPropeditArgumentsBuilder SetInputFile(string filePath);
    IMkvPropeditArgumentsBuilder WithTitle(string title);
    IMkvPropeditArgumentsBuilder AddTrack(Func<ITrackOptionsBuilder, ITrackOptionsBuilder> func);
    string[] Build();
}
