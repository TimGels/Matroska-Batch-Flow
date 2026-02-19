using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

/// <summary>
/// Contains unit tests for the <see cref="NavigationService"/> class.
/// Note: Many navigation tests require UI context and are better suited as integration tests.
/// These unit tests cover service initialization, state management, and basic navigation logic.
/// </summary>
public class NavigationServiceTests
{
    private readonly IPageService _pageService;
    private readonly ILogger<NavigationService> _logger;

    public NavigationServiceTests()
    {
        _pageService = Substitute.For<IPageService>();
        _logger = Substitute.For<ILogger<NavigationService>>();
    }

    private NavigationService CreateService() => new(_pageService, _logger);

    [Fact]
    public void CanGoBack_WhenFrameIsNull_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var canGoBack = service.CanGoBack;

        // Assert
        Assert.False(canGoBack);
    }

    [Fact]
    public void CanGoBack_WhenFrameCannotGoBack_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var frame = Substitute.For<Frame>();
        frame.CanGoBack.Returns(false);
        service.Frame = frame;

        // Act
        var canGoBack = service.CanGoBack;

        // Assert
        Assert.False(canGoBack);
    }

    [Fact]
    public void CanGoBack_WhenFrameCanGoBack_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var frame = Substitute.For<Frame>();
        frame.CanGoBack.Returns(true);
        service.Frame = frame;

        // Act
        var canGoBack = service.CanGoBack;

        // Assert
        Assert.True(canGoBack);
    }

    [Fact]
    public void GoBack_WhenCannotGoBack_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var frame = Substitute.For<Frame>();
        frame.CanGoBack.Returns(false);
        service.Frame = frame;

        // Act
        var result = service.GoBack();

        // Assert
        Assert.False(result);
        frame.DidNotReceive().GoBack();
    }

    [Fact]
    public void GoBack_WhenCanGoBack_CallsFrameGoBack()
    {
        // Arrange
        var service = CreateService();
        var frame = Substitute.For<Frame>();
        frame.CanGoBack.Returns(true);
        service.Frame = frame;

        // Act
        var result = service.GoBack();

        // Assert
        Assert.True(result);
        frame.Received(1).GoBack();
    }

    [Fact]
    public void NavigateTo_WhenFrameIsNull_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var pageKey = "TestPage";

        // Act
        var result = service.NavigateTo(pageKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NavigateTo_WithValidPageKey_NavigatesToPage()
    {
        // Arrange
        var service = CreateService();
        var frame = Substitute.For<Frame>();
        var pageType = typeof(MainPage);
        var pageKey = typeof(MainViewModel).FullName!;

        _pageService.GetPageType(pageKey).Returns(pageType);
        service.Frame = frame;
        frame.Navigate(pageType, Arg.Any<object?>()).Returns(true);

        // Act
        var result = service.NavigateTo(pageKey);

        // Assert
        Assert.True(result);
        frame.Received(1).Navigate(pageType, null);
    }

}
