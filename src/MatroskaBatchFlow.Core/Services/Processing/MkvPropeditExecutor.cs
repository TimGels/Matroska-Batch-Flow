using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Utilities;
using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Provides functionality to execute the `mkvpropedit` tool with specified commands and process the results.
/// </summary>
/// <param name="optionsMonitor">The options monitor for application configuration settings.</param>
/// <param name="processRunner">The process runner to execute external processes.</param>
public class MkvPropeditExecutor(IOptions<AppConfigOptions> optionsMonitor, IWritableSettings<UserSettings> userSettings, IProcessRunner processRunner) : IMkvToolExecutor
{
    /// <inheritdoc/>
    public async Task<MkvPropeditResult> ExecuteAsync(string commandLineArguments, CancellationToken ct = default)
    {
        if (!TryResolveExecutable(out var resolvedExe, out var resolveError))
        {
            return new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Unknown,
                StandardOutput = null,
                StandardError = resolveError,
                Warnings = [],
                ResolvedExecutablePath = resolvedExe ?? string.Empty,
                ExecutableArguments = commandLineArguments
            };
        }

        var startInfo = CreateStartInfo(resolvedExe, commandLineArguments);
        try
        {
            ProcessExecutionResult executionResult = await processRunner.RunAsync(startInfo, IsWarning, ct).ConfigureAwait(false);

            return MapToResult(executionResult, resolvedExe, commandLineArguments);
        }
        catch (Exception ex)
        {
            return new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Unknown,
                StandardOutput = null,
                StandardError = ex.Message,
                Warnings = [],
                ResolvedExecutablePath = resolvedExe,
                ExecutableArguments = commandLineArguments
            };
        }
    }

    /// <summary>
    /// Attempts to resolve the full path to the mkvpropedit executable based on the application configuration.
    /// </summary>
    /// <param name="resolvedPath">The resolved executable path if successful; otherwise, <see langword="null"/>.</param>
    /// <param name="error">The error message if resolution fails; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the executable was successfully resolved; otherwise, <see langword="false"/>.</returns>
    private bool TryResolveExecutable([NotNullWhen(true)] out string? resolvedPath, [NotNullWhen(false)] out string? error)
    {
        resolvedPath = null;
        error = null;
        string executablePath;

        // It throws when a validation failed property is accessed.
        try
        {
            if (userSettings.Value.MkvPropedit.IsCustomPathEnabled)
            {
                executablePath = userSettings.Value.MkvPropedit.CustomPath ?? string.Empty;
            }
            else
            {
                executablePath = optionsMonitor.Value.MkvPropeditPath;
            }

            if (ExecutableLocator.FindExecutable(executablePath) is string foundPath)
            {
                resolvedPath = foundPath;
                return true;
            }

            error = $"Executable '{executablePath}' could not be found.";
            return false;
        }
        catch
        {
            error = "MKVPropedit executable path is not configured correctly.";
            return false;
        }
    }

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> configured to run the specified executable with the given arguments.
    /// </summary>
    /// <param name="fileName">The resolvedPath to the executable file.</param>
    /// <param name="args">The command-line arguments to pass to the executable.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/> instance.</returns>
    private static ProcessStartInfo CreateStartInfo(string fileName, string args)
    {
        return new()
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    /// <summary>
    /// Maps a <see cref="ProcessExecutionResult"/> plus invocation context to an <see cref="MkvPropeditResult"/>.
    /// </summary>
    /// <param name="exec">The process execution result. Cannot be null.</param>
    /// <param name="resolvedExe">The resolved executable path used to start the process.</param>
    /// <param name="arguments">The arguments passed to the executable.</param>
    /// <returns>An <see cref="MkvPropeditResult"/> representing the outcome of the execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exec"/> is <see langword="null"/>.</exception>
    private static MkvPropeditResult MapToResult(ProcessExecutionResult exec, string resolvedExe, string arguments)
    {
        ArgumentNullException.ThrowIfNull(exec);

        if (exec.Canceled)
        {
            return new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Unknown,
                StandardOutput = exec.StdOut,
                StandardError = "Canceled.",
                Warnings = exec.Warnings,
                ResolvedExecutablePath = resolvedExe,
                ExecutableArguments = arguments
            };
        }

        return exec.ExitCode switch
        {
            0 => new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Success,
                StandardOutput = exec.StdOut,
                StandardError = null,
                Warnings = exec.Warnings,
                ResolvedExecutablePath = resolvedExe,
                ExecutableArguments = arguments
            },
            1 => new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Warning,
                StandardOutput = exec.StdOut,
                StandardError = exec.StdErr,
                Warnings = exec.Warnings,
                ResolvedExecutablePath = resolvedExe,
                ExecutableArguments = arguments
            },
            2 => new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Error,
                StandardOutput = exec.StdOut,
                StandardError = exec.StdErr,
                Warnings = exec.Warnings,
                ResolvedExecutablePath = resolvedExe,
                ExecutableArguments = arguments
            },
            _ => new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Unknown,
                StandardOutput = exec.StdOut,
                StandardError = "Unexpected mkvpropedit exit code.",
                Warnings = exec.Warnings,
                ResolvedExecutablePath = resolvedExe,
                ExecutableArguments = arguments
            }
        };
    }

    /// <summary>
    /// Determines whether the specified line contains the word "warning" using a case-insensitive comparison.
    /// </summary>
    /// <param name="line">The line of text to evaluate.</param>
    /// <returns><see langword="true"/> if the line contains the word "warning" (case-insensitive); otherwise, <see
    /// langword="false"/>.</returns>
    private static bool IsWarning(string line)
    {
        return line.Contains("warning", StringComparison.OrdinalIgnoreCase);
    }
}
