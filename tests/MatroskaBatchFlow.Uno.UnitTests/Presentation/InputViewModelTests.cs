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
[Collection(MessengerCollectionDefinition.Name)]
public class InputViewModelTests : IDisposable
{
    private readonly IFileListAdapter _fileListAdapter;
    private readonly IBatchConfiguration _batchConfiguration;
    private readonly IFilePickerDialogService _filePickerDialogService;
    private readonly IWritableSettings<UserSettings> _userSettings;
    private readonly IValidationStateService _validationStateService;
    private readonly IInputOperationFeedbackService _inputOperationFeedbackService;
    private readonly IBatchOperationOrchestrator _orchestrator;
    private readonly ILanguageProvider _languageProvider;
    private readonly INavigationService _navigationService;
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
        _languageProvider = Substitute.For<ILanguageProvider>();
        _navigationService = Substitute.For<INavigationService>();
        _logger = Substitute.For<ILogger<InputViewModel>>();

        _fileList = [];

        _batchConfiguration.FileList.Returns(_fileList);
        _fileListAdapter.ScannedFileViewModels.Returns(new ObservableCollection<ScannedFileViewModel>());
        _validationStateService.CurrentResults.Returns(Array.Empty<FileValidationResult>());
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
        viewModel.SelectedFiles.Add(new ScannedFileViewModel(file));

        // Act
        var canSelectAll = viewModel.CanSelectAll;

