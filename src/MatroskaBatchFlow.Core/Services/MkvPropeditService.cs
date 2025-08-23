using System.Diagnostics;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Utilities;
using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Service for executing the <c>mkvpropedit</c> command-line tool.
/// </summary>
/// <param name="optionsMonitor">The options monitor for application configuration settings.</param>
public class MkvPropeditService(IOptions<AppConfigOptions> optionsMonitor) : IMkvPropeditService
{
    /// <summary>
    /// Executes the <c>mkvpropedit</c> command-line tool with the specified commandList.
    /// </summary>
    /// <param name="arguments">The command-line commandList to be passed to <c>mkvpropedit</c>. 
    /// Cannot be <see langword="null"/>.</param>
    /// <returns>A task containing <see cref="MkvPropeditResult"/>, representing the status code 
    /// and message output from the command execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public async Task<MkvPropeditResult> ExecuteAsync(string arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (string.IsNullOrWhiteSpace(arguments))
        {
            return new MkvPropeditResult(
                Status: MkvPropeditStatus.Error,
                Output: "Arguments cannot be empty."
            );
        }

        // In case annotation validation fails, we return an error result.
        // It throws when a validation failed property is accessed.
        try
        {
            _ = optionsMonitor?.Value?.MkvPropeditPath;
        }
        catch (Exception)
        {
            return new MkvPropeditResult(
                Status: MkvPropeditStatus.Error,
                Output: "Encountered an error while accessing MkvPropedit path configuration."
            );
        }

        if (string.IsNullOrWhiteSpace(optionsMonitor?.Value?.MkvPropeditPath))
        {
            return new MkvPropeditResult(
                Status: MkvPropeditStatus.Error,
                Output: "MkvPropedit path is not configured."
            );
        }

        string? exePath = ExecutableLocator.FindExecutable(optionsMonitor.Value.MkvPropeditPath);

        if (exePath is null)
        {
            return new MkvPropeditResult(
                Status: MkvPropeditStatus.Error,
                Output: $"Executable not found: {optionsMonitor.Value.MkvPropeditPath}"
            );
        }

        var startInfo = new ProcessStartInfo()
        {
            FileName = exePath,
            Arguments = $"{arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception)
        {
            return new MkvPropeditResult(
                Status: MkvPropeditStatus.Error,
                Output: $"Failed to start process: {exePath}"
            );
        }

        string output = await process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new MkvPropeditResult(
            Status: MkvPropeditStatusHelper.FromExitCode(process.ExitCode),
            Output: output
        );
    }

    /// <summary>
    /// Executes a sequence of MKVPropEdit commands asynchronously and yields the results as they are processed.
    /// </summary>
    /// <param name="commands">A collection of command strings to be executed. Each command represents an individual operation to process.</param>
    /// <returns>An asynchronous stream of <see cref="MkvPropeditResult"/> objects, where each result represents the outcome of
    /// processing a command.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="commands"/> collection is null.</exception>
    public async IAsyncEnumerable<MkvPropeditResult> ExecuteAsync(IEnumerable<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (!commands.Any())
        {
            yield return new MkvPropeditResult(
                    Status: MkvPropeditStatus.Warning,
                    Output: "No commands to process."
                );
        }

        foreach (var command in commands)
        {
            MkvPropeditResult result;

            try
            {
                result = await ExecuteAsync(command).ConfigureAwait(false);
            }
            catch (Exception ex) // Catch any unexpected exceptions during execution to ensure the loop continues.
            {
                result = new MkvPropeditResult(
                    Status: MkvPropeditStatus.Error,
                    Output: $"Error occurred while processing command '{command}': {ex.Message}"
                );
            }

            yield return result;
        }
    }
}
