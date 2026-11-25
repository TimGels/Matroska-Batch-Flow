using System.Collections.ObjectModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Enums;
using MatroskaBatchFlow.Uno.Helpers;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

/// <summary>
/// Contains unit tests for the <see cref="SettingsViewModel"/> class.
/// </summary>
public class SettingsViewModelTests
{
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationSettingsService _validationSettingsService;
    private readonly IValidationStateService _validationStateService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private readonly IUIPreferencesService _uiPreferences;
    private readonly ILogLevelService _logLevelService;
    private readonly IOptions<LoggingOptions> _loggingOptions;
    private readonly IExternalLinkService _externalLinkService;
    private readonly IBatchHistoryService _batchHistory;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly UserSettings _userSettingsValue;

    public SettingsViewModelTests()
    {
        _userSettings = Substitute.For<IWritableSettings<UserSettings>>();
        _validationSettingsService = Substitute.For<IValidationSettingsService>();
        _validationStateService = Substitute.For<IValidationStateService>();
        _orchestrator = Substitute.For<IBatchOperationOrchestrator>();
        _uiPreferences = Substitute.For<IUIPreferencesService>();
        _logLevelService = Substitute.For<ILogLevelService>();
        _loggingOptions = Substitute.For<IOptions<LoggingOptions>>();
        _externalLinkService = Substitute.For<IExternalLinkService>();
        _batchHistory = Substitute.For<IBatchHistoryService>();
        _batchHistory.MaxRuns.Returns(UserSettings.BatchHistorySettings.DefaultMaxRuns);
        _logger = Substitute.For<ILogger<SettingsViewModel>>();

        _userSettingsValue = new UserSettings();
        _userSettings.Value.Returns(_userSettingsValue);
        _userSettings.ReadFromDiskAsync().Returns(Task.FromResult(_userSettingsValue));
        _loggingOptions.Value.Returns(new LoggingOptions());
    }

    [Fact]
    public void Constructor_LoadsThemeFromUIPreferences()
    {
        // Arrange
        _uiPreferences.AppTheme.Returns(AppThemePreference.Dark);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal((int)AppThemePreference.Dark, viewModel.SelectedThemeIndex);
    }

    [Fact]
    public void Constructor_LoadsLogLevelFromUserSettings_WhenNotConfiguredInAppSettings()
    {
        // Arrange
        _loggingOptions.Value.Returns(new LoggingOptions { MinimumLevel = "" });
        _userSettingsValue.UI.LogLevel = "Debug";

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(1, viewModel.SelectedLogLevelIndex); // Debug = 1
        Assert.True(viewModel.IsLogLevelControlEnabled);
    }

    [Fact]
    public void Constructor_LoadsLogLevelFromAppSettings_WhenConfigured()
    {
        // Arrange
        _loggingOptions.Value.Returns(new LoggingOptions { MinimumLevel = "Warning" });

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.Equal(3, viewModel.SelectedLogLevelIndex); // Warning = 3
        Assert.False(viewModel.IsLogLevelControlEnabled);
    }

    [Fact]
    public void ShowTrackAvailabilityText_GetFromUIPreferences()
    {
        // Arrange
        _uiPreferences.ShowTrackAvailabilityText.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.ShowTrackAvailabilityText;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShowTrackAvailabilityText_SetToUIPreferences()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.ShowTrackAvailabilityText = true;

        // Assert
        _uiPreferences.Received().ShowTrackAvailabilityText = true;
    }

    [Fact]
    public void EnableLoggingView_GetFromUIPreferences()
    {
        // Arrange
        _uiPreferences.EnableLoggingView.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.EnableLoggingView;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EnableLoggingView_SetToUIPreferences()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.EnableLoggingView = false;

        // Assert
        _uiPreferences.Received().EnableLoggingView = false;
    }

    [Fact]
    public void IsCustomMode_ReturnsTrueWhenCustomModeSelected()
    {
        // Arrange - start with custom mode set in settings
        _userSettingsValue.BatchValidation.Mode = StrictnessMode.Custom;
        var viewModel = CreateViewModel();

        // Assert - should initialize to Custom mode
        Assert.True(viewModel.IsCustomMode);
        Assert.Equal((int)StrictnessMode.Custom, viewModel.SelectedStrictnessModeIndex);
    }

    [Fact]
    public void IsCustomMode_ReturnsFalseWhenStrictModeSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedStrictnessModeIndex = (int)StrictnessMode.Strict;

