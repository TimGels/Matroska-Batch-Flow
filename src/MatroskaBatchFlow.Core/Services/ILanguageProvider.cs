using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

public interface ILanguageProvider
{
    public ImmutableList<MatroskaLanguageOption> Languages { get; }

    public void LoadLanguages();

    /// <summary>
    /// Resolves a raw language string from MediaInfo into a typed <see cref="MatroskaLanguageOption"/>.
    /// </summary>
    /// <param name="languageCode">The language code (e.g., "en", "eng", or "English").</param>
    /// <returns>The matching language option, or <see cref="MatroskaLanguageOption.Undetermined"/> if no match found.</returns>
    MatroskaLanguageOption Resolve(string? languageCode);
}
