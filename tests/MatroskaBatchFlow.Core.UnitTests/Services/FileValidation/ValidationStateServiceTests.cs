using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

/// <summary>
/// Contains unit tests for the <see cref="ValidationStateService"/> class.
/// </summary>
public class ValidationStateServiceTests
{
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IFileValidationEngine _validationEngine;
    private readonly IValidationSettingsService _validationSettingsService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly UniqueObservableCollection<ScannedFileInfo> _fileList;
    private readonly ValidationStateService _sut;

    public ValidationStateServiceTests()
    {
        _batchConfiguration = Substitute.For<IBatchConfiguration>();
        _validationEngine = Substitute.For<IFileValidationEngine>();
        _validationSettingsService = Substitute.For<IValidationSettingsService>();
        _userSettings = Substitute.For<IWritableSettings<UserSettings>>();

        _fileList = [];
        _batchConfiguration.FileList.Returns(_fileList);
        _userSettings.Value.Returns(new UserSettings());
        _validationSettingsService.GetEffectiveSettings(Arg.Any<UserSettings>())
            .Returns(new BatchValidationSettings());

        var logger = NullLoggerFactory.Instance.CreateLogger<ValidationStateService>();

        _sut = new ValidationStateService(
            _validationEngine,
            _batchConfiguration,
            _validationSettingsService,
            _userSettings,
            logger);
    }

