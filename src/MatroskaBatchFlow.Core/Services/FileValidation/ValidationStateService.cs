using MatroskaBatchFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Manages ongoing validation state for the current batch of files.
/// Automatically re-validates when the file list changes, and exposes a <see cref="Revalidate"/>
/// method for explicit triggers (e.g., after validation settings change).
/// </summary>
public sealed partial class ValidationStateService : IValidationStateService
{
    private readonly IFileValidationEngine _validationEngine;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IValidationSettingsService _validationSettingsService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly ILogger<ValidationStateService> _logger;

    private IReadOnlyList<FileValidationResult> _currentResults = [];
    private bool _hasBlockingErrors;
    private bool _hasWarnings;
    private bool _disposed;

    /// <inheritdoc/>
    public IReadOnlyList<FileValidationResult> CurrentResults => _currentResults;

    /// <inheritdoc/>
    public bool HasBlockingErrors => _hasBlockingErrors;

    /// <inheritdoc/>
    public bool HasWarnings => _hasWarnings;

    /// <inheritdoc/>
    public bool HasResults => _currentResults.Count > 0;

    /// <inheritdoc/>
    public event EventHandler? StateChanged;

    public ValidationStateService(
        IFileValidationEngine validationEngine,
        IBatchConfiguration batchConfiguration,
        IValidationSettingsService validationSettingsService,
        IWritableSettings<UserSettings> userSettings,
        ILogger<ValidationStateService> logger)
    {
        _validationEngine = validationEngine;
        _batchConfiguration = batchConfiguration;
        _validationSettingsService = validationSettingsService;
        _userSettings = userSettings;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public void Revalidate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var files = _batchConfiguration.FileList;

        if (files.Count == 0)
        {
            LogValidationSkipped();
            UpdateState([]); // Always clear results when there are no files, to reset any previous state
            return;
        }

        var settings = _validationSettingsService.GetEffectiveSettings(_userSettings.Value);
        List<FileValidationResult> results = [.. _validationEngine.Validate(files, settings)];

        UpdateState(results);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public async Task RevalidateAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Snapshot both collections on the calling (UI) thread before going off-thread,
        // since ObservableCollection and the settings graph are not thread-safe.
        List<ScannedFileInfo> fileSnapshot = [.. _batchConfiguration.FileList];

        if (fileSnapshot.Count == 0)
        {
            LogValidationSkipped();
            UpdateState([]); // Always clear results when there are no files, to reset any previous state
            return;
        }

        var settings = _validationSettingsService.GetEffectiveSettings(_userSettings.Value);

        // Run the CPU-bound validation work off the UI thread.
        // The continuation (and therefore UpdateState + StateChanged) resumes on the
        // original synchronization context, keeping UI-bound subscribers safe.
        List<FileValidationResult> results = await Task.Run(
            () => _validationEngine.Validate(fileSnapshot, settings).ToList());

        UpdateState(results);
    }

    /// <summary>
    /// Updates the internal validation state based on the specified collection of file validation results.
    /// </summary>
    /// <param name="results">A read-only list of <see cref="FileValidationResult"/> objects representing the latest validation results to
    /// process. Cannot be null.</param>
    private void UpdateState(IReadOnlyList<FileValidationResult> results)
    {
        _currentResults = results;

        var hasBlockingErrors = false;
        var hasWarnings = false;
        var errorCount = 0;
        var warningCount = 0;
        var infoCount = 0;

        foreach (var result in results)
        {
            if (result.IsBlocking)
            {
                hasBlockingErrors = true;
            }

            if (result.IsWarning)
            {
                hasWarnings = true;
                warningCount++;
            }

            if (result.IsError)
            {
                errorCount++;
            }

            if (result.IsInfo)
            {
                infoCount++;
            }
        }

        _hasBlockingErrors = hasBlockingErrors;
        _hasWarnings = hasWarnings;

        LogValidationCompleted(results.Count, errorCount, warningCount, infoCount);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Disposes of the resources used by the <see cref="ValidationStateService"/> instance.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }
}
