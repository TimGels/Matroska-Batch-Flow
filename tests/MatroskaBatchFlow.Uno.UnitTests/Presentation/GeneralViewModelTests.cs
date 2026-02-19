using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Presentation;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

/// <summary>
/// Contains unit tests for the <see cref="GeneralViewModel"/> class.
/// </summary>
public class GeneralViewModelTests
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly UniqueObservableCollection<ScannedFileInfo> _fileList;

    public GeneralViewModelTests()
    {
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _fileList = [];
        _batchConfiguration.FileList.Returns(_fileList);
    }

    [Fact]
    public void Title_GetFromBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.Title.Returns("Test Title");
        var viewModel = CreateViewModel();

        // Act
        var title = viewModel.Title;

        // Assert
        Assert.Equal("Test Title", title);
    }

    [Fact]
    public void Title_SetToBatchConfiguration()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Title = "New Title";

        // Assert
        _batchConfiguration.Received().Title = "New Title";
    }

    [Fact]
    public void ShouldModifyTitle_GetFromBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.ShouldModifyTitle.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        var shouldModify = viewModel.ShouldModifyTitle;

        // Assert
        Assert.True(shouldModify);
    }

    [Fact]
    public void ShouldModifyTitle_SetToBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.ShouldModifyTitle.Returns(false);
        var viewModel = CreateViewModel();

        // Act
        viewModel.ShouldModifyTitle = true;

        // Assert
        _batchConfiguration.Received().ShouldModifyTitle = true;
    }

    [Fact]
    public void AddTrackStatisticsTags_GetFromBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.AddTrackStatisticsTags.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.AddTrackStatisticsTags;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AddTrackStatisticsTags_SetToBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.AddTrackStatisticsTags.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddTrackStatisticsTags = false;

        // Assert
        _batchConfiguration.Received().AddTrackStatisticsTags = false;
    }

    [Fact]
    public void DeleteTrackStatisticsTags_GetFromBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.DeleteTrackStatisticsTags.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.DeleteTrackStatisticsTags;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DeleteTrackStatisticsTags_SetToBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.DeleteTrackStatisticsTags.Returns(true);
        var viewModel = CreateViewModel();

        // Act
        viewModel.DeleteTrackStatisticsTags = false;

        // Assert
        _batchConfiguration.Received().DeleteTrackStatisticsTags = false;
    }

    [Fact]
    public void ShouldModifyTrackStatisticsTags_GetFromBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.ShouldModifyTrackStatisticsTags.Returns(false);
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.ShouldModifyTrackStatisticsTags;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldModifyTrackStatisticsTags_SetToBatchConfiguration()
    {
        // Arrange
        _batchConfiguration.ShouldModifyTrackStatisticsTags.Returns(false);
        var viewModel = CreateViewModel();

        // Act
        viewModel.ShouldModifyTrackStatisticsTags = true;

        // Assert
        _batchConfiguration.Received().ShouldModifyTrackStatisticsTags = true;
    }

    [Fact]
    public void IsFileListPopulated_ReturnsTrueWhenFilesExist()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.IsFileListPopulated;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFileListPopulated_ReturnsFalseWhenNoFiles()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var result = viewModel.IsFileListPopulated;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OnFileListChanged_RaisesPropertyChangedForIsFileListPopulated()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsFileListPopulated))
                propertyChanged = true;
        };

        // Act
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnFileListChanged_WhenFileRemoved_RaisesPropertyChanged()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsFileListPopulated))
                propertyChanged = true;
        };

        // Act
        _fileList.Remove(file);

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnBatchConfigurationChanged_ForTitle_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.Title))
                propertyChanged = true;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.Title)));

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnBatchConfigurationChanged_ForShouldModifyTitle_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ShouldModifyTitle))
                propertyChanged = true;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.ShouldModifyTitle)));

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnBatchConfigurationChanged_ForAddTrackStatisticsTags_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.AddTrackStatisticsTags))
                propertyChanged = true;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.AddTrackStatisticsTags)));

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnBatchConfigurationChanged_ForDeleteTrackStatisticsTags_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.DeleteTrackStatisticsTags))
                propertyChanged = true;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.DeleteTrackStatisticsTags)));

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnBatchConfigurationChanged_ForShouldModifyTrackStatisticsTags_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChanged = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ShouldModifyTrackStatisticsTags))
                propertyChanged = true;
        };

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs(nameof(IBatchConfiguration.ShouldModifyTrackStatisticsTags)));

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public void OnBatchConfigurationChanged_WithUnrelatedPropertyName_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var propertyChangedCount = 0;
        viewModel.PropertyChanged += (s, e) => propertyChangedCount++;

        // Act
        _batchConfiguration.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _batchConfiguration,
            new PropertyChangedEventArgs("SomeUnrelatedProperty"));

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    private GeneralViewModel CreateViewModel()
    {
        return new GeneralViewModel(_batchConfiguration);
    }

    private static ScannedFileInfo CreateScannedFile(string path)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Audio);
        return new ScannedFileInfo(builder.Build(), path);
    }
}
