using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Core.Services.FileValidation;

namespace MatroskaBatchFlow.Core.Services.Pipeline;

/// <summary>
/// Pipeline stage that triggers a full revalidation of the current batch.
/// </summary>
public sealed class ValidateStage(IValidationStateService validationStateService) : IPipelineStage
{
    /// <inheritdoc />
    public string DisplayName => "Validating files\u2026";

    /// <inheritdoc />
    public bool IsIndeterminate => true;

    /// <inheritdoc />
    public bool ShowsOverlay => true;

    /// <inheritdoc />
    public async Task ExecuteAsync(PipelineContext context, IProgress<(int current, int total)>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await validationStateService.RevalidateAsync();
    }
}
