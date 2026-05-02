using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services.FileProcessing.Track.Name;

/// <summary>
/// Analyzes per-file audio track names and populates global TrackIntent properties with smart defaults.
/// This rule can implement advanced naming logic based on codec, channel layout, etc.
/// </summary>
public class AudioTrackNamingRule : IFileProcessingRule
{
    private static readonly Dictionary<string, string> _channelLayoutMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "L R", "Stereo" },
        { "L R C LFE Ls Rs", "5.1" },
        // Add more as needed
    };

    public void Apply(ScannedFileInfo scannedFile, IBatchConfiguration batchConfig)
    {
        if (scannedFile?.Result?.Media?.Track == null || batchConfig == null)
            return;

        var globalTracks = batchConfig.GetTrackListForType(TrackType.Audio);

        for (int i = 0; i < globalTracks.Count; i++)
        {
            // Gather all track names for this index across files to determine a common name.
            var names = GetDistinctAudioTrackNames(batchConfig.FileList, i);

            if (names.Count == 1)
            {
                globalTracks[i].Name = names[0];
            }
            else if (names.Count > 0)
            {
                // If multiple distinct names exist, attempt to find the most common one to use as a default.
                var mostCommonName = names
                    .GroupBy(n => n)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;

                globalTracks[i].Name = mostCommonName;
            }
        }
    }

    /// <summary>
    /// Retrieves a list of unique audio track names from a collection of scanned files based on the specified track
    /// index.
    /// </summary>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to analyze for audio track names.</param>
    /// <param name="trackIndex">The zero-based index of the audio track to retrieve from each file's audio tracks.</param>
    /// <returns>A list of distinct audio track names corresponding to the specified track index from the provided files. The
    /// list may be empty if no valid audio tracks are found.</returns>
    private static List<string> GetDistinctAudioTrackNames(IEnumerable<ScannedFileInfo> files, int trackIndex)
    {
        var names = new List<string>();

        foreach (var file in files)
        {
            var audioTracks = file.GetTracks(TrackType.Audio);

            if (trackIndex >= audioTracks.Count)
            {
                continue;
            }

            var name = audioTracks[trackIndex].Title;
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        // Multiple files may share the same title; callers only need unique candidates.
        return names.Distinct().ToList();
    }
}
