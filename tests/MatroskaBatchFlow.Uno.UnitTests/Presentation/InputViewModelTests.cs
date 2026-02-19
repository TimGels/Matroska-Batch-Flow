using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileValidation;
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
/// Contains unit tests for the <see cref="InputViewModel"/> class.
/// </summary>
public class InputViewModelTests : IDisposable
{
    private readonly IFileListAdapter _fileListAdapter;
    private readonly IFileScanner _fileScanner;
    private readonly IFileProcessingEngine _fileProcessingEngine;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IBatchTrackConfigurationInitializer _trackConfigInitializer;
    private readonly IFilePickerDialogService _filePickerDialogService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationStateService _validationStateService;
    private readonly IPlatformService _platformService;
    private readonly IScannedFileInfoPathComparer _pathComparer;
    private readonly ILogger<InputViewModel> _logger;
    private readonly UniqueObservableCollection<ScannedFileInfo> _fileList;

    public InputViewModelTests()
    {
        _fileListAdapter = Substitute.For<IFileListAdapter>();
        _fileScanner = Substitute.For<IFileScanner>();
        _fileProcessingEngine = Substitute.For<IFileProcessingEngine>();
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _trackConfigInitializer = Substitute.For<IBatchTrackConfigurationInitializer>();
        _filePickerDialogService = Substitute.For<IFilePickerDialogService>();
        _userSettings = Substitute.For<IWritableSettings<UserSettings>>();
        _validationStateService = Substitute.For<IValidationStateService>();
        _platformService = Substitute.For<IPlatformService>();
        _pathComparer = Substitute.For<IScannedFileInfoPathComparer>();
        _logger = Substitute.For<ILogger<InputViewModel>>();

        _fileList = [];

        _batchConfiguration.FileList.Returns(_fileList);
        _userSettings.Value.Returns(new UserSettings());
        _platformService.IsWindows().Returns(true);
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.SelectedFiles);
        Assert.Empty(viewModel.SelectedFiles);
        Assert.NotNull(viewModel.ValidationNotifications);
        Assert.False(viewModel.IsValidationInfoBarOpen);
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.NotNull(viewModel.RemoveSelected);
        Assert.NotNull(viewModel.RemoveAll);
        Assert.NotNull(viewModel.ClearSelection);
        Assert.NotNull(viewModel.SelectAll);
        Assert.NotNull(viewModel.AddFilesCommand);
        Assert.NotNull(viewModel.ShowValidationDetailsCommand);
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
    public void OnNavigatedTo_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var exception = Record.Exception(() => viewModel.OnNavigatedTo(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void OnNavigatedFrom_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var exception = Record.Exception(() => viewModel.OnNavigatedFrom());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert - Should not throw
        viewModel.Dispose();
    }

    public void Dispose()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Reset();
    }

    private InputViewModel CreateViewModel()
    {
        return new InputViewModel(
            _fileListAdapter,
            _fileScanner,
            _fileProcessingEngine,
            _batchConfiguration,
            _trackConfigInitializer,
            _filePickerDialogService,
            _userSettings,
            _validationStateService,
            _platformService,
            _pathComparer,
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
