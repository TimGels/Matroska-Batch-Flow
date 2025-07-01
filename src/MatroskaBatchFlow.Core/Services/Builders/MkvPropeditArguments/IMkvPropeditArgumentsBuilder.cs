using MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments;
public interface IMkvPropeditArgumentsBuilder
{
    /// <summary>
    /// Sets the input file to be processed.
    /// </summary>
    IMkvPropeditArgumentsBuilder SetInputFile(string filePath);

    /// <summary>
    /// Sets the title metadata for the Matroska segment (file).
    /// </summary>
    IMkvPropeditArgumentsBuilder WithTitle(string title);

    /// <summary>
    /// Adds a track configuration argument.
    /// </summary>
    IMkvPropeditArgumentsBuilder AddTrack(Func<ITrackOptionsBuilder, ITrackOptionsBuilder> func);

    /// <summary>
    /// Builds and returns an array of command-line arguments based on the provided fluent interface configuration.
    /// options.
    /// </summary>
    string[] Build();
}
