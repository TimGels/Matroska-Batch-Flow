using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Creates track configurations from scanned track information.
/// </summary>
/// <param name="languageProvider">The language provider for resolving track language codes.</param>
public class TrackConfigurationFactory(ILanguageProvider languageProvider) : ITrackConfigurationFactory
{
    /// <inheritdoc/>
    public TrackConfiguration Create(
        MediaInfoResult.MediaInfo.TrackInfo scannedTrackInfo,
        TrackType trackType,
        int index)
    {
        ArgumentNullException.ThrowIfNull(scannedTrackInfo);

        return new TrackConfiguration(scannedTrackInfo)
        {
            Type = trackType,
            Index = index,
            Name = scannedTrackInfo.Title ?? string.Empty,
            Language = ParseLanguageFromCode(scannedTrackInfo.Language),
            Default = scannedTrackInfo.Default,
            Forced = scannedTrackInfo.Forced
        };
    }

    /// <summary>
    /// Parses a language code from scanned track info and returns the matching <see cref="MatroskaLanguageOption"/>.
    /// </summary>
    /// <remarks>
    /// Performs case-insensitive comparison against ISO 639-1, ISO 639-2 (both bibliographic and terminologic),
    /// ISO 639-3 codes, the language name, and a custom code field.
    /// </remarks>
    /// <param name="languageCode">The language code from MediaInfo (e.g., "en", "eng", "jpn", or "English").</param>
    /// <returns>The matching language option, or <see cref="MatroskaLanguageOption.Undetermined"/> if no match found.</returns>
    private MatroskaLanguageOption ParseLanguageFromCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return MatroskaLanguageOption.Undetermined;

        var matchedLanguage = languageProvider.Languages.FirstOrDefault(lang =>
            string.Equals(lang.Iso639_2_b, languageCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_2_t, languageCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_1, languageCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_3, languageCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Name, languageCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Code, languageCode, StringComparison.OrdinalIgnoreCase));

        return matchedLanguage ?? MatroskaLanguageOption.Undetermined;
    }
}
