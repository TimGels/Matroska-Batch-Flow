using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Uno.Models;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Models;

public class FileCheckSummaryTests
{
    private readonly ILanguageProvider _languages = Substitute.For<ILanguageProvider>();

    public FileCheckSummaryTests()
    {
        _languages.Resolve(Arg.Any<string>()).Returns(MatroskaLanguageOption.Undetermined);
        _languages.Resolve("eng").Returns(new MatroskaLanguageOption("English", "en", "eng", "eng", "eng"));
        _languages.Resolve("jpn").Returns(new MatroskaLanguageOption("Japanese", "ja", "jpn", "jpn", "jpn"));
    }

    [Fact]
    public void Create_RepeatedMismatches_GroupsByCheckAndCountsMatchingFiles()
    {
        var comparison = new ValidationComparison(TrackType.Audio, 2,
        [
            new(@"C:\first\episode.mkv", "eng"),
            new(@"C:\second\episode.mkv", "eng"),
            new(@"C:\third\episode.mkv", "jpn"),
            new(@"C:\fourth\episode.mkv", "jpn")
        ]);
        var results = comparison.Files.Skip(2).Select(file =>
            new FileValidationResult(ValidationSeverity.Warning, file.FilePath, $"Diagnostic for {file.FilePath}")
            {
                CheckKind = ValidationCheckKind.Language,
                Comparisons = [comparison]
            }).ToArray();

        var summary = FileCheckSummary.Create(results, 4, _languages);

        var group = Assert.Single(summary.Groups);
        Assert.Equal((TrackType.Audio, 2, ValidationCheckKind.Language), (group.TrackType, group.TrackPosition, group.Kind));
        var rows = summary.GetComparisonRows(group, comparison.Files.Select(file => file.FilePath));
        Assert.Equal([("eng", "English"), ("jpn", "Japanese")], rows.Select(row => (row.RawValue, row.Evidence.LanguageName)));
        Assert.Equal([2, 2], rows.Select(row => row.Files.Count));
        Assert.Equal(4, group.Files.Count);
        Assert.False(summary.HasBlockingIssues);
        Assert.Equal(comparison.Files.Select(file => file.FilePath), group.Files.Select(file => file.FilePath));
    }

    [Fact]
    public void Create_DifferentPositionsTypesAndSeverities_RemainSeparateWithBlockersFirst()
    {
        var results = new[]
        {
            Result(ValidationSeverity.Info, ValidationCheckKind.DefaultFlag, TrackType.Audio, 1),
            Result(ValidationSeverity.Warning, ValidationCheckKind.Language, TrackType.Audio, 1),
            Result(ValidationSeverity.Warning, ValidationCheckKind.Language, TrackType.Audio, 2),
            Result(ValidationSeverity.Error, ValidationCheckKind.Language, TrackType.Text, 1)
        };

        var summary = FileCheckSummary.Create(results, 2, _languages);

        Assert.Equal(4, summary.Groups.Count);
        Assert.True(summary.Groups[0].IsBlocking);
        Assert.True(summary.HasBlockingIssues);
        Assert.Contains(summary.Groups, group => group is { TrackType: TrackType.Text, TrackPosition: 1 });
        var flags = Assert.Single(summary.Groups, group => group.IsInformation);
        Assert.Equal(["True", "False"], flags.Files.Select(file => file.Evidence.RawValue));
    }

    [Fact]
    public void Create_SortsFormatThenLayoutThenNumericPositionsAndSeparatesMissingTracks()
    {
        var format = new FileValidationResult(ValidationSeverity.Error, "other.mp4", "Unsupported extension")
            { CheckKind = ValidationCheckKind.FileFormat };
        var summary = FileCheckSummary.Create([
            Result(ValidationSeverity.Error, ValidationCheckKind.Language, TrackType.Text, 12),
            Result(ValidationSeverity.Error, ValidationCheckKind.Language, TrackType.Text, 2),
            Result(ValidationSeverity.Error, ValidationCheckKind.TrackCount, TrackType.Audio, 1),
            format], 4, _languages);

        Assert.True(summary.Groups[0].IsFormatCheck);
        Assert.Equal(ValidationCheckKind.TrackCount, summary.Groups[1].Kind);
        Assert.Equal([2, 12], summary.Groups.Skip(2).Select(group => group.TrackPosition!.Value));
        string[] paths = ["first.mkv", "second.mkv", "absent.mkv", "other.mp4"];
        var absent = Assert.Single(summary.GetComparisonRows(summary.Groups[2], paths), row => row.RawValue is null);
        Assert.Equal("absent.mkv", Assert.Single(absent.Files).FilePath);
        Assert.DoesNotContain(summary.Groups[2].Files, file => file.FilePath == "absent.mkv");
        Assert.DoesNotContain(summary.GetComparisonRows(summary.Groups[1], paths), row => row.RawValue is null);
        Assert.Equal((1, 3, 0), (summary.UnsupportedCount, summary.BlockingCheckCount, summary.AdvisoryCheckCount));
    }

    [Fact]
    public void Create_UnknownAndMissingLanguageTags_DoesNotInventLanguageNames()
    {
        var result = Result(ValidationSeverity.Warning, ValidationCheckKind.Language, TrackType.Audio, 1) with
        {
            Comparisons =
            [
                new(TrackType.Audio, 1, [new("first.mkv", ""), new("second.mkv", "unrecognized-tag")])
            ]
        };

        var group = Assert.Single(FileCheckSummary.Create([result], 2, _languages).Groups);

        Assert.Equal(["", "unrecognized-tag"], group.Files.Select(file => file.Evidence.RawValue));
        Assert.All(group.Files, file => Assert.Null(file.Evidence.LanguageName));
    }

    [Fact]
    public void Create_UnstructuredFindings_KeepsDistinctDiagnosticsVisible()
    {
        FileValidationResult[] results =
        [
            new(ValidationSeverity.Error, "first.mkv", "Unable to inspect file."),
            new(ValidationSeverity.Error, "second.mkv", "Another diagnostic.")
        ];

        var summary = FileCheckSummary.Create(results, 2, _languages);

        Assert.Equal(2, summary.Groups.Count);
        Assert.Equal(results.Select(result => result.Message),
            summary.Groups.SelectMany(group => group.Files).Select(file => file.Evidence.Diagnostic));
    }

    [Fact]
    public void Create_DisabledChecks_DoesNotReportFindingsOrClaimPlaybackSafety()
    {
        var summary = FileCheckSummary.Create(
            [new(ValidationSeverity.Off, "first.mkv", "Disabled")], 1, _languages);

        Assert.Empty(summary.Groups);
        Assert.False(summary.HasBlockingIssues);
        Assert.False(summary.HasFindings);
        Assert.True(summary.IsVisible);
        Assert.False(FileCheckSummary.Create([], 0, _languages).IsVisible);
    }

    private static FileValidationResult Result(
        ValidationSeverity severity, ValidationCheckKind kind, TrackType type, int position) =>
        new(severity, "second.mkv", "Diagnostic")
        {
            CheckKind = kind,
            Comparisons =
            [
                new(type, position, kind == ValidationCheckKind.DefaultFlag
                    ? [new("first.mkv", "True"), new("second.mkv", "False")]
                    : [new("first.mkv", "eng"), new("second.mkv", "jpn")])
            ]
        };
}
