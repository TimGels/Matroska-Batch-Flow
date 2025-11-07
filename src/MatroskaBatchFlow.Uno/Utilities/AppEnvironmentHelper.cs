using System.Reflection;

namespace MatroskaBatchFlow.Uno.Utilities;

/// <summary>
/// Provides helper methods for checking the application's runtime environment.
/// </summary>
public static class AppEnvironmentHelper
{
    // Cache the result to avoid repeated exception handling, which is costly.
    private static readonly Lazy<bool> _isPackagedApp = new(DetectIsPackagedApp);

    /// <summary>
    /// Determines whether the current application is running as a packaged app.
    /// </summary>
    /// <returns><see langword="true"/> if the application is running as a packaged app; otherwise, <see langword="false"/>.</returns>
    public static bool IsPackagedApp() => _isPackagedApp.Value;

    private static bool DetectIsPackagedApp()
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        try
        {
            var x = Assembly.GetEntryAssembly()?.GetName().Name;
            // This will throw an exception if the app is not packaged.
            _ = Package.Current.Id.Name;
            return true;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }
}
