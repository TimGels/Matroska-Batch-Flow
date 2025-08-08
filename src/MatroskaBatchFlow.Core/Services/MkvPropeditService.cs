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
    /// Executes the <c>mkvpropedit</c> command-line tool with the specified arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments to be passed to <c>mkvpropedit</c>.</param>
    /// <returns>A task containing <see cref="MkvPropeditResult"/>, representing the status code and message output from the command execution.</returns>
    public async Task<MkvPropeditResult> ExecuteAsync(string arguments)
    {
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

        if (string.IsNullOrWhiteSpace(arguments))
        {
            return new MkvPropeditResult(
                Status: MkvPropeditStatus.Error,
                Output: "Arguments cannot be null or empty."
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

        process.WaitForExit();

        return new MkvPropeditResult(
            Status: MkvPropeditStatusHelper.FromExitCode(process.ExitCode),
            Output: output
        );
    }
}
