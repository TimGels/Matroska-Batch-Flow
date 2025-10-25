using MatroskaBatchFlow.Core.Builders.MkvPropeditArguments;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides reusable logic for generating <c>mkvpropedit</c> command-line arguments from an <see cref="IBatchConfiguration"/>.
/// </summary>
public sealed class MkvPropeditArgumentsGenerator : IMkvPropeditArgumentsGenerator
{
    /// <inheritdoc />
    public string[] BuildBatchArguments(IBatchConfiguration batchConfiguration)
    {
        var results = new List<string>();

        foreach (var file in batchConfiguration.FileList)
        {
            var tokens = BuildFileArgumentTokens(file, batchConfiguration);
            if (tokens.Length == 0)
            {
                results.Add(string.Empty);
            }
            results.Add(string.Join(" ", tokens));
        }

        return [.. results];
    }

    /// <inheritdoc />
    public string BuildFileArgumentString(ScannedFileInfo file, IBatchConfiguration batchConfiguration)
    {
        var tokens = BuildFileArgumentTokens(file, batchConfiguration);

        return tokens.Length == 0 ? string.Empty : string.Join(" ", tokens);
    }

    /// <summary>
    /// Builds the individual argument tokens for a single file, based on batch-level flags and per-track
    /// modification indicators.
    /// </summary>
    /// <param name="file">The scanned file whose path will be set as the mkvpropedit input.</param>
    /// <param name="batchConfiguration">Contains global title settings and track configurations.</param>
    /// <returns> Token array suitable for joining. Returns an empty array if no modifications are requested 
    /// (to signal "no-op").</returns>
    private static string[] BuildFileArgumentTokens(ScannedFileInfo file, IBatchConfiguration batchConfiguration)
    {
        var builder = new MkvPropeditArgumentsBuilder();

        if (batchConfiguration.ShouldModifyTitle)
        {
            builder.WithTitle(batchConfiguration.Title);
        }

        if (batchConfiguration.ShouldModifyTrackStatisticsTags)
        {
            if (batchConfiguration.AddTrackStatisticsTags)
            {
                builder.WithAddTrackStatisticsTags();
            }
            if (batchConfiguration.DeleteTrackStatisticsTags)
            {
                builder.WithDeleteTrackStatisticsTags();
            }
        }

        // Per-track modifications.
        AddTracks(builder, batchConfiguration.AudioTracks, TrackType.Audio);
        AddTracks(builder, batchConfiguration.VideoTracks, TrackType.Video);
        AddTracks(builder, batchConfiguration.SubtitleTracks, TrackType.Text);

        // No tokens => no modifications requested.
        if (builder.IsEmpty())
        {
            return [];
        }

        // File path applied last.
        builder.SetInputFile(file.Path);

        return builder.Build();
    }

    /// <summary>
    /// Adds track-specific modifications to the builder, filtering out tracks that have no requested changes.
    /// </summary>
    /// <param name="builder">The accumulating mkvpropedit argument builder.</param>
    /// <param name="tracks">Track configurations of a single logical type.</param>
    /// <param name="type">The track type (must map to a Matroska track element).</param>
    private static void AddTracks(MkvPropeditArgumentsBuilder builder, IEnumerable<TrackConfiguration> tracks, TrackType type)
    {
        if (!type.IsMatroskaTrackElement())
        {
            return;
        }

        foreach (var track in tracks)
        {
            // Skip inert tracks (no requested modifications).
            if (!(track.ShouldModifyLanguage ||
                  track.ShouldModifyName ||
                  track.ShouldModifyDefaultFlag ||
                  track.ShouldModifyForcedFlag ||
                  track.ShouldModifyEnabledFlag))
            {
                continue;
            }

            builder.AddTrack(tb =>
            {
                // Track ID converted to 1-based indexing for mkvpropedit conventions.
                tb.SetTrackId(track.Index + 1).SetTrackType(type);

                if (track.ShouldModifyLanguage)
                {
                    tb.WithLanguage(track.Language.Code);
                }

                if (track.ShouldModifyName)
                {
                    tb.WithName(track.Name);
                }

                if (track.ShouldModifyDefaultFlag)
                {
                    tb.WithIsDefault(track.Default);
                }

                if (track.ShouldModifyForcedFlag)
                {
                    tb.WithIsForced(track.Forced);
                }

                if (track.ShouldModifyEnabledFlag)
                {
                    tb.WithIsEnabled(track.Enabled);
                }

                return tb;
            });
        }
    }
}
