using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

/// <summary>
/// Contains unit tests for the <see cref="InputViewModel"/> class.
/// </summary>
public class InputViewModelTests : IDisposable
{
    private readonly IFileListAdapter _fileListAdapter;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IFilePickerDialogService _filePickerDialogService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationStateService _validationStateService;
    private readonly IInputOperationFeedbackService _inputOperationFeedbackService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private readonly ILogger<InputViewModel> _logger;
    private readonly UniqueObservableCollection<ScannedFileInfo> _fileList;

    public InputViewModelTests()
    {
        _fileListAdapter = Substitute.For<IFileListAdapter>();
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _filePickerDialogService = Substitute.For<IFilePickerDialogService>();
        _userSettings = Substitute.For<IWritableSettings<UserSettings>>();
        _validationStateService = Substitute.For<IValidationStateService>();
        _inputOperationFeedbackService = Substitute.For<IInputOperationFeedbackService>();
        _orchestrator = Substitute.For<IBatchOperationOrchestrator>();
        _logger = Substitute.For<ILogger<InputViewModel>>();

        _fileList = [];

        _batchConfiguration.FileList.Returns(_fileList);
        _userSettings.Value.Returns(new UserSettings());
    }

    [Fact]
    public void CanSelectAll_ReturnsTrueWhenFileListCountExceedsSelectedCount()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        // Act
        var canSelectAll = viewModel.CanSelectAll;

        // Assert
        Assert.True(canSelectAll);
    }

    [Fact]
    public void CanSelectAll_ReturnsFalseWhenAllFilesSelected()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);
        viewModel.SelectedFiles.Add(new ScannedFileViewModel(file, _batchConfiguration));

        // Act
        var canSelectAll = viewModel.CanSelectAll;

        // Assert
        Assert.False(canSelectAll);
    }

    [Fact]
    public void IsValidationInfoBarOpen_WhenSetToFalse_ClearsNotifications()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ValidationNotifications.AddNotifications([
            new ValidationNotificationItem
            {
                Severity = ValidationSeverity.Error,
                FilePath = "test.mkv",
                Message = "Test error"
            }
        ]);
        viewModel.IsValidationInfoBarOpen = true;
        Assert.True(viewModel.HasValidationNotifications);

        // Act
        viewModel.IsValidationInfoBarOpen = false;

        // Assert
        Assert.False(viewModel.HasValidationNotifications);
    }

    [Fact]
    public void ValidationProperties_ReflectNotificationState_WithErrors()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ValidationNotifications.AddNotifications([
            new ValidationNotificationItem
            {
                Severity = ValidationSeverity.Error,
                FilePath = "test.mkv",
                Message = "Test error"
            }
        ]);

        // Assert
        Assert.True(viewModel.HasValidationNotifications);
        Assert.True(viewModel.HasErrors);
        Assert.False(viewModel.HasWarnings);
        Assert.False(viewModel.HasInfoMessages);
    }

    [Fact]
    public void ValidationProperties_ReflectNotificationState_WithWarnings()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ValidationNotifications.AddNotifications([
            new ValidationNotificationItem
            {
                Severity = ValidationSeverity.Warning,
                FilePath = "test.mkv",
                Message = "Test warning"
            }
        ]);

        // Assert
        Assert.True(viewModel.HasValidationNotifications);
        Assert.False(viewModel.HasErrors);
        Assert.True(viewModel.HasWarnings);
        Assert.False(viewModel.HasInfoMessages);
    }

    [Fact]
    public async Task RemoveSelected_WithSelectedFiles_DelegatesToOrchestrator()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var file = CreateScannedFile("file1.mkv");
        var fileVm = new ScannedFileViewModel(file, _batchConfiguration);
        viewModel.SelectedFiles.Add(fileVm);

        // Act
        await ((IAsyncRelayCommand)viewModel.RemoveSelected).ExecuteAsync(null);

        // Assert
        await _orchestrator.Received(1).RemoveFilesAsync(Arg.Is<IEnumerable<ScannedFileInfo>>(
            files => files.Contains(file)));
    }

    [Fact]
    public void SelectAll_AddsAllScannedFileViewModelsToSelectedFiles()
    {
        // Arrange
        var file1 = CreateScannedFile("file1.mkv");
        var file2 = CreateScannedFile("file2.mkv");
        var vm1 = new ScannedFileViewModel(file1, _batchConfiguration);
        var vm2 = new ScannedFileViewModel(file2, _batchConfiguration);
        var scannedFileViewModels = new ObservableCollection<ScannedFileViewModel> { vm1, vm2 };
        _fileListAdapter.ScannedFileViewModels.Returns(scannedFileViewModels);
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectAll.Execute(null);

        // Assert
        Assert.Equal(2, viewModel.SelectedFiles.Count);
    }

    public void Dispose()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Reset();
    }

    private InputViewModel CreateViewModel()
    {
        return new InputViewModel(
            _fileListAdapter,
            _batchConfiguration,
            _filePickerDialogService,
            _userSettings,
            _validationStateService,
            _inputOperationFeedbackService,
            _orchestrator,
            _logger);
    }

    private static ScannedFileInfo CreateScannedFile(string path)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Audio);
        return new ScannedFileInfo(builder.Build(), path);
    }
}
