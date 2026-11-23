namespace MatroskaBatchFlow.Uno.Helpers;

/// <summary>
/// Web pages about the project that the app opens in the browser.
/// </summary>
public static class ProjectLinks
{
    /// <summary>
    /// Gets the website page that explains how to support the project.
    /// </summary>
    /// <remarks>
    /// The app links to the website rather than to each donation platform, so the platforms can change without an app update.
    /// </remarks>
    public static Uri SupportPage { get; } = new("https://matroskabatchflow.com/sponsor");
}
