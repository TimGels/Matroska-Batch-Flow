using System.Collections.Specialized;
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

        _batchConfiguration.FileList.CollectionChanged += OnFileListChanged;
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>"
    public void Revalidate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var files = _batchConfiguration.FileList;

        if (files.Count == 0)
        {
            LogValidationSkipped();

            if (_currentResults.Count > 0)
            {
                UpdateState([]);
            }

            return;
        }

        var settings = _validationSettingsService.GetEffectiveSettings(_userSettings.Value);
        List<FileValidationResult> results = [.. _validationEngine.Validate(files, settings)];

        UpdateState(results);
    }

    private void OnFileListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        LogFileListChangeTriggered(e.Action.ToString());
        Revalidate();
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
        _batchConfiguration.FileList.CollectionChanged -= OnFileListChanged;
    }
}
