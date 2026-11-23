using MatroskaBatchFlow.Uno.Contracts.Services;
using Windows.System;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Opens web pages in the user's default browser through <see cref="Launcher"/>.
/// </summary>
public sealed partial class ExternalLinkService(ILogger<ExternalLinkService> logger) : IExternalLinkService
{
    /// <inheritdoc />
    public async Task OpenAsync(Uri uri)
    {
        try
        {
            if (!await Launcher.LaunchUriAsync(uri))
            {
                LogLinkNotOpened(uri);
            }
        }
        catch (Exception ex)
        {
            LogLinkFailed(uri, ex);
        }
    }
}
