using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

/// <summary>
/// Analyzes per-file track languages and populates global UI properties with smart defaults.
/// Per-file configurations are already populated by <see cref="BatchTrackCountSynchronizer"/>.
/// This rule determines what language to display in the UI based on all files.
/// </summary>
public class TrackLanguageRule : IFileProcessingRule
{
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);
        ArgumentNullException.ThrowIfNull(batchConfig);

        // Per-file configs already populated by synchronizer - we just populate global UI
        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsMatroskaTrackElement()))
        {
            var globalTracks = batchConfig.GetTrackListForType(trackType);

            for (int i = 0; i < globalTracks.Count; i++)
            {
                // Collect languages from all files that have this track
                var languages = batchConfig.FileConfigurations.Values
                    .Select(fc => fc.GetTrackListForType(trackType))
                    .Where(tracks => i < tracks.Count)
                    .Select(tracks => tracks[i].Language)
                    .Where(lang => lang != null)
                    .ToList();

                if (languages.Count == 0)
                    continue;

                // Business logic: Use most common language, or Undetermined if no clear winner
                globalTracks[i].Language = DetermineMostCommonLanguage(languages);
            }
        }
    }

    /// <summary>
    /// Determines the most common language from a list of languages across multiple files.
    /// </summary>
    /// <param name="languages">List of languages from all files for a specific track position.</param>
    /// <returns>The most frequently occurring language, or Undetermined if all languages differ equally.</returns>
    private static MatroskaLanguageOption DetermineMostCommonLanguage(List<MatroskaLanguageOption> languages)
    {
        if (languages.Count == 0)
            return MatroskaLanguageOption.Undetermined;

        // If all files have the same language, use it
        if (languages.Distinct().Count() == 1)
            return languages[0];

        // Find most common language
        var languageGroups = languages
            .GroupBy(l => l.Iso639_2_b)
            .OrderByDescending(g => g.Count())
            .ToList();

        // If there's a clear winner (more than half), use it
        var mostCommon = languageGroups[0];
        if (mostCommon.Count() > languages.Count / 2)
            return mostCommon.First();

        // Otherwise, no clear default - return Undetermined
        return MatroskaLanguageOption.Undetermined;
    }
}
