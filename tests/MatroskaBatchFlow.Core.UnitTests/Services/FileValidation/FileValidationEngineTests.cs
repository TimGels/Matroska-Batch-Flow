using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

public class FileValidationEngineTests
{
    private readonly FileValidationEngine _engine = new(new FileFormatValidationRule(),
        [new TrackCountConsistencyRule(), new LanguageConsistencyRule()], NullLogger<FileValidationEngine>.Instance);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_MixedBatch_OnlyComparesSupportedFiles(bool invalidFirst)
    {
        var invalid = File("other.mp4", "MPEG-4", "hi", "en");
        var first = File("one.mkv", "Matroska", "ja");
        var second = File("two.MKV", "matroska", "en");
        var settings = new BatchValidationSettings();
        settings.CustomSettings.AudioTrackValidation.Language = ValidationSeverity.Error;
        var results = _engine.Validate(invalidFirst ? [invalid, first, second] : [first, second, invalid], settings).ToArray();

        Assert.Collection(results,
            format =>
            {
                Assert.Equal(ValidationCheckKind.FileFormat, format.CheckKind);
                Assert.True(format.IsBlocking);
                Assert.Equal(invalid.Path, format.ValidatedFilePath);
                Assert.Equal(new UnsupportedFormatIssue(UnsupportedFormatKind.WrongExtension, ".mp4"), format.FormatIssue);
            },
            language =>
            {
                Assert.Equal(ValidationCheckKind.Language, language.CheckKind);
                Assert.True(language.IsBlocking);
                Assert.Equal([first.Path, second.Path], Assert.Single(language.Comparisons).Files.Select(f => f.FilePath));
            });
    }

    [Fact]
    public void Validate_NoSupportedFiles_ReportsReasonsEvenInLenientMode()
    {
        var settings = new BatchValidationSettings { Mode = StrictnessMode.Lenient };
        new ValidationSettingsService().ApplyPreset(settings.CustomSettings, settings.Mode);
        var results = _engine.Validate([File("renamed.mkv", "MPEG-4", "hi"), File("unknown.mkv", null)], settings).ToArray();

        Assert.Equal(2, results.Length);
        Assert.All(results, result => Assert.True(result.IsBlocking && result.CheckKind == ValidationCheckKind.FileFormat));
        Assert.Equal(new UnsupportedFormatIssue(UnsupportedFormatKind.WrongContainer, "MPEG-4"), results[0].FormatIssue);
        Assert.Equal(new UnsupportedFormatIssue(UnsupportedFormatKind.UnknownContainer), results[1].FormatIssue);
    }

    private static ScannedFileInfo File(string path, string? format, params string[] languages)
    {
        var builder = new MediaInfoResultBuilder();
        if (format is not null)
        {
            builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.General).WithFormat(format).Build());
        }
        foreach (string language in languages)
            builder.AddTrack(new TrackInfoBuilder().WithType(TrackType.Audio).WithLanguage(language).Build());
        return new ScannedFileInfo(builder.Build(), path);
    }
}