        // Assert
        Assert.False(canSelectAll);
    }

    [Fact]
    public void ReviewFileChecks_NavigatesWithoutDiscardingBlockingFindings()
    {
        _fileList.Add(CreateScannedFile("test.mkv"));
        _validationStateService.CurrentResults.Returns(
            new[] { new FileValidationResult(ValidationSeverity.Error, "test.mkv", "Test error") });
        using var viewModel = CreateViewModel();

        viewModel.ReviewFileChecksCommand.Execute(null);

        _navigationService.Received(1).NavigateTo(typeof(FileChecksViewModel).FullName!,
            Arg.Is<FileCheckVisit>(visit => visit.FilePath == null));
        Assert.True(viewModel.FileChecks.HasBlockingIssues);
        Assert.Single(viewModel.FileChecks.Groups);
    }

    [Fact]
    public void ReviewFileChecks_FromFileIndicator_PassesFilePathWithoutChangingSelection()
    {
        var selectedFile = new ScannedFileViewModel(CreateScannedFile("selected.mkv"));
        var reviewedFile = new ScannedFileViewModel(CreateScannedFile("reviewed.mkv"));
        using var viewModel = CreateViewModel();
        viewModel.SelectedFiles.Add(selectedFile);

        viewModel.ReviewFileChecksCommand.Execute(reviewedFile);

        _navigationService.Received(1).NavigateTo(typeof(FileChecksViewModel).FullName!,
            Arg.Is<FileCheckVisit>(visit => visit.FilePath == "reviewed.mkv"));
        Assert.Same(selectedFile, Assert.Single(viewModel.SelectedFiles));
    }

    [Theory]
    [InlineData(ValidationSeverity.Error, true)]
    [InlineData(ValidationSeverity.Warning, false)]
    public void FileChecks_InitialState_ReflectsCurrentValidation(ValidationSeverity severity, bool blocks)
    {
        _fileList.Add(CreateScannedFile("test.mkv"));
        _validationStateService.CurrentResults.Returns(
            new[] { new FileValidationResult(severity, "test.mkv", "Test finding") });

        using var viewModel = CreateViewModel();

        Assert.True(viewModel.FileChecks.HasFindings);
        Assert.True(viewModel.FileChecks.IsVisible);
        Assert.Equal(blocks, viewModel.FileChecks.HasBlockingIssues);
        Assert.Equal(severity, Assert.Single(viewModel.FileChecks.Groups).Severity);
    }

    [Fact]
    public void FileChecks_CountsComparisonParticipantsAndMarksOnlyUnsupportedInputs()
    {
        foreach (string path in new[] { "one.mkv", "two.mkv", "clip.mp4" })
        {
            var file = CreateScannedFile(path);
            _fileList.Add(file);
            _fileListAdapter.ScannedFileViewModels.Add(new ScannedFileViewModel(file));
        }
        _validationStateService.CurrentResults.Returns(new[] {
            new FileValidationResult(ValidationSeverity.Error, "two.mkv", "Track counts differ") {
                CheckKind = ValidationCheckKind.TrackCount,
                Comparisons = [new(TrackType.Audio, null, [new("one.mkv", "1"), new("two.mkv", "2")])]
            },
            new FileValidationResult(ValidationSeverity.Error, "clip.mp4", "Unsupported extension")
                { CheckKind = ValidationCheckKind.FileFormat }
        });

        using var vm = CreateViewModel();

        Assert.Equal([1, 1, 1], vm.FileList.Select(file => file.FileCheckCount));
        Assert.Equal("clip.mp4", Assert.Single(vm.FileList, file => file.IsUnsupported).FileName);
    }

    [Fact]
    public void FileChecks_Revalidation_UpdatesFileIndicatorsWithoutChangingSelection()
    {
        var file = CreateScannedFile("test.mkv");
        _fileList.Add(file);
        var fileViewModel = new ScannedFileViewModel(file);
        _fileListAdapter.ScannedFileViewModels.Add(fileViewModel);
        using var viewModel = CreateViewModel();
        viewModel.SelectedFiles.Add(fileViewModel);
        _validationStateService.CurrentResults.Returns(
            new[] { new FileValidationResult(ValidationSeverity.Warning, file.Path, "Test finding") });

        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        Assert.True(fileViewModel.HasFileChecks);
        Assert.Equal(1, fileViewModel.FileCheckCount);
        Assert.Single(viewModel.SelectedFiles);

        _validationStateService.CurrentResults.Returns(Array.Empty<FileValidationResult>());
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        Assert.False(fileViewModel.HasFileChecks);
        Assert.Empty(viewModel.FileChecks.Groups);
        Assert.Single(viewModel.SelectedFiles);

        _fileList.Clear();
        _validationStateService.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        Assert.False(viewModel.FileChecks.IsVisible);
    }

    [Fact]
    public async Task RemoveSelected_WithSelectedFiles_DelegatesToOrchestrator()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var file = CreateScannedFile("file1.mkv");
        var fileVm = new ScannedFileViewModel(file);
        viewModel.SelectedFiles.Add(fileVm);

        // Act
        await ((IAsyncRelayCommand)viewModel.RemoveSelected).ExecuteAsync(null);

        // Assert
        await _orchestrator.Received(1).RemoveFilesAsync(Arg.Is<IEnumerable<ScannedFileInfo>>(
            files => files.Contains(file)));
    }

    [Fact]
    public async Task RemoveFile_FromRowMenu_RemovesOnlyThatFileThroughOrchestrator()
    {
        var selected = new ScannedFileViewModel(CreateScannedFile("selected.mkv"));
        var clicked = new ScannedFileViewModel(CreateScannedFile("clip.mp4"));
        using var viewModel = CreateViewModel();
        viewModel.SelectedFiles.Add(selected);

        await viewModel.RemoveFileCommand.ExecuteAsync(clicked);

        await _orchestrator.Received(1).RemoveFilesAsync(Arg.Is<IEnumerable<ScannedFileInfo>>(
            files => files.Single() == clicked.FileInfo));
    }

    [Fact]
    public void SelectAll_AddsAllScannedFileViewModelsToSelectedFiles()
    {
        // Arrange
        var file1 = CreateScannedFile("file1.mkv");
        var file2 = CreateScannedFile("file2.mkv");
        var vm1 = new ScannedFileViewModel(file1);
        var vm2 = new ScannedFileViewModel(file2);
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
            _languageProvider,
            _navigationService,
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
