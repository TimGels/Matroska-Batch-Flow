using System.Collections.Immutable;
using System.Text.Json;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services;

public partial class LanguageProvider : ILanguageProvider
{
    private readonly LanguageOptions _options;
    private readonly ILogger<LanguageProvider> _logger;
    public ImmutableList<MatroskaLanguageOption> Languages { get; private set; } = [];

    public LanguageProvider(IOptions<LanguageOptions> options, ILogger<LanguageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        LoadLanguages();
    }

    // Helper methods
    private static ImmutableList<MatroskaLanguageOption> LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var documentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var serializerOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        using var doc = JsonDocument.Parse(json, documentOptions);
        var languagesElement = doc.RootElement.GetProperty("languages");
        return JsonSerializer.Deserialize<ImmutableList<MatroskaLanguageOption>>(languagesElement.GetRawText(), serializerOptions)!;
    }

    public void LoadLanguages()
    {
        try
        {
            var userFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _options.FilePath);
            if (File.Exists(userFile))
            {
                Languages = LoadFromFile(userFile);
                LogLanguagesLoaded(Languages.Count, userFile);
                return;
            }

            LogLanguageFileNotFound(userFile);
            //// Fallback to embedded
            //var assembly = Assembly.GetExecutingAssembly();
            //using var stream = assembly.GetManifestResourceStream(
            //    $"YourApp.Resources.{_config["LanguageSettings:FallbackPath"]}");

            //_languages = JsonSerializer.Deserialize<List<Language>>(stream)!
            //    .ToImmutableDictionary(x => x.Code);
        }
        catch (Exception ex)
        {
            LogLanguageLoadFailed(ex, _options.FilePath);
            Languages = [];
        }
    }
}
