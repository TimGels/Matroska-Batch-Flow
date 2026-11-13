using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// Minimum time the processing overlay stays visible, preventing a flash for batches that finish almost instantly.
    /// </summary>
    private static readonly TimeSpan MinOverlayDuration = TimeSpan.FromMilliseconds(500);

    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IBatchReportStore _batchReportStore;
    private readonly IFileProcessingOrchestrator _orchestrator;
    private readonly IMkvPropeditArgumentsGenerator _mkvPropeditArgumentsBuilder;
    private readonly IValidationStateService _validationStateService;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly IScannedFileInfoPathComparer _pathComparer;
    private readonly ITaskbarProgressService _taskbarProgress;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource _processingCts = new();

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProcessBatch))]
    private bool hasFiles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProcessBatch))]
    private bool hasConfiguredChanges;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProcessBatch))]
    private bool hasNoBlockingErrors = true;

    /// <summary>
    /// Gets whether the batch can be processed. Requires files to be present, at least one
    /// configured change, and no blocking validation errors.
    /// </summary>
    public bool CanProcessBatch => HasFiles && HasConfiguredChanges && HasNoBlockingErrors;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInteractionEnabled))]
    private bool isProcessing;

    /// <summary>
    /// Gets whether cancellation of the running batch was requested.
    /// </summary>
    [ObservableProperty]
    private bool isCancelling;

    /// <summary>
    /// Gets the state of the overlay shown while the batch is being processed.
    /// </summary>
    [ObservableProperty]
    private OperationOverlayState processingOverlay = OperationOverlayState.Inactive;

    /// <summary>
    /// Gets whether the navigation and commands can be used. They are disabled while the batch is being processed,
    /// because the arguments for each file are built from the live configuration when that file is processed.
    /// </summary>
    public bool IsInteractionEnabled => !IsProcessing;

    [ObservableProperty]
    private BatchExecutionReport? batchReport; // expose current batch summary to UI

    /// <summary>
    /// Gets a value indicating whether the log viewer tab should be visible.
    /// </summary>
    public bool EnableLoggingView => _uiPreferences.EnableLoggingView;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }
    public ICommand GenerateMkvpropeditArgumentsCommand { get; }
    public ICommand ProcessBatchCommand { get; }
    public IRelayCommand CancelProcessingCommand { get; }

    public MainViewModel(
        IBatchConfiguration batchConfiguration,
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IFileProcessingOrchestrator orchestrator,
        IBatchReportStore batchResultStore,
        IMkvPropeditArgumentsGenerator argumentsService,
        IValidationStateService validationStateService,
        IUIPreferencesService uiPreferences,
        IScannedFileInfoPathComparer pathComparer,
        ITaskbarProgressService taskbarProgress,
        ILogger<MainViewModel> logger)
    {
        _batchConfiguration = batchConfiguration;
        _orchestrator = orchestrator;
        _batchReportStore = batchResultStore;
        _mkvPropeditArgumentsBuilder = argumentsService;
        _validationStateService = validationStateService;
        _uiPreferences = uiPreferences;
        _pathComparer = pathComparer;
        _taskbarProgress = taskbarProgress;
        _logger = logger;

        NavigationService = navigationService;
        NavigationViewService = navigationViewService;

        GenerateMkvpropeditArgumentsCommand = new RelayCommand(GenerateMkvpropeditArgumentsHandler);
        ProcessBatchCommand = new AsyncRelayCommand(ProcessBatchAsync);
        CancelProcessingCommand = new RelayCommand(CancelProcessing, CanCancelProcessing);

        NavigationService.Navigated += OnNavigated;
        _batchConfiguration.StateChanged += BatchConfigurationOnStateChangedHandler;
        _validationStateService.StateChanged += OnValidationStateChanged;
        _uiPreferences.PropertyChanged += OnUIPreferencesChanged;

        BatchReport = _batchReportStore.ActiveBatch;

        // Initialize state properties based on current batch/validation state
        OnValidationStateChanged(this, EventArgs.Empty);
        BatchConfigurationOnStateChangedHandler(this, EventArgs.Empty);
    }

    private void OnUIPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IUIPreferencesService.EnableLoggingView))
        {
            OnPropertyChanged(nameof(EnableLoggingView));
        }
    }

    /// <summary>
    /// Requests cancellation of the running batch. Files that are already being written are finished first.
    /// </summary>
    private void CancelProcessing()
    {
        if (!CanCancelProcessing())
            return;

        LogBatchCancellationRequested();
        IsCancelling = true;
        _taskbarProgress.SetPaused();

        if (BatchReport is not null)
            UpdateProcessingProgress(BatchReport);

        _processingCts.Cancel();
    }

    private bool CanCancelProcessing() => IsProcessing && !IsCancelling;

    partial void OnIsProcessingChanged(bool value) => CancelProcessingCommand.NotifyCanExecuteChanged();

    partial void OnIsCancellingChanged(bool value) => CancelProcessingCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Processes the batch of files based on the current batch configuration, asynchronously.
    /// </summary>
    /// <remarks>
    /// Must be started on the UI thread: progress and status changes of the batch report are raised on the captured
    /// context and drive the processing overlay and taskbar progress.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task ProcessBatchAsync()
    {
        if (_batchConfiguration.FileList.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(
                new DialogMessage("Info", "FileInfo list is empty. Please add files to process."));
            return;
        }

        // Build preview commands via central arguments service.
        var commands = _mkvPropeditArgumentsBuilder.BuildBatchArguments(_batchConfiguration);
        var (isValid, errorMessage) = ValidateMkvpropeditArguments(commands);
        if (!isValid)
        {
            LogBatchProcessingAborted(errorMessage);
            WeakReferenceMessenger.Default.Send(new DialogMessage("Error", errorMessage));
            return;
        }

        _batchReportStore.Reset();

        var batch = _batchReportStore.CreateBatch();
        _batchReportStore.SetActiveBatch(batch);
        BatchReport = batch;

        _processingCts.Dispose();
        _processingCts = new CancellationTokenSource();

        IsCancelling = false;
        IsProcessing = true;
        batch.PropertyChanged += OnProcessingBatchPropertyChanged;
        UpdateProcessingProgress(batch);
        var overlayStartedTicks = Stopwatch.GetTimestamp();

        DialogMessage outcome;
        try
        {
            await _orchestrator.ProcessAllAsync(_batchConfiguration.FileList, _processingCts.Token);

            MarkProcessedFilesAsStale(batch);
            outcome = SummarizeOutcome(batch);
        }
        catch (OperationCanceledException)
        {
            LogBatchProcessingCancelled();
            outcome = new DialogMessage("Cancelled", "Batch processing was cancelled.");
        }
        catch (Exception ex)
        {
            LogBatchProcessingError(ex);
            outcome = new DialogMessage("Error", $"Unexpected error: {ex.Message}");
        }
        finally
        {
            batch.PropertyChanged -= OnProcessingBatchPropertyChanged;

            var remaining = MinOverlayDuration - Stopwatch.GetElapsedTime(overlayStartedTicks);
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);

            ProcessingOverlay = OperationOverlayState.Inactive;
            _taskbarProgress.Clear();
            IsCancelling = false;
            IsProcessing = false;
        }

        // Shown after the overlay is hidden, so the dialog is not briefly stacked on top of it.
        WeakReferenceMessenger.Default.Send(outcome);
    }

    /// <summary>
    /// Marks successfully processed files as stale, so they are re-scanned before the next validation.
    /// </summary>
    /// <param name="batch">The batch that was processed.</param>
    private void MarkProcessedFilesAsStale(BatchExecutionReport batch)
    {
        var processedFileReports = batch.FileReports.Where(r => r.Status == ProcessingStatus.Succeeded || r.Status == ProcessingStatus.SucceededWithWarnings);

        foreach (var fileReport in processedFileReports)
        {
            var file = _batchConfiguration.FileList.FirstOrDefault(f => _pathComparer.PathEquals(f.Path, fileReport.Path));
            if (file != null)
            {
                _batchConfiguration.MarkFileAsStale(file.Id);
            }
        }
    }

    /// <summary>
    /// Updates the processing overlay and taskbar progress when the progress of the running batch changes.
    /// </summary>
    private void OnProcessingBatchPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not BatchExecutionReport batch)
            return;

        if (e.PropertyName is nameof(BatchExecutionReport.OverallProgress)
            or nameof(BatchExecutionReport.CurrentFile)
            or nameof(BatchExecutionReport.Completed)
            or nameof(BatchExecutionReport.Total))
        {
            UpdateProcessingProgress(batch);
        }
    }

    /// <summary>
    /// Builds the processing overlay state from the batch report and mirrors the overall progress on the taskbar.
    /// </summary>
    /// <param name="batch">The batch that is being processed.</param>
    private void UpdateProcessingProgress(BatchExecutionReport batch)
    {
        var currentFile = batch.CurrentFile;
        var fileNumber = Math.Min(batch.Completed + (currentFile is null ? 0 : 1), batch.Total);

        var detail = string.Empty;
        if (currentFile is not null)
        {
            var fileName = System.IO.Path.GetFileName(currentFile.Path);
            detail = currentFile.Progress is int percent ? $"{fileName} ({percent}%)" : fileName;
        }

        ProcessingOverlay = new OperationOverlayState(
            IsCancelling ? "Cancelling…" : "Processing files…",
            IsIndeterminate: false,
            Current: fileNumber,
            Total: batch.Total,
            BlocksInput: true)
        {
            Detail = detail,
            Progress = batch.OverallProgress,
            IsCancelling = IsCancelling
        };

        _taskbarProgress.SetProgress(batch.OverallProgress);
    }

    /// <summary>
    /// Builds the dialog that summarizes the outcome of the batch processing.
    /// </summary>
    /// <param name="report">The batch that was processed.</param>
    /// <returns>The dialog message to show.</returns>
    private static DialogMessage SummarizeOutcome(BatchExecutionReport report)
    {
        var failures = report.FileReports
            .Where(r => r.Status == ProcessingStatus.Failed)
            .Select(r => $"{r.Path} => {string.Join("; ", r.Errors)}")
            .ToList();

        if (report.Canceled > 0)
        {
            var message = $"Processing was cancelled. {report.Canceled} of {report.Total} files were not processed.";
            if (failures.Count > 0)
            {
                message += $"{Environment.NewLine}{Environment.NewLine}Failed:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}";
            }

            return new DialogMessage("Batch Cancelled", message);
        }

        if (failures.Count > 0)
        {
            return new DialogMessage("Batch Completed with Errors", string.Join(Environment.NewLine, failures));
        }

        if (report.Warnings > 0)
        {
            var warns = report.FileReports
                .Where(r => r.Status == ProcessingStatus.SucceededWithWarnings)
                .Select(r => $"{r.Path} => {string.Join("; ", r.Warnings)}");

            return new DialogMessage("Batch Completed with Warnings", string.Join(Environment.NewLine, warns));
        }

        return new DialogMessage("Batch Completed", "All files processed successfully.");
    }

    /// <summary>
    /// Generates MKVPropEdit command arguments and displays them in a dialog.
    /// </summary>
    private void GenerateMkvpropeditArgumentsHandler()
    {
        var commands = _mkvPropeditArgumentsBuilder.BuildBatchArguments(_batchConfiguration);

        if (commands.All(arg => string.IsNullOrWhiteSpace(arg)))
        {
            WeakReferenceMessenger.Default.Send(
                new DialogMessage("Error", "No valid mkvpropedit commands could be generated."));
            return;
        }

        var argumentsString = string.Join(Environment.NewLine + Environment.NewLine, commands);
        WeakReferenceMessenger.Default.Send(new MkvPropeditArgumentsDialogMessage(argumentsString));
        _batchConfiguration.MkvpropeditArguments = argumentsString;
    }

    private void OnNavigated(object sender, NavigationEventArgs eventArgs)
    {
        IsBackEnabled = NavigationService.CanGoBack;
        if (eventArgs.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;

            return;
        }

        Selected = NavigationViewService.GetSelectedItem(eventArgs.SourcePageType);
    }

    /// <summary>
    /// Handles validation state changes to update file presence and blocking error state.
    /// </summary>
    private void OnValidationStateChanged(object? sender, EventArgs e)
    {
        HasFiles = _batchConfiguration.FileList.Count > 0;
        HasNoBlockingErrors = !_validationStateService.HasBlockingErrors;
    }

    /// <summary>
    /// Handles the <see cref="IBatchConfiguration.StateChanged"/> event to update whether
    /// any configured changes (mkvpropedit arguments) exist.
    /// </summary>
    /// <param name="sender">The source of the event. This can be <see langword="null"/>.</param>
    /// <param name="eventArgs">The event data associated with the state change.</param>
    private void BatchConfigurationOnStateChangedHandler(object? sender, EventArgs eventArgs)
    {
        var commands = _mkvPropeditArgumentsBuilder.BuildBatchArguments(_batchConfiguration);
        (bool isValid, _) = ValidateMkvpropeditArguments(commands);
        HasConfiguredChanges = isValid;
    }

    /// <summary>
    /// Validates the provided MKVPropEdit command arguments to ensure they are valid.
    /// </summary>
    /// <param name="commands">An array of command arguments to validate.</param>
    /// <returns>A tuple where the first value is <see langword="true"/> if all arguments are valid; otherwise, <see
    /// langword="false"/>. The second value contains an error message if validation fails, or an empty string if
    /// validation succeeds.</returns>
    private static (bool, string) ValidateMkvpropeditArguments(string[] commands)
    {
        if (commands is { Length: 0 })
        {
            return (false, "No commands to process.");
        }

        if (commands.All(arg => string.IsNullOrWhiteSpace(arg)))
        {
            return (false, "No valid mkvpropedit commands could be generated.");
        }

        // Valid if at least one file has modifications (non-empty command)
        // Empty strings represent files with no modifications, which is expected behavior
        return (true, string.Empty);
    }
}
