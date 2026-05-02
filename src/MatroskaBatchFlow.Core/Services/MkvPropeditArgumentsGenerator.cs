using MatroskaBatchFlow.Core.Builders.MkvPropeditArguments;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides reusable logic for generating <c>mkvpropedit</c> command-line arguments from an <see cref="IBatchConfiguration"/>.
/// </summary>
public sealed partial class MkvPropeditArgumentsGenerator(ILogger<MkvPropeditArgumentsGenerator> logger) : IMkvPropeditArgumentsGenerator
{
    /// <inheritdoc />
    public string[] BuildBatchArguments(IBatchConfiguration batchConfiguration)
    {
        var results = new List<string>();

        foreach (var file in batchConfiguration.FileList)
        {
            var tokens = BuildFileArgumentTokens(file, batchConfiguration);
            if (tokens.Length != 0)
            {
                results.Add(string.Join(" ", tokens));
            }
        }

        LogBatchArgumentsGenerated(batchConfiguration.FileList.Count, results.Count);

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
    /// <param name="batchConfiguration">Contains global title settings and track intents.</param>
    /// <returns>Token array suitable for joining. Returns an empty array if no modifications are requested 
    /// (to signal "no-op").</returns>
    private string[] BuildFileArgumentTokens(ScannedFileInfo file, IBatchConfiguration batchConfiguration)
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

        AddTracksForFile(builder, file, TrackType.Audio, batchConfiguration);
        AddTracksForFile(builder, file, TrackType.Video, batchConfiguration);
        AddTracksForFile(builder, file, TrackType.Text, batchConfiguration);

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
    /// Adds track-specific modifications to the builder for a specific file.
    /// </summary>
    /// <param name="builder">The accumulating mkvpropedit argument builder.</param>
    /// <param name="file">The file being processed.</param>
    /// <param name="type">The track type (must map to a Matroska track element).</param>
    /// <param name="batchConfig">The batch configuration containing track intents.</param>
    private void AddTracksForFile(
        MkvPropeditArgumentsBuilder builder,
        ScannedFileInfo file,
        TrackType type,
        IBatchConfiguration batchConfig)
    {
        if (!type.IsMatroskaTrackElement())
        {
            return;
        }

        var trackIntents = batchConfig.GetTrackListForType(type);

        var scannedTracks = file.GetTracks(type);

        foreach (var intent in trackIntents)
        {
            // Check if this track actually exists in the file
            if (intent.Index < 0 || intent.Index >= scannedTracks.Count)
            {
                LogTrackMissingInFile(file.Path, type, intent.Index);
                continue;
            }

            if (!(intent.ShouldModifyLanguage ||
                  intent.ShouldModifyName ||
                  intent.ShouldModifyDefaultFlag ||
                  intent.ShouldModifyForcedFlag ||
                  intent.ShouldModifyEnabledFlag))
            {
                continue;
            }

            builder.AddTrack(tb =>
            {
                // Track ID converted to 1-based indexing for mkvpropedit conventions.
                tb.SetTrackId(intent.Index + 1).SetTrackType(type);

                if (intent.ShouldModifyLanguage)
                {
                    tb.WithLanguage(intent.Language.Code);
                }

                if (intent.ShouldModifyName)
                {
                    tb.WithName(intent.Name);
                }

                if (intent.ShouldModifyDefaultFlag)
                {
                    tb.WithIsDefault(intent.Default);
                }

                if (intent.ShouldModifyForcedFlag)
                {
                    tb.WithIsForced(intent.Forced);
                }

                if (intent.ShouldModifyEnabledFlag)
                {
                    tb.WithIsEnabled(intent.Enabled);
                }

                return tb;
            });
        }
    }
}
