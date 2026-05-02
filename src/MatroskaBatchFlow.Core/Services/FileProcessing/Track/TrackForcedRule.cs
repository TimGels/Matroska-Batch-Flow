using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

/// <summary>
/// Analyzes per-file track forced flags and populates global TrackIntent properties.
/// This rule determines the forced flag to display in the UI based on all scanned files.
/// </summary>
public class TrackForcedRule : IFileProcessingRule
{
    /// <summary>
    /// Analyzes per-file forced flags and populates global intents with the most common value.
    /// </summary>
    /// <param name="scannedFile">The scanned file information (used for context).</param>
    /// <param name="batchConfig">The batch configuration to update with global UI forced flags.</param>
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);
        ArgumentNullException.ThrowIfNull(batchConfig);

        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsMatroskaTrackElement()))
        {
            var globalTracks = batchConfig.GetTrackListForType(trackType);

            for (int i = 0; i < globalTracks.Count; i++)
            {
                var forcedFlags = batchConfig.FileList
                    .Select(f => f.GetTracks(trackType))
                    .Where(tracks => i < tracks.Count)
                    .Select(tracks => tracks[i].Forced)
                    .ToList();

                if (forcedFlags.Count == 0)
                    continue;

                globalTracks[i].Forced = forcedFlags.Count(f => f) > forcedFlags.Count / 2;
            }
        }
    }
}
