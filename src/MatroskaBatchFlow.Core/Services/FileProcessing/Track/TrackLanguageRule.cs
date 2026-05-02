using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

/// <summary>
/// Analyzes per-file track languages and populates global TrackIntent properties with smart defaults.
/// This rule determines what language to display in the UI based on all scanned files.
/// </summary>
public class TrackLanguageRule(ILanguageProvider languageProvider) : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);
        ArgumentNullException.ThrowIfNull(batchConfig);

        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsMatroskaTrackElement()))
        {
            var globalTracks = batchConfig.GetTrackListForType(trackType);

            for (int i = 0; i < globalTracks.Count; i++)
            {
                // Resolve raw MediaInfo values before grouping so aliases such as "en" and "eng"
                // are counted as the same canonical language.
                var languages = batchConfig.FileList
                    .Select(f => f.GetTracks(trackType))
                    .Where(tracks => i < tracks.Count)
                    .Select(tracks => languageProvider.Resolve(tracks[i].Language))
                    .ToList();

                if (languages.Count == 0)
                    continue;

                globalTracks[i].Language = DetermineMostCommonLanguage(languages);
            }
        }
    }

    /// <summary>
    /// Determines the most common normalized language across multiple files.
    /// </summary>
    /// <param name="languages">List of resolved languages from all files for a specific track position.</param>
    /// <returns>The most frequently occurring language, or <see cref="MatroskaLanguageOption.Undetermined"/> if there is no clear winner.</returns>
    private static MatroskaLanguageOption DetermineMostCommonLanguage(List<MatroskaLanguageOption> languages)
    {
        if (languages.Count == 0)
            return MatroskaLanguageOption.Undetermined;

        // If all languages are the same (including aliases), return that language immediately.
        if (languages.Select(l => l.Iso639_2_b).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            return languages[0];

        // Group languages by their ISO 639-2/B code and find the most common one. 
        // This handles cases where different aliases represent the same language.
        var groups = languages
            .GroupBy(l => l.Iso639_2_b, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ToList();

        // If the most common language represents a majority, return it.
        var mostCommon = groups[0];
        if (mostCommon.Count() > languages.Count / 2)
            return languages.First(l => string.Equals(l.Iso639_2_b, mostCommon.Key, StringComparison.OrdinalIgnoreCase));

        // No language has a majority, return undetermined to avoid making an arbitrary choice.
        return MatroskaLanguageOption.Undetermined;
    }
}
