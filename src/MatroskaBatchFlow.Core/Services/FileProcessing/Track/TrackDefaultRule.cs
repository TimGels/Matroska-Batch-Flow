using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

/// <summary>
/// Analyzes per-file track default flags and populates global TrackIntent properties.
/// This rule determines the default flag to display in the UI based on all scanned files.
/// </summary>
public class TrackDefaultRule : IFileProcessingRule
{
    /// <summary>
    /// Analyzes per-file default flags and populates global intents with the most common value.
    /// </summary>
    /// <param name="scannedFile">The scanned file information (used for context).</param>
    /// <param name="batchConfig">The batch configuration to update with global UI defaults.</param>
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);
        ArgumentNullException.ThrowIfNull(batchConfig);

        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsMatroskaTrackElement()))
        {
            var globalTracks = batchConfig.GetTrackListForType(trackType);

            for (int i = 0; i < globalTracks.Count; i++)
            {
                var defaultFlags = batchConfig.FileList
                    .Select(f => f.GetTracks(trackType))
                    .Where(tracks => i < tracks.Count)
                    .Select(tracks => tracks[i].Default)
                    .ToList();

                if (defaultFlags.Count == 0)
                    continue;

                globalTracks[i].Default = defaultFlags.Count(f => f) > defaultFlags.Count / 2;
            }
        }
    }
}
