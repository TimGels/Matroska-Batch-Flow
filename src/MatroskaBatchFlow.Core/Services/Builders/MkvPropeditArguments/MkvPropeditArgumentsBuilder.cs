using MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments.TrackOptions;

namespace MatroskaBatchFlow.Core.Services.Builders.MkvPropeditArguments;
public class MkvPropeditArgumentsBuilder : IMkvPropeditArgumentsBuilder
{
    private readonly List<TrackOptionsBuilder> _trackOptionsBuilders = [];
    private string? _inputFile;
    private string? _title;
    public IMkvPropeditArgumentsBuilder SetInputFile(string filePath)
    {
        _inputFile = filePath;
        return this;
    }
    public IMkvPropeditArgumentsBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }
    public IMkvPropeditArgumentsBuilder AddTrack(Func<ITrackOptionsBuilder, ITrackOptionsBuilder> func)
    {
        var trackOptionsBuilder = new TrackOptionsBuilder();
        func(trackOptionsBuilder);
        _trackOptionsBuilders.Add(trackOptionsBuilder);
        return this;
    }

    public string[] Build()
    {
        if (string.IsNullOrEmpty(_inputFile))
            throw new InvalidOperationException("Target file (input) must be specified.");

        var args = new List<string>
        {
            $"{_inputFile}"
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
