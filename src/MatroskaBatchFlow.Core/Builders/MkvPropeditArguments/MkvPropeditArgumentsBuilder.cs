using MatroskaBatchFlow.Core.Builders.MkvPropeditArguments.TrackOptions;

namespace MatroskaBatchFlow.Core.Builders.MkvPropeditArguments;
public class MkvPropeditArgumentsBuilder : IMkvPropeditArgumentsBuilder
{
    private readonly List<TrackOptionsBuilder> _trackOptionsBuilders = [];
    private string? _inputFile;
    private string? _title;

    /// <inheritdoc />
    /// <param name="filePath">The full path to the input file. This must be a valid, non-null, and non-empty string representing the file
    /// path.</param>
    /// <returns><see cref="IMkvPropeditArgumentsBuilder"/></returns>
    public IMkvPropeditArgumentsBuilder SetInputFile(string filePath)
    {
        _inputFile = filePath;
        return this;
    }

    /// <inheritdoc />
    /// <param name="title">The title to assign to the Matroska file. Cannot be null.</param>
    /// <returns><see cref="IMkvPropeditArgumentsBuilder"/></returns>
    public IMkvPropeditArgumentsBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <inheritdoc />
    /// <param name="func">A function that configures the track options. The function receives an <see cref="ITrackOptionsBuilder"/>
    /// instance to define the track's properties and returns the configured <see cref="ITrackOptionsBuilder"/>.</param>
    /// <returns>The current instance of <see cref="IMkvPropeditArgumentsBuilder"/>, allowing for method chaining.</returns>
    public IMkvPropeditArgumentsBuilder AddTrack(Func<ITrackOptionsBuilder, ITrackOptionsBuilder> func)
    {
        var trackOptionsBuilder = new TrackOptionsBuilder();
        func(trackOptionsBuilder);
        _trackOptionsBuilders.Add(trackOptionsBuilder);
        return this;
    }

    /// <inheritdoc />
    /// <remarks>This method constructs a list of arguments for mkvpropedit, incorporating options provided. 
    /// The resulting array can be used to execute the tool with the desired configuration.</remarks>
    /// <returns>An array of strings representing the command-line arguments.
    /// <exception cref="InvalidOperationException">Thrown if the input file is not specified or is null or empty.</exception>
    public string[] Build()
    {
        if (string.IsNullOrEmpty(_inputFile))
            throw new InvalidOperationException("Target file (input) must be specified.");

        var args = new List<string>
        {
            $"\"{_inputFile}\""
        };

        // Add the segment title if specified.
        if (_title is not null)
        {
            args.Add($"--edit");
            args.Add("info");
            args.Add($"--set");
            args.Add($"title=\"{_title}\"");
        }

        // Add each track's options.
        foreach (var tob in _trackOptionsBuilders)
            args.AddRange(tob.Build());

        return [.. args];
    }
}
