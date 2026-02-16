namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Service that manages ongoing validation state for the current batch of files.
/// Re-validates when the file list changes or when explicitly triggered (e.g., after validation settings change).
/// </summary>
public interface IValidationStateService : IDisposable
{
    /// <summary>
    /// Gets the latest validation results from the most recent validation run.
    /// </summary>
    IReadOnlyList<FileValidationResult> CurrentResults { get; }

    /// <summary>
    /// Gets whether any current validation results have blocking errors (severity = Error).
    /// </summary>
    bool HasBlockingErrors { get; }

    /// <summary>
    /// Gets whether any current validation results have warnings.
    /// </summary>
    bool HasWarnings { get; }

    /// <summary>
    /// Gets whether any validation results exist from the last validation run.
    /// </summary>
    bool HasResults { get; }

    /// <summary>
    /// Occurs after each re-validation, providing the updated validation state.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Forces a re-validation of all files in the current batch against the current validation settings.
    /// </summary>
    void Revalidate();
}
