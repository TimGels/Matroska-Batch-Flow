using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Enums;
using MatroskaBatchFlow.Uno.Services;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

/// <summary>
/// Contains unit tests for the <see cref="UIPreferencesService"/> class.
/// </summary>
public class UIPreferencesServiceTests
{
    private static IWritableSettings<UserSettings> CreateMockSettings(UserSettings? settings = null)
    {
        settings ??= new UserSettings();
        var mock = Substitute.For<IWritableSettings<UserSettings>>();
        mock.Value.Returns(settings);
        return mock;
    }

    [Fact]
    public void EnableLoggingView_DefaultValue_IsFalse()
    {
        // Arrange
        var settings = new UserSettings();
        var mockSettings = CreateMockSettings(settings);

        // Act
        var service = new UIPreferencesService(mockSettings);

        // Assert
        Assert.False(service.EnableLoggingView);
    }

    [Fact]
    public void EnableLoggingView_WhenSettingIsTrue_ReturnsTrue()
    {
        // Arrange
        var settings = new UserSettings { UI = { EnableLoggingView = true } };
        var mockSettings = CreateMockSettings(settings);

        // Act
        var service = new UIPreferencesService(mockSettings);

        // Assert
        Assert.True(service.EnableLoggingView);
    }

    [Fact]
    public void EnableLoggingView_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var mockSettings = CreateMockSettings();
        var service = new UIPreferencesService(mockSettings);

        // Act & Assert
        Assert.PropertyChanged(
            service,
            nameof(UIPreferencesService.EnableLoggingView),
            () => service.EnableLoggingView = true);

        Assert.True(service.EnableLoggingView);
    }

    [Fact]
    public void EnableLoggingView_SetValue_PersistsToSettings()
    {
        // Arrange
        var mockSettings = CreateMockSettings();
        var service = new UIPreferencesService(mockSettings);

        // Act
        service.EnableLoggingView = true;

        // Assert
        mockSettings.Received(1).UpdateAsync(Arg.Any<Action<UserSettings>>());
    }

    [Fact]
    public void ShowTrackAvailabilityText_DefaultValue_IsTrue()
    {
        // Arrange
        var settings = new UserSettings();
        var mockSettings = CreateMockSettings(settings);

        // Act
        var service = new UIPreferencesService(mockSettings);

        // Assert
        Assert.True(service.ShowTrackAvailabilityText);
    }

    [Fact]
    public void ShowTrackAvailabilityText_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var mockSettings = CreateMockSettings();
        var service = new UIPreferencesService(mockSettings);

        // Act & Assert
        Assert.PropertyChanged(
            service,
            nameof(UIPreferencesService.ShowTrackAvailabilityText),
            () => service.ShowTrackAvailabilityText = false);
    }

    [Fact]
    public void AppTheme_DefaultValue_IsSystem()
    {
        // Arrange
        var settings = new UserSettings { UI = { Theme = "System" } };
        var mockSettings = CreateMockSettings(settings);

        // Act
        var service = new UIPreferencesService(mockSettings);

        // Assert
        Assert.Equal(AppThemePreference.System, service.AppTheme);
    }

    [Fact]
    public void AppTheme_WithInvalidThemeValue_FallsBackToSystem()
    {
        // Arrange
        var settings = new UserSettings { UI = { Theme = "InvalidTheme" } };
        var mockSettings = CreateMockSettings(settings);

        // Act
        var service = new UIPreferencesService(mockSettings);

        // Assert
        Assert.Equal(AppThemePreference.System, service.AppTheme);
    }

    [Theory]
    [InlineData("Light", AppThemePreference.Light)]
    [InlineData("Dark", AppThemePreference.Dark)]
    [InlineData("System", AppThemePreference.System)]
    public void AppTheme_WithValidThemeValue_ParsesCorrectly(string themeString, AppThemePreference expected)
    {
        // Arrange
        var settings = new UserSettings { UI = { Theme = themeString } };
        var mockSettings = CreateMockSettings(settings);

        // Act
        var service = new UIPreferencesService(mockSettings);

        // Assert
        Assert.Equal(expected, service.AppTheme);
    }

    [Fact]
    public void AppTheme_SetValue_PersistsToSettings()
    {
        // Arrange
        var mockSettings = CreateMockSettings();
        var service = new UIPreferencesService(mockSettings);

        // Act
        service.AppTheme = AppThemePreference.Dark;

        // Assert
        mockSettings.Received(1).UpdateAsync(Arg.Any<Action<UserSettings>>());
    }
}
