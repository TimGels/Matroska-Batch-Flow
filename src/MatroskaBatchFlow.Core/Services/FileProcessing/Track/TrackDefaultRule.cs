using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track;

/// <summary>
/// A rule that applies track default settings from a scanned file to the batch configuration.
/// </summary>
public class TrackDefaultRule : IFileProcessingRule
{
    /// <summary>
    /// Applies the track default settings from the scanned file to the batch configuration.
    /// </summary>
    /// <param name="scannedFile">The scanned file information containing track data.</param>
    /// <param name="batchConfig">The batch configuration to update with track default settings.</param>
    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        ArgumentNullException.ThrowIfNull(scannedFile);
        ArgumentNullException.ThrowIfNull(batchConfig);

        foreach (var trackType in Enum.GetValues<TrackType>().Where(t => t.IsMatroskaTrackElement()))
        {
            var scannedTracks = scannedFile.Result.Media.Track
                .Where(t => t.Type == trackType)
                .ToArray();

            var batchTracks = batchConfig.GetTrackListForType(trackType);

            for (int i = 0; i < batchTracks.Count && i < scannedTracks.Length; i++)
            {
                batchTracks[i].Default = scannedTracks[i].Default;
            }
        }
    }
}
