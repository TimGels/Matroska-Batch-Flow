using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileProcessing.Track;

public class TrackLanguageRuleTests
{
    [Fact]
    public void Apply_WhenAliasesRepresentSameLanguage_UsesNormalizedMajority()
    {
        // Arrange
        var english = new MatroskaLanguageOption("English", "en", "eng", "eng", "eng");
        var japanese = new MatroskaLanguageOption("Japanese", "ja", "jpn", "jpn", "jpn");
        var batchConfig = CreateBatchConfiguration();
        var languageProvider = CreateLanguageProvider(english, japanese);

        var japaneseFile = CreateScannedFile("file1.mkv", "jpn");
        var englishBibliographicFile = CreateScannedFile("file2.mkv", "eng");
        var englishTwoLetterFile = CreateScannedFile("file3.mkv", "en");

        AddAndInitializeTracks(batchConfig, languageProvider, japaneseFile, englishBibliographicFile, englishTwoLetterFile);

        var sut = new TrackLanguageRule(languageProvider);

        // Act
        sut.Apply(japaneseFile, batchConfig);

        // Assert
        Assert.Same(english, batchConfig.AudioTracks[0].Language);
    }

    [Fact]
    public void Apply_WhenNoLanguageHasMajority_ReturnsUndetermined()
    {
        // Arrange
        var english = new MatroskaLanguageOption("English", "en", "eng", "eng", "eng");
        var japanese = new MatroskaLanguageOption("Japanese", "ja", "jpn", "jpn", "jpn");
        var batchConfig = CreateBatchConfiguration();
        var languageProvider = CreateLanguageProvider(english, japanese);

        var englishFile = CreateScannedFile("file1.mkv", "eng");
        var japaneseFile = CreateScannedFile("file2.mkv", "jpn");

        AddAndInitializeTracks(batchConfig, languageProvider, englishFile, japaneseFile);

        var sut = new TrackLanguageRule(languageProvider);

        // Act
        sut.Apply(englishFile, batchConfig);

        // Assert
        Assert.Same(MatroskaLanguageOption.Undetermined, batchConfig.AudioTracks[0].Language);
    }

    private static BatchConfiguration CreateBatchConfiguration()
    {
        var platformService = Substitute.For<IPlatformService>();
        platformService.IsWindows().Returns(true);

        var comparer = new ScannedFileInfoPathComparer(platformService);
        var logger = Substitute.For<ILogger<BatchConfiguration>>();

        return new BatchConfiguration(comparer, logger);
    }

    private static ILanguageProvider CreateLanguageProvider(MatroskaLanguageOption english, MatroskaLanguageOption japanese)
    {
        var languageProvider = Substitute.For<ILanguageProvider>();
        languageProvider.Languages.Returns(ImmutableList.Create(MatroskaLanguageOption.Undetermined, english, japanese));
        languageProvider.Resolve(Arg.Any<string?>()).Returns(callInfo =>
        {
            var code = callInfo.Arg<string?>();
            if (string.IsNullOrWhiteSpace(code))
            {
                return MatroskaLanguageOption.Undetermined;
            }

            return code.Trim().ToLowerInvariant() switch
            {
                "en" or "eng" => english,
                "ja" or "jpn" => japanese,
                _ => MatroskaLanguageOption.Undetermined
            };
        });

        return languageProvider;
    }

    private static void AddAndInitializeTracks(BatchConfiguration batchConfig, ILanguageProvider languageProvider, params ScannedFileInfo[] files)
    {
        var initializer = new BatchTrackConfigurationInitializer(batchConfig, new TrackIntentFactory(languageProvider));

        foreach (var file in files)
        {
            batchConfig.FileList.Add(file);
            initializer.Initialize(file, TrackType.Audio);
        }
    }

    private static ScannedFileInfo CreateScannedFile(string path, string languageCode)
    {
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrack(new TrackInfoBuilder()
                .WithType(TrackType.Audio)
                .WithStreamKindID(0)
                .WithLanguage(languageCode)
                .Build())
            .Build();

        return new ScannedFileInfo(mediaInfoResult, path);
    }
}
