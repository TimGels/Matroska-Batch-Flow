using System.Collections.Immutable;
using System.Text.Json;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services;

public class LanguageProvider : ILanguageProvider
{
    private readonly LanguageOptions _options;
    public ImmutableList<MatroskaLanguageOption> Languages { get; private set; } = [];

    public LanguageProvider(IOptions<LanguageOptions> options)
    {
        _options = options.Value;
        LoadLanguages();
    }

    // Helper methods
    private static ImmutableList<MatroskaLanguageOption> LoadFromFile(string path) =>
        JsonSerializer.Deserialize<ImmutableList<MatroskaLanguageOption>>(File.ReadAllText(path))!
            .ToImmutableList();

    public void LoadLanguages()
    {
        try
        {
            var userFile = AppDomain.CurrentDomain.BaseDirectory + _options.FilePath;
            if (File.Exists(userFile))
            {
                Languages = LoadFromFile(userFile);
                return;
            }

            //// Fallback to embedded
            //var assembly = Assembly.GetExecutingAssembly();
            //using var stream = assembly.GetManifestResourceStream(
            //    $"YourApp.Resources.{_config["LanguageSettings:FallbackPath"]}");

            //_languages = JsonSerializer.Deserialize<List<Language>>(stream)!
            //    .ToImmutableDictionary(x => x.Code);
        } catch (Exception)
        {
            //_logger.LogError(ex, "Language load failed");
            //_languages = ImmutableDictionary<string, Language>.Empty;
        }
    }
}
