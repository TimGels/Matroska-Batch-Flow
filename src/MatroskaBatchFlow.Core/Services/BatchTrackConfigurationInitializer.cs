using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Initializes per-file track configurations based on scanned file information.
/// </summary>
/// <remarks>
/// This class orchestrates track initialization for each file:
/// <list type="bullet">
/// <item>Creates per-file <see cref="FileTrackValues"/> directly from scanned track metadata</item>
/// <item>Delegates global <see cref="TrackConfiguration"/> creation to <see cref="ITrackConfigurationFactory"/> when expanding to match maximum track counts</item>
/// <item>Populates file-specific track lists in <see cref="IBatchConfiguration.FileConfigurations"/></item>
/// </list>
/// </remarks>
/// <param name="batchConfig">The batch configuration to be modified.</param>
/// <param name="trackConfigFactory">The factory for creating global track configurations.</param>
/// <param name="languageProvider">The language provider for resolving track language codes.</param>
public class BatchTrackConfigurationInitializer(
    IBatchConfiguration batchConfig,
    ITrackConfigurationFactory trackConfigFactory,
    ILanguageProvider languageProvider) : IBatchTrackConfigurationInitializer
{
    /// <inheritdoc/>
    public void Initialize(ScannedFileInfo scannedFile, params TrackType[] trackTypes)
    {
        if (scannedFile?.Result?.Media?.Track == null || trackTypes.Length == 0)
            return;

        // Ensure per-file configuration exists
        if (!batchConfig.FileConfigurations.TryGetValue(scannedFile.Id, out FileTrackConfiguration? fileConfig))
        {
            fileConfig = new FileTrackConfiguration
            {
                FilePath = scannedFile.Path
            };
            batchConfig.FileConfigurations.Add(scannedFile.Id, fileConfig);
        }

        // Populate file-specific track values based on what this file has
        foreach (var trackType in trackTypes)
        {
            // Ordering tracks by StreamKindID as it represents the track order in the file
            var scannedTracks = scannedFile.Result.Media.Track
                .Where(t => t.Type == trackType)
                .OrderBy(t => t.StreamKindID)
                .ToList();

            var fileTracks = fileConfig.GetTrackListForType(trackType);

            int existingCount = fileTracks.Count;
            int scannedCount = scannedTracks.Count;

            for (int i = existingCount; i < scannedCount; i++)
            {
                var scannedTrack = scannedTracks[i];
                fileTracks.Add(new FileTrackValues
                {
                    ScannedTrackInfo = scannedTrack,
                    Type = trackType,
                    Index = i,
                    Name = scannedTrack.Title ?? string.Empty,
                    Language = languageProvider.Resolve(scannedTrack.Language),
                    Default = scannedTrack.Default,
                    Forced = scannedTrack.Forced,
                });
            }
        }

        // Update global collections for UI display based on maximum track counts
        UpdateGlobalTracksForMaximumCounts(fileConfig, trackTypes);
    }

    /// <summary>
    /// Updates global track collections to reflect the maximum track count across all files.
    /// This ensures the UI displays all tracks that exist in any file, allowing users to
    /// configure tracks that may not exist in all files.
    /// </summary>
    /// <remarks>
    /// This will not remove any existing global tracks; it only adds new ones as needed.
    /// </remarks>
    /// <param name="fileConfig">The file configuration being initialized.</param>
    /// <param name="trackTypes">The track types to update.</param>
    private void UpdateGlobalTracksForMaximumCounts(FileTrackConfiguration fileConfig, TrackType[] trackTypes)
    {
        foreach (var trackType in trackTypes)
        {
            var fileTracks = fileConfig.GetTrackListForType(trackType);
            var batchTracks = batchConfig.GetTrackListForType(trackType);

            int fileTrackCount = fileTracks.Count;
            int batchTrackCount = batchTracks.Count;

            // Expand global collection if this file has more tracks than currently represented
            for (int i = batchTrackCount; i < fileTrackCount; i++)
            {
                var sourceTrack = fileTracks[i];
                batchTracks.Add(trackConfigFactory.Create(sourceTrack.ScannedTrackInfo, trackType, i));
            }
        }
    }
}
