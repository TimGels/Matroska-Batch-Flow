using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

public interface IMkvPropeditArgumentsGenerator
{
    /// <summary>
    /// Constructs an array of command-line argument strings based on the provided <see cref="IBatchConfiguration"/>.
    /// </summary>
    /// <param name="batchConfiguration">The configuration object containing the list of files and associated settings for building the batch arguments.</param>
    /// <returns>An array of strings, each representing the command-line arguments for a corresponding file in the batch configuration.</returns>
    string[] BuildBatchArguments(IBatchConfiguration batchConfiguration);

    /// <summary>
    /// Constructs a command-line argument token string for a single file based on the provided batch configuration.
    /// </summary>
    /// <param name="file">The file information used to generate the argument string.</param>
    /// <param name="batchConfiguration">The batch configuration that determines how the argument string is constructed.</param>
    /// <returns>A string representing the constructed command-line arguments for the file.</returns>
    string BuildFileArgumentString(ScannedFileInfo file, IBatchConfiguration batchConfiguration);
}
