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

    [Fact]
    public void BatchConfiguration_CanBeSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var newBatchConfig = Substitute.For<IBatchConfiguration>();

        // Act
        viewModel.BatchConfiguration = newBatchConfig;

        // Assert
        Assert.Same(newBatchConfig, viewModel.BatchConfiguration);
    }

    [Fact]
    public void BatchConfiguration_SetDifferentValue_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        var newBatchConfig = Substitute.For<IBatchConfiguration>();

        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.BatchConfiguration))
                propertyChanged = true;
        };

        // Act
        viewModel.BatchConfiguration = newBatchConfig;

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void BatchConfiguration_SetSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChangedCount = 0;

        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.BatchConfiguration))
                propertyChangedCount++;
        };

        // Act
        viewModel.BatchConfiguration = _batchConfiguration;

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    private OutputViewModel CreateViewModel()
    {
        return new OutputViewModel(_batchConfiguration);
    }
}
