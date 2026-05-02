using MatroskaBatchFlow.Core.Enums;
using static MatroskaBatchFlow.Core.Models.MediaInfoResult.MediaInfo;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Creates track intents from scanned track information.
/// </summary>
/// <param name="languageProvider">The language provider for resolving track language codes.</param>
public class TrackIntentFactory(ILanguageProvider languageProvider) : ITrackIntentFactory
{
    /// <inheritdoc/>
    public TrackIntent Create(TrackInfo scannedTrackInfo, TrackType trackType, int index)
    {
        ArgumentNullException.ThrowIfNull(scannedTrackInfo);

        return new TrackIntent(scannedTrackInfo)
        {
            Type = trackType,
            Index = index,
            Name = scannedTrackInfo.Title ?? string.Empty,
            Language = languageProvider.Resolve(scannedTrackInfo.Language),
            Default = scannedTrackInfo.Default,
            Forced = scannedTrackInfo.Forced,
            Enabled = true,
        };
    }
}
