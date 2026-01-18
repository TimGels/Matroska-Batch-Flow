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

    /// <summary>
    /// Gets the application version.
    /// </summary>
    /// <returns>The application <see cref="Version"/>, or a default version (0.0.0) if unavailable.</returns>
    public static Version GetApplicationVersion()
    {
        if (IsPackagedApp())
        {
#if WINDOWS10_0_19041_0_OR_GREATER
            var pkgVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            return new Version(pkgVersion.Major, pkgVersion.Minor, pkgVersion.Build, pkgVersion.Revision);
#endif
        }

        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
    }

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
