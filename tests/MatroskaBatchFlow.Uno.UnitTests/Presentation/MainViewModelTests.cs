using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

/// <summary>
/// Contains unit tests for the <see cref="MainViewModel"/> class.
/// </summary>
[Collection(MessengerCollectionDefinition.Name)]
public class MainViewModelTests
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly INavigationService _navigationService;
    private readonly INavigationViewService _navigationViewService;
    private readonly IFileProcessingOrchestrator _orchestrator;
    private readonly IBatchReportStore _batchReportStore;
    private readonly IMkvPropeditArgumentsGenerator _argumentsGenerator;
    private readonly IValidationStateService _validationStateService;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly IScannedFileInfoPathComparer _pathComparer;
    private readonly ITaskbarProgressService _taskbarProgress;
    private readonly UniqueObservableCollection<ScannedFileInfo> _fileList;

    public MainViewModelTests()
    {
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _navigationService = Substitute.For<INavigationService>();
        _navigationViewService = Substitute.For<INavigationViewService>();
        _orchestrator = Substitute.For<IFileProcessingOrchestrator>();
        _batchReportStore = Substitute.For<IBatchReportStore>();
        _argumentsGenerator = Substitute.For<IMkvPropeditArgumentsGenerator>();
        _validationStateService = Substitute.For<IValidationStateService>();
        _uiPreferences = Substitute.For<IUIPreferencesService>();
        _pathComparer = Substitute.For<IScannedFileInfoPathComparer>();
        _taskbarProgress = Substitute.For<ITaskbarProgressService>();

        _fileList = [];
        _batchConfiguration.FileList.Returns(_fileList);
        _batchReportStore.ActiveBatch.Returns((BatchExecutionReport?)null);
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromCurrentState_WhenFilesExist()
    {
        // Arrange - Set up initial state with files and no blocking errors
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        _validationStateService.HasBlockingErrors.Returns(false);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns(["mkvpropedit file1.mkv --set title=Test"]);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.HasFiles);
        Assert.True(viewModel.HasNoBlockingErrors);
        Assert.True(viewModel.HasConfiguredChanges);
        Assert.True(viewModel.CanProcessBatch);
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromCurrentState_WhenNoFiles()
    {
        // Arrange - No files in the list
        _validationStateService.HasBlockingErrors.Returns(false);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns([]);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.False(viewModel.HasFiles);
        Assert.True(viewModel.HasNoBlockingErrors);
        Assert.False(viewModel.HasConfiguredChanges);
        Assert.False(viewModel.CanProcessBatch);
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromCurrentState_WhenBlockingErrorsExist()
    {
        // Arrange - Files exist but with blocking errors
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        _validationStateService.HasBlockingErrors.Returns(true);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns(["mkvpropedit file1.mkv --set title=Test"]);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.HasFiles);
        Assert.False(viewModel.HasNoBlockingErrors); // Blocking errors present
        Assert.True(viewModel.HasConfiguredChanges);
        Assert.False(viewModel.CanProcessBatch); // Cannot process due to blocking errors
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromCurrentState_WhenNoConfiguredChanges()
    {
        // Arrange - Files exist but no valid commands
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        _validationStateService.HasBlockingErrors.Returns(false);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns([""]); // Empty command = no changes configured

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.HasFiles);
        Assert.True(viewModel.HasNoBlockingErrors);
        Assert.False(viewModel.HasConfiguredChanges); // No valid commands
        Assert.False(viewModel.CanProcessBatch); // Cannot process without changes
    }

    [Fact]
    public void OnValidationStateChanged_UpdatesHasFiles()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.False(viewModel.HasFiles);

        var file = CreateScannedFile("file1.mkv");
        _validationStateService.HasBlockingErrors.Returns(false);

        // Act - Adding a file triggers StateChanged event
        _fileList.Add(file);
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        // Assert
        Assert.True(viewModel.HasFiles);
    }

    [Fact]
    public void OnValidationStateChanged_UpdatesHasNoBlockingErrors()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        _validationStateService.HasBlockingErrors.Returns(false);
        var viewModel = CreateViewModel();
        Assert.True(viewModel.HasNoBlockingErrors);

        // Act - Simulate validation finding blocking errors
        _validationStateService.HasBlockingErrors.Returns(true);
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        // Assert
        Assert.False(viewModel.HasNoBlockingErrors);
        Assert.False(viewModel.CanProcessBatch);
    }

    [Fact]
    public void BatchConfigurationStateChanged_UpdatesHasConfiguredChanges()
    {
        // Arrange
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns([""]);
        var viewModel = CreateViewModel();
        Assert.False(viewModel.HasConfiguredChanges);

        // Act - Simulate configuration change that produces valid commands
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns(["mkvpropedit file1.mkv --set title=Test"]);
        _batchConfiguration.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        // Assert
        Assert.True(viewModel.HasConfiguredChanges);
    }

    [Fact]
    public void CanProcessBatch_RequiresAllThreeConditions()
    {
        // Arrange - Start with files, no errors, and valid changes
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        _validationStateService.HasBlockingErrors.Returns(false);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns(["mkvpropedit file1.mkv --set title=Test"]);

        var viewModel = CreateViewModel();
        Assert.True(viewModel.CanProcessBatch);

        // Act & Assert - Remove files
        _fileList.Clear();
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        Assert.False(viewModel.CanProcessBatch);

        // Act & Assert - Add files back but introduce blocking errors
        _fileList.Add(file);
        _validationStateService.HasBlockingErrors.Returns(true);
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        Assert.False(viewModel.CanProcessBatch);

        // Act & Assert - Remove blocking errors but clear configured changes
        _validationStateService.HasBlockingErrors.Returns(false);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns([""]);
        _batchConfiguration.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        Assert.False(viewModel.CanProcessBatch);

        // Act & Assert - Restore all conditions
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns(["mkvpropedit file1.mkv --set title=Test"]);
        _batchConfiguration.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        Assert.True(viewModel.CanProcessBatch);
    }

    [Fact]
    public void EnableLoggingView_ReflectsUIPreferencesValue()
    {
        // Arrange
        _uiPreferences.EnableLoggingView.Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.True(viewModel.EnableLoggingView);
    }

    [Fact]
    public void OnUIPreferencesChanged_UpdatesEnableLoggingView()
    {
        // Arrange
        _uiPreferences.EnableLoggingView.Returns(false);
        var viewModel = CreateViewModel();
        Assert.False(viewModel.EnableLoggingView);

        // Act
        _uiPreferences.EnableLoggingView.Returns(true);
        _uiPreferences.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _uiPreferences,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IUIPreferencesService.EnableLoggingView)));

        // Assert
        Assert.True(viewModel.EnableLoggingView);
    }

    [Fact]
    public void Constructor_SetsBatchReportFromStore()
    {
        // Arrange
        var expectedReport = new BatchExecutionReport();
        _batchReportStore.ActiveBatch.Returns(expectedReport);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Same(expectedReport, viewModel.BatchReport);
    }

    [Fact]
    public async Task ProcessBatch_ShowsProgressOfNewBatch_AndHidesOverlayWhenDone()
    {
        // Arrange
        var batch = SetupProcessableBatch();
        var viewModel = CreateViewModel();
        OperationOverlayState? overlayDuringRun = null;
        var interactionEnabledDuringRun = true;
        _orchestrator.ProcessAllAsync(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var first = CreateReport("media/first.mkv", ProcessingStatus.Succeeded);
                var second = CreateReport("media/second.mkv", ProcessingStatus.Running);
                batch.TryAddFileReport(first);
                batch.TryAddFileReport(second);
                second.Progress = 40;

                overlayDuringRun = viewModel.ProcessingOverlay;
                interactionEnabledDuringRun = viewModel.IsInteractionEnabled;

                second.Status = ProcessingStatus.Succeeded;
                return Task.FromResult(new List<FileProcessingReport> { first, second });
            });

        // Act
        await ((IAsyncRelayCommand)viewModel.ProcessBatchCommand).ExecuteAsync(null);

        // Assert
        Assert.Same(batch, viewModel.BatchReport);
        Assert.NotNull(overlayDuringRun);
        Assert.True(overlayDuringRun.IsActive);
        Assert.Equal(2, overlayDuringRun.Current);
        Assert.Equal(2, overlayDuringRun.Total);
        Assert.Equal("second.mkv (40%)", overlayDuringRun.Detail);
        Assert.Equal(70d, overlayDuringRun.ProgressPercent, precision: 6);
        Assert.False(interactionEnabledDuringRun);

        Assert.False(viewModel.ProcessingOverlay.IsActive);
        Assert.True(viewModel.IsInteractionEnabled);
        _taskbarProgress.Received().SetProgress(Arg.Is<double>(p => Math.Abs(p - 0.7) < 1e-6));
        _taskbarProgress.Received(1).Clear();
    }

    [Fact]
    public async Task CancelProcessing_CancelsRun_AndReportsCancelledBatch()
    {
        // Arrange
        var batch = SetupProcessableBatch();
        var viewModel = CreateViewModel();
        OperationOverlayState? overlayAfterCancel = null;
        var tokenCanceled = false;
        var canCancelAgain = true;
        _orchestrator.ProcessAllAsync(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var report = CreateReport("media/file1.mkv", ProcessingStatus.Running);
                batch.TryAddFileReport(report);

                viewModel.CancelProcessingCommand.Execute(null);
                overlayAfterCancel = viewModel.ProcessingOverlay;
                tokenCanceled = callInfo.Arg<CancellationToken>().IsCancellationRequested;
                canCancelAgain = viewModel.CancelProcessingCommand.CanExecute(null);

                report.Status = ProcessingStatus.Canceled;
                return Task.FromResult(new List<FileProcessingReport> { report });
            });

        var dialogs = new List<DialogMessage>();
        var recipient = new object();
        WeakReferenceMessenger.Default.Register<DialogMessage>(recipient, (_, message) => dialogs.Add(message));

        try
        {
            // Act
            await ((IAsyncRelayCommand)viewModel.ProcessBatchCommand).ExecuteAsync(null);
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
        }

        // Assert
        Assert.True(tokenCanceled);
        Assert.NotNull(overlayAfterCancel);
        Assert.True(overlayAfterCancel.IsCancelling);
        Assert.False(canCancelAgain);
        _taskbarProgress.Received(1).SetPaused();
        Assert.Contains(dialogs, d => d.Title == "Batch Cancelled" && d.Message.Contains("1 of 1 files were not processed"));
        Assert.False(viewModel.IsCancelling);
        Assert.False(viewModel.CancelProcessingCommand.CanExecute(null));
    }

    private BatchExecutionReport SetupProcessableBatch()
    {
        _fileList.Add(CreateScannedFile("media/file1.mkv"));
        _validationStateService.HasBlockingErrors.Returns(false);
        _argumentsGenerator.BuildBatchArguments(_batchConfiguration)
            .Returns(["mkvpropedit file1.mkv --set title=Test"]);

        var batch = new BatchExecutionReport();
        _batchReportStore.CreateBatch().Returns(batch);
        return batch;
    }

    private static FileProcessingReport CreateReport(string path, ProcessingStatus status)
    {
        return new FileProcessingReport
        {
            SourceFile = CreateScannedFile(path),
            Path = path,
            Status = status
        };
    }

    private MainViewModel CreateViewModel()
    {
        var logger = Substitute.For<ILogger<MainViewModel>>();
        return new MainViewModel(
            _batchConfiguration,
            _navigationService,
            _navigationViewService,
            _orchestrator,
            _batchReportStore,
            _argumentsGenerator,
            _validationStateService,
            _uiPreferences,
            _pathComparer,
            _taskbarProgress,
            logger);
    }

    private static ScannedFileInfo CreateScannedFile(string path)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Audio);
        return new ScannedFileInfo(builder.Build(), path);
    }
}
