using System.Diagnostics;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Orchestrates the processing of scannedFiles using mkvpropedit based on batch configurations.
/// </summary>
public class FileProcessingOrchestrator : IFileProcessingOrchestrator
{
    private readonly IBatchConfiguration _batchConfig;
    private readonly IBatchReportStore _batchReportStore;
    private readonly IMkvToolExecutor _mkvToolExecutor;
    private readonly IMkvPropeditArgumentsGenerator _mkvPropeditArgumentsGenerator;

    public FileProcessingOrchestrator(
        IBatchConfiguration batchConfig,
        IBatchReportStore batchReportStore,
        IMkvToolExecutor mkvToolExecutor,
        IMkvPropeditArgumentsGenerator mkvPropeditArgumentsGenerator)
    {
        _batchConfig = batchConfig;
        _batchReportStore = batchReportStore;
        _mkvToolExecutor = mkvToolExecutor;
        _mkvPropeditArgumentsGenerator = mkvPropeditArgumentsGenerator;
    }

    /// <summary>
    /// Enrolls the specified scannedFiles into the active batch report, creating processing reports for each.
    /// </summary>
    /// <param name="scannedFiles">The scannedFiles to enroll.</param>
    /// <returns>A list of created <see cref="FileProcessingReport"/> instances.</returns>
    private List<FileProcessingReport> EnrollFiles(IEnumerable<ScannedFileInfo> scannedFiles)
    {
        var batchReport = _batchReportStore.ActiveBatch;
        var created = new List<FileProcessingReport>();

        foreach (var scannedFile in scannedFiles)
        {
            var report = new FileProcessingReport
            {
                SourceFile = scannedFile,
                Path = scannedFile.Path,
                Status = ProcessingStatus.Pending
            };

            if (batchReport.TryAddFileReport(report))
            {
                created.Add(report);
            }
        }

        return created;
    }

    /// <inheritdoc/>
    public async Task<FileProcessingReport> ProcessFileAsync(FileProcessingReport fileReport, CancellationToken ct = default)
    {
        // If already running, return existing report.
        if (fileReport.Status == ProcessingStatus.Running)
            return fileReport;

        if (ct.IsCancellationRequested)
        {
            fileReport.Status = ProcessingStatus.Canceled;
            fileReport.Notes = "Processing canceled before start.";
            return fileReport;
        }

        fileReport.Status = ProcessingStatus.Running;
        fileReport.StartedAt = DateTimeOffset.Now;

        var executionTimer = Stopwatch.StartNew();
        try
        {
            ct.ThrowIfCancellationRequested();

            // Build arguments for this specific scannedFile.
            var argString = _mkvPropeditArgumentsGenerator.BuildFileArgumentString(fileReport.SourceFile, _batchConfig);

            // No modifications requested => skip processing.
            if (string.IsNullOrWhiteSpace(argString))
            {
                fileReport.Status = ProcessingStatus.Skipped;
                fileReport.Notes = "No modifications requested for this scannedFile.";

                return fileReport;
            }

            // Execute mkvpropedit with the constructed arguments.
            MkvPropeditResult propeditResult = await _mkvToolExecutor.ExecuteAsync(argString, ct);
            fileReport.ExecutedCommand = propeditResult.SimulatedCommandLine;

            if (propeditResult.IsFatal)
            {
                var combined = Combine(propeditResult.StandardError, propeditResult.StandardOutput);
                fileReport.AddError(string.IsNullOrWhiteSpace(combined)
                    ? $"mkvpropedit failed (exit code {(int)propeditResult.Status})."
                    : combined);
            }
            else if (propeditResult.Status == MkvPropeditStatus.Warning)
            {
                fileReport.AddWarnings(propeditResult.Warnings);
            }

            if (fileReport.Status != ProcessingStatus.Failed)
            {
                fileReport.Status = fileReport.Warnings.Count > 0
                    ? ProcessingStatus.SucceededWithWarnings
                    : ProcessingStatus.Succeeded;
            }
        }
        catch (OperationCanceledException)
        {
            fileReport.Status = ProcessingStatus.Canceled;
            fileReport.Notes = "Processing canceled.";
        }
        catch (Exception ex)
        {
            fileReport.AddError(ex.Message);
        }
        finally
        {
            executionTimer.Stop();
            fileReport.FinishedAt = DateTimeOffset.Now;
            fileReport.Duration = executionTimer.Elapsed;
        }

        return fileReport;
    }

    /// <inheritdoc/>
    public async Task<List<FileProcessingReport>> ProcessAllAsync(IEnumerable<ScannedFileInfo> scannedFiles, CancellationToken ct = default)
    {
        List<FileProcessingReport> reports = EnrollFiles(scannedFiles);

        foreach (var report in reports)
        {
            await ProcessFileAsync(report, ct);       
        }

        return reports;
    }

    /// <summary>
    /// Combines the specified standard error and standard output strings into a single string, separated by a newline,
    /// while omitting any null, empty, or whitespace-only values.
    /// </summary>
    /// <param name="stderr">The standard error output string, or <see langword="null"/> if not available.</param>
    /// <param name="stdout">The standard output string, or <see langword="null"/> if not available.</param>
    /// <returns>A combined string of non-empty outputs, or an empty string if both inputs are null or whitespace.</returns>
    private static string Combine(string? stderr, string? stdout)
    {
        List<string> parts = new(capacity: 2);

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            parts.Add(stderr!);
        }

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            parts.Add(stdout!);
        }

        return parts.Count == 0 ? string.Empty : string.Join(Environment.NewLine, parts);
    }
}
