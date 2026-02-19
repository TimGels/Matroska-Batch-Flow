using MatroskaBatchFlow.Uno.Presentation;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.UI.Xaml.Controls;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

/// <summary>
/// Contains unit tests for the <see cref="PageService"/> class.
/// </summary>
public class PageServiceTests
{
    [Fact]
    public void Constructor_RegistersAllPages()
    {
        // Arrange & Act
        var service = new PageService();

        // Assert - Verify all expected pages are registered
        Assert.NotNull(service.GetPageType(typeof(MainViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(InputViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(GeneralViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(VideoViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(AudioViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(SubtitleViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(OutputViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(SettingsViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(LogViewerViewModel).FullName!));
        Assert.NotNull(service.GetPageType(typeof(BatchResultsViewModel).FullName!));
    }

    [Fact]
    public void GetPageType_WithMainViewModel_ReturnsMainPage()
    {
        // Arrange
        var service = new PageService();
        var key = typeof(MainViewModel).FullName!;

        // Act
        var pageType = service.GetPageType(key);

        // Assert
        Assert.Equal(typeof(MainPage), pageType);
    }

    [Fact]
    public void GetPageType_WithInputViewModel_ReturnsInputPage()
    {
        // Arrange
        var service = new PageService();
        var key = typeof(InputViewModel).FullName!;

        // Act
        var pageType = service.GetPageType(key);

        // Assert
        Assert.Equal(typeof(InputPage), pageType);
    }

    [Fact]
    public void GetPageType_WithSettingsViewModel_ReturnsSettingsPage()
    {
        // Arrange
        var service = new PageService();
        var key = typeof(SettingsViewModel).FullName!;

        // Act
        var pageType = service.GetPageType(key);

        // Assert
        Assert.Equal(typeof(SettingsPage), pageType);
    }

    [Fact]
    public void GetPageType_WithLogViewerViewModel_ReturnsLogViewerPage()
    {
        // Arrange
        var service = new PageService();
        var key = typeof(LogViewerViewModel).FullName!;

        // Act
        var pageType = service.GetPageType(key);

        // Assert
        Assert.Equal(typeof(LogViewerPage), pageType);
    }

    [Fact]
    public void GetPageType_WithBatchResultsViewModel_ReturnsBatchResultsPage()
    {
        // Arrange
        var service = new PageService();
        var key = typeof(BatchResultsViewModel).FullName!;

        // Act
        var pageType = service.GetPageType(key);

        // Assert
        Assert.Equal(typeof(BatchResultsPage), pageType);
    }

    [Fact]
    public void GetPageType_WithUnknownKey_ThrowsArgumentException()
    {
        // Arrange
        var service = new PageService();
        var unknownKey = "Unknown.ViewModel";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => service.GetPageType(unknownKey));
        Assert.Contains("Page not found", exception.Message);
        Assert.Contains(unknownKey, exception.Message);
    }

    [Fact]
    public void GetPageType_WithNullKey_ThrowsException()
    {
        // Arrange
        var service = new PageService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GetPageType(null!));
    }

    [Fact]
    public void GetPageType_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var service = new PageService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.GetPageType(string.Empty));
    }

    [Fact]
    public void GetPageType_WithWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var service = new PageService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.GetPageType("   "));
    }
}
