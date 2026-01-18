using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Default implementation for running external processes and aggregating output.
/// </summary>
public sealed partial class ProcessRunner(ILogger<ProcessRunner> logger) : IProcessRunner
{

    /// <inheritdoc />
    public async Task<ProcessExecutionResult> RunAsync(ProcessStartInfo startInfo, Func<string, bool>? isWarning = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        LogProcessStarting(startInfo.FileName);
        isWarning ??= static _ => false; // Default: no warnings.

        var stdout = new List<string>();
        var stderr = new List<string>();
        var warnings = new List<string>();

        // Ensure encodings (caller may omit).
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.StandardOutputEncoding ??= Encoding.UTF8;
        startInfo.StandardErrorEncoding ??= Encoding.UTF8;

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        // Handler for standard output data.
        void OnOutput(object? _, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            stdout.Add(e.Data);
            if (isWarning(e.Data))
            {
                warnings.Add(e.Data);
            }
        }

        // Handler for standard error data.
        void OnError(object? _, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            stderr.Add(e.Data);
            if (isWarning(e.Data))
            {
                warnings.Add(e.Data);
            }
        }

        process.OutputDataReceived += OnOutput;
        process.ErrorDataReceived += OnError;

        // Start the process.
        if (!process.Start())
        {
            LogProcessStartFailed(startInfo.FileName);
            throw new InvalidOperationException($"Failed to start process '{startInfo.FileName}'.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Ensure process is killed if cancellation is requested.
        using (ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch { } // Ignore exceptions from Kill.
        }))
        {
            // Wait for process to exit or cancellation.
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }

        var exitCode = process.HasExited ? process.ExitCode : -1;
        
        if (ct.IsCancellationRequested)
        {
            LogProcessCanceled(startInfo.FileName);
        }
        else
        {
            LogProcessExited(startInfo.FileName, exitCode);
        }

        return new ProcessExecutionResult(
            ExitCode: exitCode,
            StdOut: string.Join(Environment.NewLine, stdout),
            StdErr: string.Join(Environment.NewLine, stderr),
            Warnings: warnings,
            Canceled: ct.IsCancellationRequested);
    }
}
