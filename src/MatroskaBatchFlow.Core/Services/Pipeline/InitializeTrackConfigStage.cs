using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services.FileProcessing;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Pipeline stage that initializes per-file track configurations and applies processing rules
/// for all scanned files in the context.
/// </summary>
/// <remarks>
/// Reads <see cref="PipelineContextKeys.ScannedFiles"/> from the context.
/// Reports determinate progress as each file is processed.
/// </remarks>
public sealed class InitializeTrackConfigStage(
    IBatchTrackConfigurationInitializer trackConfigInitializer,
    IFileProcessingEngine fileProcessingRuleEngine,
    IBatchConfiguration batchConfig) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Applying track configuration\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => false;

    /// <inheritdoc />
    public bool ShowsOverlay => true;

    /// <inheritdoc />
    public Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        if (!context.TryGet<List<ScannedFileInfo>>(PipelineContextKeys.ScannedFiles, out var scannedFiles) || scannedFiles.Count == 0)
            return Task.CompletedTask;

        var totalFiles = scannedFiles.Count;

        for (var index = 0; index < totalFiles; index++)
        {
            ct.ThrowIfCancellationRequested();
            var file = scannedFiles[index];

            trackConfigInitializer.Initialize(file, TrackType.Audio, TrackType.Video, TrackType.Text);
            fileProcessingRuleEngine.Apply(file, batchConfig);

            progress?.Report((index + 1, totalFiles));
        }

        return Task.CompletedTask;
    }
}
