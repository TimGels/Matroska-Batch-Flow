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
    public void Revalidate_WithEmptyFileList_DoesNotFireStateChanged_WhenAlreadyEmpty()
    {
        // Arrange — results are already empty (default state)
        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act
        _sut.Revalidate();

        // Assert
        Assert.False(eventFired);
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

        // Subscribe before clearing so we capture the event
        var eventFired = false;
        _sut.StateChanged += (_, _) => eventFired = true;

        // Act — clearing the file list triggers CollectionChanged → Revalidate → clears results
        _fileList.Clear();

        // Assert
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasResults);
        Assert.True(eventFired);
    }

    [Fact]
    public void FileListCollectionChanged_TriggersRevalidation()
    {
        // Arrange
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
            });

        // Act — adding a file triggers CollectionChanged
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        // Assert — the service should have re-validated automatically
        Assert.True(_sut.HasResults);
        Assert.Single(_sut.CurrentResults);
    }

    [Fact]
    public void FileListCollectionChanged_RemovingFile_TriggersRevalidation()
    {
        // Arrange
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
            });
        _sut.Revalidate(); // populate results
        Assert.True(_sut.HasResults);

        // Act — remove file so list becomes empty
        _fileList.Remove(file);

        // Assert — results should be cleared from the CollectionChanged re-validation
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasResults);
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
    public void Dispose_UnsubscribesFromFileListChanges()
    {
        // Arrange
        _validationEngine.Validate(Arg.Any<IEnumerable<ScannedFileInfo>>(), Arg.Any<BatchValidationSettings>())
            .Returns(new List<FileValidationResult>
            {
                new(ValidationSeverity.Warning, "file1.mkv", "Test warning")
            });

        // Act — dispose, then add a file
        _sut.Dispose();
        var file = CreateScannedFile("file1.mkv");
        _fileList.Add(file);

        // Assert — validation should NOT have run since we disposed (unsubscribed)
        Assert.Empty(_sut.CurrentResults);
        Assert.False(_sut.HasResults);
    }

    [Fact]
    public void Revalidate_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.Revalidate());
    }

    private static ScannedFileInfo CreateScannedFile(string path)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Audio);
        return new ScannedFileInfo(builder.Build(), path);
    }
}
