using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class MainViewModel : ObservableObject
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IBatchReportStore _batchReportStore;
    private readonly IFileProcessingOrchestrator _orchestrator;
    private readonly IMkvPropeditArgumentsGenerator _mkvPropeditArgumentsBuilder;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource _processingCts = new();

    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    [ObservableProperty]
    private bool canProcessBatch;

    [ObservableProperty]
    private bool isProcessing;

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
    public ICommand CancelProcessingCommand { get; }

    public MainViewModel(
        IBatchConfiguration batchConfiguration,
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IFileProcessingOrchestrator orchestrator,
        IBatchReportStore batchResultStore,
        IMkvPropeditArgumentsGenerator argumentsService,
        IUIPreferencesService uiPreferences,
        ILogger<MainViewModel> logger)
    {
        _batchConfiguration = batchConfiguration;
        _orchestrator = orchestrator;
        _batchReportStore = batchResultStore;
        _mkvPropeditArgumentsBuilder = argumentsService;
        _uiPreferences = uiPreferences;
        _logger = logger;

        NavigationService = navigationService;
        NavigationViewService = navigationViewService;

        GenerateMkvpropeditArgumentsCommand = new RelayCommand(GenerateMkvpropeditArgumentsHandler);
        ProcessBatchCommand = new AsyncRelayCommand(ProcessBatchAsync);
        CancelProcessingCommand = new RelayCommand(CancelProcessing);

        NavigationService.Navigated += OnNavigated;
        _batchConfiguration.StateChanged += BatchConfigurationOnStateChangedHandler;
        _uiPreferences.PropertyChanged += OnUIPreferencesChanged;

        BatchReport = _batchReportStore.ActiveBatch;
    }

    private void OnUIPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IUIPreferencesService.EnableLoggingView))
        {
            OnPropertyChanged(nameof(EnableLoggingView));
        }
    }

    /// <summary>
    /// Cancels the ongoing batch processing operation.
    /// </summary>
    private void CancelProcessing()
    {
        if (!IsProcessing)
            return;

        _processingCts.Cancel();
    }

    /// <summary>
    /// Processes the batch of files based on the current batch configuration, asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task ProcessBatchAsync()
    {
        if (_batchConfiguration.FileList.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(
                new DialogMessage("Info", "FileInfo list is empty. Please add files to process."));
            return;
        }

        IsProcessing = true;
        _processingCts = new CancellationTokenSource();

        try
        {
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

            var batchExecutionReport = _batchReportStore.CreateBatch();
            _batchReportStore.SetActiveBatch(batchExecutionReport);

            await _orchestrator.ProcessAllAsync(_batchConfiguration.FileList, _processingCts.Token);

            // Mark successfully processed files as stale (needs re-scanning before next validation)
            var report = _batchReportStore.ActiveBatch;
            var processedFileReports = report.FileReports.Where(r => r.Status == ProcessingStatus.Succeeded || r.Status == ProcessingStatus.SucceededWithWarnings);

            foreach (var fileReport in processedFileReports)
            {
                var file = _batchConfiguration.FileList.FirstOrDefault(f => f.Path == fileReport.Path);
                if (file != null)
                {
                    _batchConfiguration.MarkFileAsStale(file.Id);
                }
            }

            SummarizeOutcome();
        }
        catch (OperationCanceledException)
        {
            LogBatchProcessingCancelled();
            WeakReferenceMessenger.Default.Send(new DialogMessage("Cancelled", "Batch processing was cancelled."));
        }
        catch (Exception ex)
        {
            LogBatchProcessingError(ex);
            WeakReferenceMessenger.Default.Send(new DialogMessage("Error", $"Unexpected error: {ex.Message}"));
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Summarizes the outcome of the batch processing and sends a dialog message with the results.
    /// </summary>
    private void SummarizeOutcome()
    {
        var report = _batchReportStore.ActiveBatch;
        if (report.Failed > 0)
        {
            var failures = report.FileReports
                .Where(r => r.Status == ProcessingStatus.Failed)
                .Select(r => $"{r.Path} => {string.Join("; ", r.Errors)}");

            WeakReferenceMessenger.Default.Send(new DialogMessage(
                "Batch Completed with Errors",
                string.Join(Environment.NewLine, failures)));
        }
        else if (report.Warnings > 0)
        {
            var warns = report.FileReports
                .Where(r => r.Status == ProcessingStatus.SucceededWithWarnings)
                .Select(r => $"{r.Path} => {string.Join("; ", r.Warnings)}");

            WeakReferenceMessenger.Default.Send(new DialogMessage(
                "Batch Completed with Warnings",
                string.Join(Environment.NewLine, warns)));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new DialogMessage(
                "Batch Completed",
                "All files processed successfully."));
        }
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
    /// Handles the <see cref="IBatchConfiguration.StateChanged"/> event to update the ability to process the batch.
    /// </summary>
    /// <param name="sender">The source of the event. This can be <see langword="null"/>.</param>
    /// <param name="eventArgs">The event data associated with the state change.</param>
    private void BatchConfigurationOnStateChangedHandler(object? sender, EventArgs eventArgs)
    {
        var commands = _mkvPropeditArgumentsBuilder.BuildBatchArguments(_batchConfiguration);
        (bool isValid, _) = ValidateMkvpropeditArguments(commands);
        CanProcessBatch = isValid && _batchConfiguration.FileList.Count > 0;
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
