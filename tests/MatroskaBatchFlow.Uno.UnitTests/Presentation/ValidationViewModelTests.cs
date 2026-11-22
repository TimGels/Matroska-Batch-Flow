using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Core.Utilities;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Presentation;

public class ValidationViewModelTests
{
    private readonly IBatchConfiguration _batch = Substitute.For<IBatchConfiguration>();
    private readonly IValidationStateService _validation = Substitute.For<IValidationStateService>();
    private readonly ILanguageProvider _languages = Substitute.For<ILanguageProvider>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IBatchOperationOrchestrator _orchestrator = Substitute.For<IBatchOperationOrchestrator>();
    private readonly UniqueObservableCollection<ScannedFileInfo> _files = [];

    public ValidationViewModelTests()
    {
        _batch.FileList.Returns(_files);
        _validation.CurrentResults.Returns(Array.Empty<FileValidationResult>());
        _languages.Resolve(Arg.Any<string>()).Returns(MatroskaLanguageOption.Undetermined);
        _files.Add(new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "first.mkv"));
    }

    [Fact]
    public void Constructor_SelectsHighestPriorityFindingFromCurrentState()
    {
        _validation.CurrentResults.Returns(new[]
        {
            new FileValidationResult(ValidationSeverity.Warning, "first.mkv", "Review this."),
            new FileValidationResult(ValidationSeverity.Error, "first.mkv", "Cannot process this.")
        });

        using var viewModel = CreateViewModel();

        Assert.Equal(2, viewModel.FileChecks.Groups.Count);
        Assert.True(viewModel.SelectedCheck!.IsBlocking);
        Assert.Same(viewModel.FileChecks.Groups[0], viewModel.SelectedCheck);
        Assert.False(viewModel.ReviewTrackSettingsCommand.CanExecute(null));
    }

    [Fact]
    public void Revalidation_RemovesStaleSelectionAndShowsCorrectEmptyState()
    {
        _validation.CurrentResults.Returns(new[]
        {
            new FileValidationResult(ValidationSeverity.Warning, "first.mkv", "Review this.")
        });
        using var viewModel = CreateViewModel();
        Assert.True(viewModel.HasSelectedCheck);

        _validation.CurrentResults.Returns(Array.Empty<FileValidationResult>());
        _validation.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        Assert.Null(viewModel.SelectedCheck);
        Assert.True(viewModel.HasNoFindings);
        Assert.Equal(1, viewModel.FileChecks.FileCount);

        _files.Clear();
        _validation.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        Assert.Equal(0, viewModel.FileChecks.FileCount);
    }

    [Fact]
    public void OnNavigatedTo_FromFileIndicator_ShowsOnlyRelevantChecksUntilScopeCleared()
    {
        _validation.CurrentResults.Returns(new[]
        {
            new FileValidationResult(ValidationSeverity.Error, "first.mkv", "First finding."),
            new FileValidationResult(ValidationSeverity.Warning, "second.mkv", "Second finding.")
        });
        using var viewModel = CreateViewModel();

        viewModel.OnNavigatedTo(new FileCheckVisit("second.mkv"));

        Assert.Equal("second.mkv", Assert.Single(viewModel.SelectedCheck!.Files).FilePath);
        Assert.Single(viewModel.VisibleGroups);
        Assert.Equal(ValidationSeverity.Warning, Assert.Single(viewModel.CheckSections).Topic.Severity);
        viewModel.ShowAllChecksCommand.Execute(null);
        Assert.Equal(2, viewModel.VisibleGroups.Count);
        Assert.Equal([(ValidationSeverity.Error, 1), (ValidationSeverity.Warning, 1)],
            viewModel.CheckSections.Select(section => (section.Topic.Severity, section.CategoryCount)));
    }

    [Fact]
    public void FileScopedComparison_KeepsFileValueAndLiveBatchDistributionWhenCountsChange()
    {
        _files.Add(new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "second.mkv"));
        var result = new FileValidationResult(ValidationSeverity.Error, "second.mkv", "Track count difference")
        {
            CheckKind = ValidationCheckKind.TrackCount,
            Comparisons = [new(TrackType.Audio, null, [new("first.mkv", "2"), new("second.mkv", "1")])]
        };
        _validation.CurrentResults.Returns([result]);
        using var vm = CreateViewModel();
        vm.OnNavigatedTo(new FileCheckVisit("first.mkv"));

        Assert.Equal("2", vm.FocusedFile!.Evidence.RawValue);
        Assert.Equal([("2", 1), ("1", 1)], vm.ComparisonRows.Select(row => (row.RawValue, row.Files.Count)));

        _files.Add(new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "third.mkv"));
        _validation.CurrentResults.Returns([result with
        {
            Comparisons = [new(TrackType.Audio, null, [new("first.mkv", "2"), new("second.mkv", "1"), new("third.mkv", "1")])]
        }]);
        _validation.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);

        Assert.Equal("first.mkv", Assert.Single(vm.VisibleFiles).FilePath);
        Assert.Equal([("2", 1), ("1", 2)], vm.ComparisonRows.Select(row => (row.RawValue, row.Files.Count)));
        vm.ShowAllChecksCommand.Execute(null);
        Assert.Null(vm.FocusedFile);
        Assert.Equal(3, vm.VisibleFiles.Count);
    }

    [Fact]
    public void BackRestoresEarlierVisitWhileFreshReviewStartsAtHighestPriority()
    {
        _validation.CurrentResults.Returns(new[]
        {
            new FileValidationResult(ValidationSeverity.Error, "first.mkv", "First finding."),
            new FileValidationResult(ValidationSeverity.Warning, "second.mkv", "Second finding.")
        });
        using var viewModel = CreateViewModel();
        var visit = new FileCheckVisit("second.mkv");
        viewModel.OnNavigatedTo(visit);
        viewModel.SelectedCheck = viewModel.FileChecks.Groups[1];
        var selected = viewModel.SelectedCheck;

        viewModel.OnNavigatedFrom();
        viewModel.OnNavigatedTo(new FileCheckVisit());
        Assert.True(viewModel.SelectedCheck!.IsBlocking);
        Assert.False(viewModel.IsFileScoped);
        viewModel.OnNavigatedTo(visit);

        Assert.True(viewModel.IsFileScoped);
        Assert.Same(selected, viewModel.SelectedCheck);
    }

    [Theory]
    [InlineData(TrackType.Audio, typeof(AudioViewModel))]
    [InlineData(TrackType.Video, typeof(VideoViewModel))]
    [InlineData(TrackType.Text, typeof(SubtitleViewModel))]
    public void ReviewTrackSettings_NavigatesToTheSelectedTrackType(TrackType trackType, Type targetViewModel)
    {
        _validation.CurrentResults.Returns(new[]
        {
            new FileValidationResult(ValidationSeverity.Warning, "second.mkv", "Different language")
            {
                CheckKind = ValidationCheckKind.Language,
                Comparisons = [new(trackType, 1, [new("first.mkv", "eng"), new("second.mkv", "jpn")])]
            }
        });
        using var viewModel = CreateViewModel();

        Assert.True(viewModel.ReviewTrackSettingsCommand.CanExecute(null));
        viewModel.ReviewTrackSettingsCommand.Execute(null);

        _navigation.Received(1).NavigateTo(targetViewModel.FullName!, new TrackCheckInspection(1, false));
        Assert.True(viewModel.FileChecks.HasFindings);
    }

    [Fact]
    public void ExpandValues_RestoresMissingTagsAndAbsentSlotsIndependentlyAgainstLiveFiles()
    {
        foreach (var path in new[] { "one.mkv", "two.mkv", "three.mkv", "four.mkv", "five.mkv" })
            _files.Add(new ScannedFileInfo(new MediaInfoResultBuilder().Build(), path));
        _validation.CurrentResults.Returns(new[] {
            new FileValidationResult(ValidationSeverity.Error, "four.mkv", "Language difference") {
                CheckKind = ValidationCheckKind.Language,
                Comparisons = [new(TrackType.Audio, 1, [new("one.mkv", "ja"), new("two.mkv", "hi"),
                    new("three.mkv", "en"), new("four.mkv", ""), new("five.mkv", "")])]
            }
        });
        using var vm = CreateViewModel();
        var visit = new FileCheckVisit();
        vm.OnNavigatedTo(visit);
        var unspecified = vm.ComparisonRows.Single(row => row.RawValue == string.Empty);
        var absent = vm.ComparisonRows.Single(row => row.RawValue is null);
        Assert.Equal(["four.mkv", "five.mkv"], unspecified.Files.Select(file => file.FilePath));
        Assert.Equal("first.mkv", Assert.Single(absent.Files).FilePath);
        unspecified.IsExpanded = true;
        absent.IsExpanded = true;
        vm.ReviewTrackSettingsCommand.Execute(null);
        _files.Add(new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "new.mkv"));
        _validation.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
        vm.OnNavigatedTo(visit);
        Assert.Equal([string.Empty, null], vm.ComparisonRows.Where(row => row.IsExpanded).Select(row => row.RawValue));
        Assert.Equal(["first.mkv", "new.mkv"], vm.ComparisonRows.Single(row => row.RawValue is null).Files.Select(file => file.FilePath));
        vm.OnNavigatedTo(new FileCheckVisit("two.mkv"));
        Assert.Equal("hi", Assert.Single(vm.VisibleFiles).Evidence.RawValue);
        Assert.DoesNotContain(vm.ComparisonRows, row => row.IsExpanded);
        vm.ReviewTrackSettingsCommand.Execute(null);
        _navigation.Received().NavigateTo(typeof(AudioViewModel).FullName!, new TrackCheckInspection(1, true));
    }

    [Fact]
    public void Sidebar_GroupsTrackNumbersByPropertyWithinSeverity()
    {
        FileValidationResult Check(int position, ValidationCheckKind kind, ValidationSeverity severity) =>
            new(severity, "second.mkv", "Difference")
            {
                CheckKind = kind,
                Comparisons = [new(TrackType.Text, position, [new("first.mkv", "en"), new("second.mkv", "pt")])]
            };
        _validation.CurrentResults.Returns([
            Check(12, ValidationCheckKind.Language, ValidationSeverity.Error),
            Check(2, ValidationCheckKind.Language, ValidationSeverity.Error),
            Check(4, ValidationCheckKind.DefaultFlag, ValidationSeverity.Error),
            Check(16, ValidationCheckKind.Language, ValidationSeverity.Info)
        ]);
        using var vm = CreateViewModel();

        var sections = vm.CheckSections;
        Assert.Equal([(ValidationSeverity.Error, 3), (ValidationSeverity.Info, 1)],
            sections.Where(section => section.ShowHeading).Select(section => (section.Topic.Severity, section.CategoryCount)));
        Assert.Equal([ValidationCheckKind.Language, ValidationCheckKind.DefaultFlag, ValidationCheckKind.Language],
            sections.Select(section => section.Topic.Kind));
        Assert.Equal([2, 12], sections[0].Select(check => check.TrackPosition));
        Assert.Equal(16, Assert.Single(sections[2]).TrackPosition);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemoveUnsupported_OnlyRemovesUnsupportedFilesInScope(bool fileScoped)
    {
        var unsupported = new[] { "one.mp4", "two.mp4" };
        foreach (string path in unsupported)
            _files.Add(new ScannedFileInfo(new MediaInfoResultBuilder().Build(), path));
        _validation.CurrentResults.Returns(unsupported.Select(path =>
            new FileValidationResult(ValidationSeverity.Error, path, "Unsupported extension")
                { CheckKind = ValidationCheckKind.FileFormat }).ToArray());
        using var vm = CreateViewModel();
        if (fileScoped)
        {
            vm.OnNavigatedTo(new FileCheckVisit(unsupported[1]));
        }
        _orchestrator.RemoveFilesAsync(Arg.Any<IEnumerable<ScannedFileInfo>>()).Returns(call =>
        {
            foreach (var file in call.Arg<IEnumerable<ScannedFileInfo>>())
                _files.Remove(file);
            _validation.CurrentResults.Returns(_files.Where(file => unsupported.Contains(file.Path))
                .Select(file => new FileValidationResult(ValidationSeverity.Error, file.Path, "Unsupported extension")
                    { CheckKind = ValidationCheckKind.FileFormat }).ToArray());
            _validation.StateChanged += Raise.Event<EventHandler>(this, EventArgs.Empty);
            return Task.CompletedTask;
        });

        await vm.RemoveUnsupportedCommand.ExecuteAsync(null);

        string[] expected = fileScoped ? [unsupported[1]] : unsupported;
        await _orchestrator.Received(1).RemoveFilesAsync(Arg.Is<IEnumerable<ScannedFileInfo>>(
            files => files.Select(file => file.Path).SequenceEqual(expected)));
        Assert.False(vm.IsFileScoped);
        Assert.Equal(!fileScoped, vm.HasNoFindings);
        Assert.Equal(fileScoped ? 1 : 2, vm.RemovedFileCount);
    }

    private ValidationViewModel CreateViewModel() => new(_batch, _validation, _languages, _navigation, _orchestrator);
}
