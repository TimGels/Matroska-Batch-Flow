using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Creates track configurations from scanned track information.
/// </summary>
/// <param name="languageProvider">The language provider for resolving track language codes.</param>
public class TrackConfigurationFactory(ILanguageProvider languageProvider) : ITrackConfigurationFactory
{
    /// <inheritdoc/>
    public TrackConfiguration Create(
        MediaInfoResult.MediaInfo.TrackInfo scannedTrackInfo,
        TrackType trackType,
        int index)
    {
        ArgumentNullException.ThrowIfNull(scannedTrackInfo);

        return new TrackConfiguration(scannedTrackInfo)
        {
            Type = trackType,
            Index = index,
            Name = scannedTrackInfo.Title ?? string.Empty,
            Language = languageProvider.Resolve(scannedTrackInfo.Language),
            Default = scannedTrackInfo.Default,
            Forced = scannedTrackInfo.Forced
        };
    }
}
