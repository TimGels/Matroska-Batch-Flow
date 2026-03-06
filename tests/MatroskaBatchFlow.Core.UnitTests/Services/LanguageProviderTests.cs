using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

/// <summary>
/// Contains unit tests for the LanguageProvider class.
/// </summary>
public class LanguageProviderTests : IDisposable
{
    private readonly ILogger<LanguageProvider> _logger = Substitute.For<ILogger<LanguageProvider>>();
    private readonly string _testFilesDirectory = Path.Combine(Path.GetTempPath(), "LanguageProviderTests");

    public LanguageProviderTests()
    {
        Directory.CreateDirectory(_testFilesDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFilesDirectory))
        {
            Directory.Delete(_testFilesDirectory, recursive: true);
        }
    }

    [Fact]
    public void Constructor_LoadsLanguagesFromFile_WhenFileExists()
    {
        // Arrange
        var testFile = CreateTestLanguageFile("test1.json");
        var options = CreateOptions(testFile);

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.NotEmpty(provider.Languages);
        Assert.Equal(2, provider.Languages.Count);
        Assert.Contains(provider.Languages, l => l.Iso639_2_b == "eng" && l.Name == "English");
        Assert.Contains(provider.Languages, l => l.Iso639_2_b == "fre" && l.Name == "French");
    }

    [Fact]
    public void Constructor_SetsEmptyLanguages_WhenFileDoesNotExist()
    {
        // Arrange
        var options = CreateOptions("nonexistent.json");

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.Empty(provider.Languages);
    }

    [Fact]
    public void Constructor_SetsEmptyLanguages_WhenFileIsInvalid()
    {
        // Arrange
        var testFile = Path.Combine(_testFilesDirectory, "invalid.json");
        File.WriteAllText(testFile, "{ invalid json }");
        var options = CreateOptions(testFile);

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.Empty(provider.Languages);
    }

    [Fact]
    public void LoadLanguages_UpdatesLanguagesProperty()
    {
        // Arrange
        var testFile = CreateTestLanguageFile("test2.json");
        var options = CreateOptions(testFile);
        var provider = new LanguageProvider(options, _logger);
        var initialCount = provider.Languages.Count;

        // Act
        provider.LoadLanguages();

        // Assert
        Assert.Equal(initialCount, provider.Languages.Count);
    }

    [Fact]
    public void LoadLanguages_HandlesEmptyLanguagesArray()
    {
        // Arrange
        var testFile = Path.Combine(_testFilesDirectory, "empty.json");
        File.WriteAllText(testFile, @"{ ""languages"": [] }");
        var options = CreateOptions(testFile);

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.Empty(provider.Languages);
    }

    [Fact]
    public void LoadLanguages_HandlesLanguagesWithComments()
    {
        // Arrange
        var testFile = Path.Combine(_testFilesDirectory, "comments.json");
        var json = """
            {
                // Single-line comment
                "languages": [
                    /* Block comment */
                    { "name": "English", "iso639_1": "en", "iso639_2_b": "eng", "iso639_2_t": "eng", "iso639_3": "eng" }
                ]
            }
            """;
        File.WriteAllText(testFile, json);
        var options = CreateOptions(testFile);

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.Single(provider.Languages);
        Assert.Contains(provider.Languages, l => l.Iso639_2_b == "eng");
    }

    [Fact]
    public void LoadLanguages_HandlesTrailingCommas()
    {
        // Arrange
        var testFile = Path.Combine(_testFilesDirectory, "trailing_commas.json");
        var json = """
            {
                "languages": [
                    { "name": "English", "iso639_1": "en", "iso639_2_b": "eng", "iso639_2_t": "eng", "iso639_3": "eng" },
                ]
            }
            """;
        File.WriteAllText(testFile, json);
        var options = CreateOptions(testFile);

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.Single(provider.Languages);
        Assert.Contains(provider.Languages, l => l.Iso639_2_b == "eng");
    }

    [Fact]
    public void LoadLanguages_PreservesLanguageProperties()
    {
        // Arrange
        var testFile = Path.Combine(_testFilesDirectory, "full_properties.json");
        var json = @"{
            ""languages"": [
                { 
                    ""name"": ""English"",
                    ""iso639_1"": ""en"", 
                    ""iso639_2_b"": ""eng"",
                    ""iso639_2_t"": ""eng"",
                    ""iso639_3"": ""eng""
                }
            ]
        }";
        File.WriteAllText(testFile, json);
        var options = CreateOptions(testFile);

        // Act
        var provider = new LanguageProvider(options, _logger);

        // Assert
        Assert.Single(provider.Languages);
        var language = provider.Languages[0];
        Assert.Equal("en", language.Code);
        Assert.Equal("English", language.Name);
        Assert.Equal("eng", language.Iso639_2_b);
    }

    [Fact]
    public void Languages_IsImmutable()
    {
        // Arrange
        var testFile = CreateTestLanguageFile("test3.json");
        var options = CreateOptions(testFile);
        var provider = new LanguageProvider(options, _logger);

        // Act & Assert
        Assert.IsAssignableFrom<System.Collections.Immutable.ImmutableList<MatroskaLanguageOption>>(provider.Languages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ReturnsUndetermined_WhenLanguageCodeIsNullOrWhitespace(string? languageCode)
    {
        // Arrange
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve(languageCode);

        // Assert
        Assert.Equal(MatroskaLanguageOption.Undetermined, result);
    }

    [Fact]
    public void Resolve_ReturnsUndetermined_WhenNoMatchFound()
    {
        // Arrange
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve("nonexistent");

        // Assert
        Assert.Equal(MatroskaLanguageOption.Undetermined, result);
    }

    [Fact]
    public void Resolve_MatchesByIso639_2_b()
    {
        // Arrange — French has iso639_2_b "fre" (distinct from iso639_2_t "fra")
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve("fre");

        // Assert
        Assert.Equal("French", result.Name);
    }

    [Fact]
    public void Resolve_MatchesByIso639_2_t()
    {
        // Arrange — French has iso639_2_t "fra" (distinct from iso639_2_b "fre")
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve("fra");

        // Assert
        Assert.Equal("French", result.Name);
    }

    [Fact]
    public void Resolve_MatchesByIso639_1()
    {
        // Arrange
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve("en");

        // Assert
        Assert.Equal("English", result.Name);
    }

    [Fact]
    public void Resolve_MatchesByIso639_3()
    {
        // Arrange — "cmn" only appears in the ISO 639-3 field, so this isolates that match path.
        var provider = CreateProviderWithLanguageWithoutIso639_1();

        // Act
        var result = provider.Resolve("cmn");

        // Assert
        Assert.Equal("Chinese", result.Name);
    }

    [Fact]
    public void Resolve_MatchesByName()
    {
        // Arrange
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve("French");

        // Assert
        Assert.Equal("French", result.Name);
    }

    [Fact]
    public void Resolve_MatchesByCode()
    {
        // Arrange — Code is derived: iso639_1 ?? iso639_2_b, so "fr" for French.
        // "fr" also matches iso639_1 directly. Use a language with null iso639_1
        // to isolate the Code path.
        var provider = CreateProviderWithLanguageWithoutIso639_1();

        // Act — Code falls back to iso639_2_b "zho" when iso639_1 is null
        var result = provider.Resolve("zho");

        // Assert
        Assert.Equal("Chinese", result.Name);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        // Arrange
        var provider = CreateProviderWithTestLanguages();

        // Act
        var result = provider.Resolve("ENG");

        // Assert
        Assert.Equal("English", result.Name);
    }

    private LanguageProvider CreateProviderWithTestLanguages()
    {
        var testFile = CreateTestLanguageFile("resolve_test.json");
        var options = CreateOptions(testFile);
        return new LanguageProvider(options, _logger);
    }

    private LanguageProvider CreateProviderWithLanguageWithoutIso639_1()
    {
        var testFile = Path.Combine(_testFilesDirectory, "resolve_no_iso1.json");
        var json = """
            {
                "languages": [
                    { "name": "Chinese", "iso639_1": null, "iso639_2_b": "zho", "iso639_2_t": "chi", "iso639_3": "cmn" }
                ]
            }
            """;
        File.WriteAllText(testFile, json);
        var options = CreateOptions(testFile);
        return new LanguageProvider(options, _logger);
    }

    private static IOptions<LanguageOptions> CreateOptions(string filePath)
    {
        var options = Options.Create(new LanguageOptions { FilePath = filePath });
        return options;
    }

    private string CreateTestLanguageFile(string filename)
    {
        var testFile = Path.Combine(_testFilesDirectory, filename);
        var json = @"{
            ""languages"": [
                { ""name"": ""English"", ""iso639_1"": ""en"", ""iso639_2_b"": ""eng"", ""iso639_2_t"": ""eng"", ""iso639_3"": ""eng"" },
                { ""name"": ""French"", ""iso639_1"": ""fr"", ""iso639_2_b"": ""fre"", ""iso639_2_t"": ""fra"", ""iso639_3"": ""fra"" }
            ]
        }";
        File.WriteAllText(testFile, json);
        return testFile;
    }
}
