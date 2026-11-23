namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Opens web pages outside the app.
/// </summary>
public interface IExternalLinkService
{
    /// <summary>
    /// Opens a web page in the user's default browser.
    /// </summary>
    /// <remarks>
    /// Failures are logged instead of thrown, so callers such as event handlers can await it safely.
    /// </remarks>
    /// <param name="uri">The address of the page.</param>
    /// <returns>A task that completes when the browser has been asked to open the page.</returns>
    Task OpenAsync(Uri uri);
}