        // Assert
        Assert.False(viewModel.IsCustomMode);
    }

    [Fact]
    public void LogLevelDescription_WhenEnabled_ShowsStandardDescription()
    {
        // Arrange
        _loggingOptions.Value.Returns(new LoggingOptions { MinimumLevel = "" });
        var viewModel = CreateViewModel();

        // Act
        var description = viewModel.LogLevelDescription;

        // Assert
        Assert.Contains("Set the minimum logging level", description);
    }

    [Fact]
    public void LogLevelDescription_WhenDisabled_ShowsConfiguredLevel()
    {
        // Arrange
        _loggingOptions.Value.Returns(new LoggingOptions { MinimumLevel = "Error" });
        var viewModel = CreateViewModel();

        // Act
        var description = viewModel.LogLevelDescription;

        // Assert
        Assert.Contains("Error", description);
        Assert.Contains("appsettings.json", description);
    }

    [Fact]
    public void OnSelectedThemeIndexChanged_UpdatesAppTheme()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedThemeIndex = (int)AppThemePreference.Dark;

        // Assert
        _uiPreferences.Received().AppTheme = AppThemePreference.Dark;
    }

    [Fact]
    public void ChangingStrictnessMode_DelegatesToOrchestrator()
    {
        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>()).Returns(Task.CompletedTask);
        _orchestrator.RevalidateAsync().Returns(Task.CompletedTask);

        _userSettingsValue.BatchValidation.Mode = StrictnessMode.Custom;
        var viewModel = CreateViewModel();

        viewModel.SelectedStrictnessModeIndex = (int)StrictnessMode.Strict;

        _orchestrator.Received(1).RevalidateAsync();
    }

    [Fact]
    public async Task OpenSupportPageCommand_OpensSupportPageOnce()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.OpenSupportPageCommand.ExecuteAsync(null);

        // Assert
        await _externalLinkService.Received(1).OpenAsync(Arg.Any<Uri>());
        await _externalLinkService.Received(1).OpenAsync(ProjectLinks.SupportPage);
    }

    [Theory]
    [InlineData(double.NaN, 20)]
    [InlineData(0, 1)]
    [InlineData(500, 100)]
    [InlineData(7.4, 7)]
    public async Task BatchHistoryMaxRuns_SavesAValidLimit(double entered, int expected)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.BatchHistoryMaxRuns = entered;

        // Assert
        Assert.Equal(expected, viewModel.BatchHistoryMaxRuns);
        if (expected == UserSettings.BatchHistorySettings.DefaultMaxRuns)
        {
            await _batchHistory.DidNotReceive().SetMaxRunsAsync(Arg.Any<int>(), Arg.Any<int>());
        }
        else
        {
            await _batchHistory.Received(1).SetMaxRunsAsync(expected);
        }
    }

    [Fact]
    public async Task BatchHistoryMaxRuns_WaitsForConfirmation_WhenSavedResultsWouldBeRemoved()
    {
        // Arrange
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        var viewModel = CreateViewModel();

        // Act
        viewModel.BatchHistoryMaxRuns = 7;

        // Assert
        Assert.Equal(3, viewModel.BatchHistoryRunsToRemove);
        Assert.True(viewModel.IsBatchHistoryLimitPending);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(7, Arg.Is<int>(count => count > 0));

        // Act
        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);

        // Assert
        Assert.False(viewModel.IsBatchHistoryLimitPending);
        await _batchHistory.Received(1).SetMaxRunsAsync(7, 3);
    }

    [Fact]
    public async Task CancelBatchHistoryLimit_RestoresTheSavedLimit()
    {
        // Arrange
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        // Act
        viewModel.CancelBatchHistoryLimitCommand.Execute(null);

        // Assert
        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, viewModel.BatchHistoryMaxRuns);
        Assert.False(viewModel.IsBatchHistoryLimitPending);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(7, Arg.Is<int>(count => count > 0));
    }

    [Fact]
    public async Task CancelBatchHistoryLimit_ClosesTheSaveError_SoTheLimitIsRefreshedAgain()
    {
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        _batchHistory.SetMaxRunsAsync(7, 3).Returns(Task.FromException<int>(new IOException("history file is read-only")));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;
        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);
        Assert.True(viewModel.BatchHistoryLimitSaveFailed);

        viewModel.CancelBatchHistoryLimitCommand.Execute(null);
        _userSettingsValue.BatchHistory.MaxRuns = 50;
        viewModel.OnNavigatedTo(null!);

        Assert.False(viewModel.BatchHistoryLimitSaveFailed);
        Assert.Equal(50, viewModel.BatchHistoryMaxRuns);
    }

    [Fact]
    public async Task BatchHistoryMaxRuns_ClosesTheSaveError_WhenTheLimitInEffectIsEnteredAgain()
    {
        _batchHistory.SetMaxRunsAsync(30).Returns(Task.FromException<int>(
            new BatchHistoryLimitSaveException(false, new IOException("settings file is read-only"))));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 30;
        Assert.True(viewModel.BatchHistoryLimitSaveFailed);

        viewModel.BatchHistoryMaxRuns = UserSettings.BatchHistorySettings.DefaultMaxRuns;

        Assert.False(viewModel.BatchHistoryLimitSaveFailed);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(UserSettings.BatchHistorySettings.DefaultMaxRuns, Arg.Any<int>());
    }

    [Fact]
    public async Task CancelBatchHistoryLimit_IsUnavailable_WhileTheLimitIsConfirmed()
    {
        var finishConfirm = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        _batchHistory.SetMaxRunsAsync(7, 3).Returns(async _ =>
        {
            await finishConfirm.Task;
            return 0;
        });
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        var confirm = viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);

        Assert.False(viewModel.CancelBatchHistoryLimitCommand.CanExecute(null));
        finishConfirm.SetResult();
        await confirm;
        Assert.True(viewModel.CancelBatchHistoryLimitCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(2, false, true)]
    [InlineData(10, true, false)]
    public async Task ConfirmBatchHistoryLimit_ClosesOnlyWhenTheServiceReportsThatTheTrimCommitted(
        int runsAfterFailure,
        bool historyTrimmed,
        bool expectedPending)
    {
        var runs = Enumerable.Range(0, runsAfterFailure)
            .Select(_ => new BatchRunRecord(Guid.NewGuid(), DateTimeOffset.UnixEpoch, []));
        _batchHistory.Runs.Returns(new ReadOnlyObservableCollection<BatchRunRecord>(new ObservableCollection<BatchRunRecord>(runs)));
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        _batchHistory.SetMaxRunsAsync(7, 3).Returns(Task.FromException<int>(
            new BatchHistoryLimitSaveException(historyTrimmed, new IOException("settings file is read-only"))));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);

        Assert.True(viewModel.BatchHistoryLimitSaveFailed);
        Assert.Equal(expectedPending, viewModel.IsBatchHistoryLimitPending);
    }

    [Fact]
    public async Task ConfirmBatchHistoryLimit_StaysPending_WhenHistoryWriteFailsDespiteLocalRunsBeingWithinLimit()
    {
        var runs = Enumerable.Range(0, 2)
            .Select(_ => new BatchRunRecord(Guid.NewGuid(), DateTimeOffset.UnixEpoch, []));
        _batchHistory.Runs.Returns(new ReadOnlyObservableCollection<BatchRunRecord>(new ObservableCollection<BatchRunRecord>(runs)));
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        _batchHistory.SetMaxRunsAsync(7, 3).Returns(Task.FromException<int>(new IOException("history file is read-only")));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);

        Assert.True(viewModel.BatchHistoryLimitSaveFailed);
        Assert.True(viewModel.IsBatchHistoryLimitPending);
    }

    [Fact]
    public async Task BatchHistoryMaxRuns_SavesAtOnce_WhenRaisedAgainBeforeConfirming()
    {
        // Arrange
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        // Act
        viewModel.BatchHistoryMaxRuns = 12;

        // Assert
        Assert.False(viewModel.IsBatchHistoryLimitPending);
        await _batchHistory.Received(1).SetMaxRunsAsync(12);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(7, Arg.Is<int>(count => count > 0));
    }

    [Fact]
    public async Task ConfirmBatchHistoryLimit_ShowsChangedRemovalCount_AndRequiresAnotherConfirmation()
    {
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        _batchHistory.SetMaxRunsAsync(7, 3).Returns(4);
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsBatchHistoryLimitPending);
        Assert.Equal(4, viewModel.BatchHistoryRunsToRemove);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(7, 4);

        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);
        Assert.False(viewModel.IsBatchHistoryLimitPending);
        await _batchHistory.Received(1).SetMaxRunsAsync(7, 4);
    }

    [Fact]
    public async Task BatchHistoryMaxRuns_SavesAnEditReturningToLiveValue_DuringASave()
    {
        var savedLimit = 21;
        var delayedSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var latestSaved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _batchHistory.MaxRuns.Returns(_ => savedLimit);
        _batchHistory.SetMaxRunsAsync(22).Returns(async _ =>
        {
            await delayedSave.Task;
            savedLimit = 22;
            return 0;
        });
        _batchHistory.SetMaxRunsAsync(21).Returns(_ =>
        {
            savedLimit = 21;
            latestSaved.TrySetResult();
            return Task.FromResult(0);
        });
        var viewModel = CreateViewModel();

        viewModel.BatchHistoryMaxRuns = 22;
        viewModel.BatchHistoryMaxRuns = 21;
        delayedSave.SetResult();

        await latestSaved.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(21, savedLimit);
        Assert.Equal(savedLimit, viewModel.BatchHistoryMaxRuns);
    }

    [Fact]
    public async Task OnNavigatedTo_ShowsThePersistedLimit_WithoutSavingIt()
    {
        var savedSettings = new UserSettings();
        savedSettings.BatchHistory.MaxRuns = 50;
        _userSettings.ReadFromDiskAsync().Returns(Task.FromResult(savedSettings));
        var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(null!);

        Assert.Equal(50, viewModel.BatchHistoryMaxRuns);
        Assert.Equal(50, _userSettingsValue.BatchHistory.MaxRuns);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task OnNavigatedTo_KeepsALimitThatWaitsForConfirmation()
    {
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        var savedSettings = new UserSettings();
        savedSettings.BatchHistory.MaxRuns = 50;
        _userSettings.ReadFromDiskAsync().Returns(Task.FromResult(savedSettings));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;

        viewModel.OnNavigatedTo(null!);

        Assert.Equal(7, viewModel.BatchHistoryMaxRuns);
        Assert.Equal(3, viewModel.BatchHistoryRunsToRemove);
        await _userSettings.DidNotReceive().ReadFromDiskAsync();
    }

    [Fact]
    public async Task OnNavigatedTo_DoesNotRefreshWhileLimitSaveIsInProgress()
    {
        var saveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _batchHistory.SetMaxRunsAsync(22).Returns(async _ =>
        {
            saveStarted.TrySetResult();
            await finishSave.Task;
            return 0;
        });
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 22;
        await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        viewModel.OnNavigatedTo(null!);

        Assert.Equal(22, viewModel.BatchHistoryMaxRuns);
        await _userSettings.DidNotReceive().ReadFromDiskAsync();
        finishSave.TrySetResult();
    }

    [Fact]
    public async Task OnNavigatedTo_DiscardsALimitThatCouldNotBeSaved_AndShowsThePersistedLimit()
    {
        _batchHistory.SetMaxRunsAsync(30).Returns(Task.FromException<int>(
            new BatchHistoryLimitSaveException(false, new IOException("settings file is read-only"))));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 30;
        _userSettingsValue.BatchHistory.MaxRuns = 50;

        viewModel.OnNavigatedTo(null!);

        Assert.False(viewModel.BatchHistoryLimitSaveFailed);
        Assert.Equal(50, viewModel.BatchHistoryMaxRuns);
        await _batchHistory.DidNotReceive().SetMaxRunsAsync(50, Arg.Any<int>());
    }

    [Fact]
    public async Task OnNavigatedTo_KeepsAFailedConfirmation_ThatStillWaits()
    {
        _batchHistory.SetMaxRunsAsync(7).Returns(3);
        _batchHistory.SetMaxRunsAsync(7, 3).Returns(Task.FromException<int>(new IOException("history file is read-only")));
        var viewModel = CreateViewModel();
        viewModel.BatchHistoryMaxRuns = 7;
        await viewModel.ConfirmBatchHistoryLimitCommand.ExecuteAsync(null);

        viewModel.OnNavigatedTo(null!);

        Assert.True(viewModel.BatchHistoryLimitSaveFailed);
        Assert.True(viewModel.IsBatchHistoryLimitPending);
        Assert.Equal(7, viewModel.BatchHistoryMaxRuns);
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(
            _userSettings,
            _validationSettingsService,
            _validationStateService,
            _orchestrator,
            _uiPreferences,
            _logLevelService,
            _loggingOptions,
            _externalLinkService,
            _batchHistory,
            _logger);
    }
}
