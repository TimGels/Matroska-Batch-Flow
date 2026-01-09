using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

/// <summary>
/// Analyzes per-file track forced flags and populates global UI properties.
/// Per-file configurations are already populated by <see cref="BatchTrackConfigurationInitializer"/>.
/// This rule determines what forced flag to display in the UI based on all files.
/// </summary>
public class TrackForcedRule : IFileProcessingRule
{
    /// <summary>
    /// Analyzes per-file forced flags and populates global UI with most common value.
    /// </summary>
    /// <param name="scannedFile">The scanned file information (used for context).</param>
    /// <param name="batchConfig">The batch configuration to update with global UI forced flags.</param>
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
                // Collect forced flags from all files that have this track
                var forcedFlags = batchConfig.FileConfigurations.Values
                    .Select(fc => fc.GetTrackListForType(trackType))
                    .Where(tracks => i < tracks.Count)
                    .Select(tracks => tracks[i].Forced)
                    .ToList();

                if (forcedFlags.Count == 0)
                    continue;

                // Business logic: Use most common value (true if majority are true)
                globalTracks[i].Forced = forcedFlags.Count(f => f) > forcedFlags.Count / 2;
            }
        }
    }
}