    [Fact]
    public void Revalidate_WithEmptyFileList_HasNoResults()
    {
        // Act
        _sut.Revalidate();

        // Assert
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasBlockingErrors);
        Assert.False(_sut.HasWarnings);
        Assert.False(_sut.HasResults);
    }

    [Fact]
    public void Revalidate_WithFiles_RunsValidationEngine()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        var expectedResults = new List<FileValidationResult>
        {
            new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
        };
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(expectedResults);

        // Act
        _sut.Revalidate();

        // Assert
        Assert.Single(_sut.CurrentResults);
        Assert.False(_sut.HasBlockingErrors);
        Assert.True(_sut.HasWarnings);
        Assert.True(_sut.HasResults);
    }

    [Fact]
    public void Revalidate_WithBlockingErrors_SetsHasBlockingErrors()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        var expectedResults = new List<FileValidationResult>
        {
            new(ValidationSeverity.Error, "file1.mkv", "Track count mismatch")
        };
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(expectedResults);

        // Act
        _sut.Revalidate();

        // Assert
        Assert.True(_sut.HasBlockingErrors);
        Assert.True(_sut.HasResults);
    }

    [Fact]
    public void Revalidate_FiresStateChangedEvent()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>());

        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act
        _sut.Revalidate();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Revalidate_WithEmptyFileList_FiresStateChanged()
    {
        // Arrange — results are already empty (default state)
        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act
        _sut.Revalidate();

        // Assert — StateChanged always fires to ensure consistent behavior for file-list changes
        Assert.True(eventFired);
    }

    [Fact]
    public void Revalidate_ClearsPreviousResults_WhenFileListBecomesEmpty()
    {
        // Arrange — first populate with results
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
            });
        _sut.Revalidate();
        Assert.True(_sut.HasResults);

        // Clear the file list and subscribe for state change
        _fileList.Clear();
        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act — explicitly revalidate after file list becomes empty
        _sut.Revalidate();

        // Assert
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasResults);
        Assert.True(eventFired);
    }



    [Fact]
    public void Revalidate_UsesCurrentEffectiveSettings()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        var customSettings = new BatchValidationSettings
        {
            Mode = StrictnessMode.Custom
        };
        _validationSettingsService.GetEffectiveSettings(Arg.Any<UserSettings>())
            .Returns(customSettings);
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>());

        // Act
        _sut.Revalidate();

        // Assert — verify the correct settings were passed
        _validationEngine.Received().Validate(
            Arg.Any<IEnumerable<ScannedFileInfo>>(),
            Arg.Is<BatchValidationSettings>(s => s.Mode == StrictnessMode.Custom));
    }

    [Fact]
    public void Revalidate_WithMixedResults_SetsPropertiesCorrectly()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Error, "file1.mkv", "Track count mismatch"),
                new(ValidationSeverity.Warning, "file1.mkv", "Language inconsistency"),
                new(ValidationSeverity.Info, "file1.mkv", "Info message")
            });

        // Act
        _sut.Revalidate();

        // Assert
        Assert.Equal(3, _sut.CurrentResults.Count);
        Assert.True(_sut.HasBlockingErrors);
        Assert.True(_sut.HasWarnings);
        Assert.True(_sut.HasResults);
    }



    [Fact]
    public void Revalidate_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.Revalidate());
    }

    [Fact]
    public async Task RevalidateAsync_WithEmptyFileList_HasNoResults()
    {
        // Act
        await _sut.RevalidateAsync();

        // Assert
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasBlockingErrors);
        Assert.False(_sut.HasWarnings);
        Assert.False(_sut.HasResults);
    }

    [Fact]
    public async Task RevalidateAsync_WithFiles_RunsValidationEngine()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        var expectedResults = new List<FileValidationResult>
        {
            new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
        };
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(expectedResults);

        // Act
        await _sut.RevalidateAsync();

        // Assert
        Assert.Single(_sut.CurrentResults);
        Assert.False(_sut.HasBlockingErrors);
        Assert.True(_sut.HasWarnings);
        Assert.True(_sut.HasResults);
    }

    [Fact]
    public async Task RevalidateAsync_WithBlockingErrors_SetsHasBlockingErrors()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        var expectedResults = new List<FileValidationResult>
        {
            new(ValidationSeverity.Error, "file1.mkv", "Track count mismatch")
        };
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(expectedResults);

        // Act
        await _sut.RevalidateAsync();

        // Assert
        Assert.True(_sut.HasBlockingErrors);
        Assert.True(_sut.HasResults);
    }

    [Fact]
    public async Task RevalidateAsync_FiresStateChangedEvent()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>());

        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act
        await _sut.RevalidateAsync();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public async Task RevalidateAsync_WithEmptyFileList_FiresStateChanged()
    {
        // Arrange — results are already empty (default state)
        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act
        await _sut.RevalidateAsync();

        // Assert — StateChanged always fires to ensure consistent behavior for file-list changes
        Assert.True(eventFired);
    }

    [Fact]
    public async Task RevalidateAsync_ClearsPreviousResults_WhenFileListBecomesEmpty()
    {
        // Arrange — first populate with results
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
            });
        await _sut.RevalidateAsync();
        Assert.True(_sut.HasResults);

        // Clear the file list and subscribe for state change
        _fileList.Clear();
        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act — explicitly revalidate after file list becomes empty
        await _sut.RevalidateAsync();

        // Assert
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasResults);
        Assert.True(eventFired);
    }

    [Fact]
    public async Task RevalidateAsync_UsesCurrentEffectiveSettings()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        var customSettings = new BatchValidationSettings
        {
            Mode = StrictnessMode.Custom
        };
        _validationSettingsService.GetEffectiveSettings(Arg.Any<UserSettings>())
            .Returns(customSettings);
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>());

        // Act
        await _sut.RevalidateAsync();

        // Assert — verify the correct settings were passed
        _validationEngine.Received().Validate(
            Arg.Any<IEnumerable<ScannedFileInfo>>(),
            Arg.Is<BatchValidationSettings>(s => s.Mode == StrictnessMode.Custom));
    }

    [Fact]
    public async Task RevalidateAsync_WithMixedResults_SetsPropertiesCorrectly()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Error, "file1.mkv", "Track count mismatch"),
                new(ValidationSeverity.Warning, "file1.mkv", "Language inconsistency"),
                new(ValidationSeverity.Info, "file1.mkv", "Info message")
            });

        // Act
        await _sut.RevalidateAsync();

        // Assert
        Assert.Equal(3, _sut.CurrentResults.Count);
        Assert.True(_sut.HasBlockingErrors);
        Assert.True(_sut.HasWarnings);
        Assert.True(_sut.HasResults);
    }

    [Fact]
    public async Task RevalidateAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _sut.RevalidateAsync());
    }

    private static ScannedFileInfo CreateScannedFile(string path)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Audio);
        return new ScannedFileInfo(builder.Build(), path);
    }
}
