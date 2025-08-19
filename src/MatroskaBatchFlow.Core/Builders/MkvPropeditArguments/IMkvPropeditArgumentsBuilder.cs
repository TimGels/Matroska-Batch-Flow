using MatroskaBatchFlow.Core.Builders.MkvPropeditArguments.TrackOptions;

namespace MatroskaBatchFlow.Core.Builders.MkvPropeditArguments;
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
    /// Adds a track statistics tags argument to the command.
    /// </summary>
    /// <returns></returns>
    IMkvPropeditArgumentsBuilder WithAddTrackStatisticsTags();

    /// <summary>
    /// Adds a delete track statistics tags argument to the command.
    /// </summary>
    /// <returns></returns>
    IMkvPropeditArgumentsBuilder WithDeleteTrackStatisticsTags();

    /// <summary>
    /// Adds a track configuration argument.
    /// </summary>
    IMkvPropeditArgumentsBuilder AddTrack(Func<ITrackOptionsBuilder, ITrackOptionsBuilder> func);

    /// <summary>
    /// Builds and returns an array of command-line arguments based on the provided fluent interface configuration.
    /// options.
    /// </summary>
    string[] Build();

    /// <summary>
    /// Determines whether the current instance has no properties set, including no tracks.
    /// </summary>
    /// <returns><see langword="true"/> if no properties are set, including tracks; otherwise, <see langword="false"/>.</returns>
    bool IsEmpty();
}
