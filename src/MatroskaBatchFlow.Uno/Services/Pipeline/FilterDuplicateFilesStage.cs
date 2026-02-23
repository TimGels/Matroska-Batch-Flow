using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Services.Pipeline;

/// <summary>
/// Pipeline stage that filters duplicate files from the input set and notifies the user about any duplicates found.
/// Reads and overwrites <see cref="PipelineContextKeys.InputFiles"/> in the context.
/// </summary>
public sealed partial class FilterDuplicateFilesStage(
    IBatchConfiguration batchConfig,
    IPlatformService platformService,
    ILogger<FilterDuplicateFilesStage> logger) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Checking for duplicates\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => true;

    /// <inheritdoc />
    public bool ShowsOverlay => false;

    /// <inheritdoc />
    public Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        if (!context.TryGet<FileInfo[]>(PipelineContextKeys.InputFiles, out var files) || files.Length == 0)
            return Task.CompletedTask;

        var comparer = platformService.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        // Build lookup of existing paths with appropriate comparer for O(1) lookups.
        var existingPaths = new HashSet<string>(batchConfig.FileList.Select(f => f.Path), comparer);

        var seenPaths = new HashSet<string>(comparer);
        var duplicates = new List<string>();
        var uniqueFiles = new List<FileInfo>();

        foreach (var file in files)
        {
            var normalizedPath = file.FullName;

            if (existingPaths.Contains(normalizedPath))
            {
                duplicates.Add(normalizedPath);
                continue;
            }

            if (!seenPaths.Add(normalizedPath))
            {
                duplicates.Add(normalizedPath);
            }
            else
            {
                uniqueFiles.Add(file);
            }
        }

        if (duplicates.Count > 0)
        {
            LogDuplicatesSkipped(duplicates.Count);

            var duplicateFileNames = string.Join(Environment.NewLine, duplicates.Select(p => Path.GetFileName(p) ?? p));

            var message = duplicates.Count == 1
                ? $"This file is already in the list:{Environment.NewLine}{duplicateFileNames}"
                : $"These {duplicates.Count} files are already in the list:{Environment.NewLine}{duplicateFileNames}";

            WeakReferenceMessenger.Default.Send(new DialogMessage("Duplicate Files", message));
        }

        // Overwrite context with filtered files
        context.Set(PipelineContextKeys.InputFiles, uniqueFiles.ToArray());

        return Task.CompletedTask;
    }
}
