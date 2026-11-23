namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="ExternalLinkService"/>.
/// </summary>
public sealed partial class ExternalLinkService
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "The default browser did not open {Uri}")]
    private partial void LogLinkNotOpened(Uri uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to open {Uri} in the default browser")]
    private partial void LogLinkFailed(Uri uri, Exception ex);
}
