using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Enums;
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
        _logger = Substitute.For<ILogger<SettingsViewModel>>();

        _userSettingsValue = new UserSettings();
        _userSettings.Value.Returns(_userSettingsValue);
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
            _logger);
    }
}
