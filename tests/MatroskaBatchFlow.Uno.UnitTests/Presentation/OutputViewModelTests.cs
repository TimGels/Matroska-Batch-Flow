using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Presentation;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

/// <summary>
/// Contains unit tests for the <see cref="OutputViewModel"/> class.
/// </summary>
public class OutputViewModelTests
{
    private readonly IBatchConfiguration _batchConfiguration;

    public OutputViewModelTests()
    {
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
    }

    [Fact]
    public void Constructor_InitializesBatchConfiguration()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.BatchConfiguration);
        Assert.Same(_batchConfiguration, viewModel.BatchConfiguration);
    }

    private OutputViewModel CreateViewModel()
    {
        return new OutputViewModel(_batchConfiguration);
    }
}
