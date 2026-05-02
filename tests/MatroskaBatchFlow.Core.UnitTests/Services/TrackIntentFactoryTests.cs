using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

/// <summary>
/// Contains unit tests for the TrackIntentFactory class, verifying correct creation of
/// TrackIntent objects from scanned track information.
/// </summary>
public class TrackIntentFactoryTests
{
    private readonly ILanguageProvider _mockLanguageProvider = Substitute.For<ILanguageProvider>();

    public TrackIntentFactoryTests()
    {
        // Default: any unmatched code resolves to Undetermined.
        _mockLanguageProvider.Resolve(Arg.Any<string?>()).Returns(MatroskaLanguageOption.Undetermined);
    }

    [Fact]
    public void Create_SetsBasicProperties()
    {
        // Arrange
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithTitle("English Commentary")
            .WithDefault(true)
            .WithForced(false)
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Audio, 0);

        // Assert
        Assert.Equal(TrackType.Audio, result.Type);
        Assert.Equal(0, result.Index);
        Assert.Equal("English Commentary", result.Name);
        Assert.True(result.Default);
        Assert.False(result.Forced);
        Assert.Same(trackInfo, result.ScannedTrackInfo);
    }

    [Fact]
    public void Create_SetsEmptyNameWhenTitleIsNull()
    {
        // Arrange
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Video)
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Video, 0);

        // Assert
        Assert.Equal(string.Empty, result.Name);
    }

    [Fact]
    public void Create_DelegatesToResolveAndUsesResult()
    {
        // Arrange
        var englishLanguage = new MatroskaLanguageOption(
            name: "English",
            iso639_1: "en",
            iso639_2_b: "eng",
            iso639_2_t: "eng",
            iso639_3: "eng");
        _mockLanguageProvider.Resolve("eng").Returns(englishLanguage);
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("eng")
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Audio, 0);

        // Assert
        Assert.Same(englishLanguage, result.Language);
    }

    [Fact]
    public void Create_PassesLanguageCodeToResolve()
    {
        // Arrange
        var japaneseLanguage = new MatroskaLanguageOption(
            name: "Japanese",
            iso639_1: "ja",
            iso639_2_b: "jpn",
            iso639_2_t: "jpn",
            iso639_3: "jpn");
        _mockLanguageProvider.Resolve("ja").Returns(japaneseLanguage);
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("ja")
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Audio, 0);

        // Assert
        Assert.Same(japaneseLanguage, result.Language);
    }

    [Fact]
    public void Create_ReturnsUndeterminedForUnknownLanguage()
    {
        // Arrange
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("xyz")
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Audio, 0);

        // Assert
        Assert.Same(MatroskaLanguageOption.Undetermined, result.Language);
    }

    [Fact]
    public void Create_ReturnsUndeterminedForEmptyLanguage()
    {
        // Arrange
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage(string.Empty)
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Audio, 0);

        // Assert
        Assert.Same(MatroskaLanguageOption.Undetermined, result.Language);
    }

    [Fact]
    public void Create_ReturnsUndeterminedForWhitespaceLanguage()
    {
        // Arrange
        var factory = new TrackIntentFactory(_mockLanguageProvider);
        var trackInfo = new TrackInfoBuilder()
            .WithType(TrackType.Audio)
            .WithLanguage("   ")
            .WithStreamKindID(0)
            .Build();

        // Act
        var result = factory.Create(trackInfo, TrackType.Audio, 0);

        // Assert
        Assert.Same(MatroskaLanguageOption.Undetermined, result.Language);
    }

    [Fact]
    public void Create_ThrowsForNullTrackInfo()
    {
        // Arrange
        var factory = new TrackIntentFactory(_mockLanguageProvider);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!, TrackType.Audio, 0));
    }
}
