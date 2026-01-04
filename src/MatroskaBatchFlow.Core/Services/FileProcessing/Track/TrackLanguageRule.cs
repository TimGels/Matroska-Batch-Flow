using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

public class TrackLanguageRule(ILanguageProvider languageProvider) : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);
        ArgumentNullException.ThrowIfNull(batchConfig);

        var languages = languageProvider.Languages;

        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsMatroskaTrackElement()))
        {
            var scannedTracks = scannedFile.Result.Media.Track
               .Where(t => t.Type == trackType)
               .ToList();

            // Use the per-file track list for this scanned file
            var fileConfig = batchConfig.FileConfigurations[scannedFile.Path];
            var batchTracksForFile = fileConfig.GetTrackListForType(trackType);

            for (int i = 0; i < batchTracksForFile.Count; i++)
            {
                var scannedLanguage = scannedTracks.ElementAtOrDefault(i)?.Language;
                if (scannedLanguage is null)
                    continue;

                batchTracksForFile[i].Language = MatchLanguageOption(languages, scannedLanguage);
            }
        }
    }

    /// <summary>
    /// Matches a scanned language string to a corresponding <see cref="MatroskaLanguageOption"/> from a list of
    /// available language options.
    /// </summary>
    /// <remarks>This method performs a case-insensitive comparison of the scanned language string against
    /// various properties of each <see cref="MatroskaLanguageOption"/> in the list, including ISO 639-1, ISO 639-2
    /// (both bibliographic and terminologic), ISO 639-3 codes, the language name, and a custom code.</remarks>
    /// <param name="languages">A list of <see cref="MatroskaLanguageOption"/> objects representing the available 
    /// language options.</param> <param name="scannedLanguage">The language string to match. This can be an ISO 639-1, 
    /// ISO 639-2 (bibliographic or terminologic), ISO 639-3 code, or a language name.</param>
    /// <returns>The first <see cref="MatroskaLanguageOption"/> that matches the scanned language string, based on a
    /// case-insensitive comparison. If no match is found, returns <see cref="MatroskaLanguageOption.Undetermined"/>.
    /// </returns>
    private static MatroskaLanguageOption MatchLanguageOption(
        ImmutableList<MatroskaLanguageOption> languages,
        string scannedLanguage)
    {
        return languages
            .FirstOrDefault(lang =>
                string.Equals(lang.Iso639_2_b, scannedLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lang.Iso639_2_t, scannedLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lang.Iso639_1, scannedLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lang.Iso639_3, scannedLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lang.Name, scannedLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lang.Code, scannedLanguage, StringComparison.OrdinalIgnoreCase))
            ?? MatroskaLanguageOption.Undetermined;
    }
}
