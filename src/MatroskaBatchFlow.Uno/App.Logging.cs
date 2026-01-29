using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using MatroskaBatchFlow.Uno.Utilities;

namespace MatroskaBatchFlow.Uno;

/// <summary>
/// LoggerMessage definitions for <see cref="App"/>.
/// </summary>
public partial class App
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Application started")]
    private partial void LogApplicationStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "  Version: {Version}")]
    private partial void LogStartupVersion(string version);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Platform: {Platform}")]
    private partial void LogStartupPlatform(string platform);

    [LoggerMessage(Level = LogLevel.Information, Message = "  OS: {OperatingSystem}")]
    private partial void LogStartupOS(string operatingSystem);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Runtime: {Runtime}")]
    private partial void LogStartupRuntime(string runtime);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Architecture: {Architecture}")]
    private partial void LogStartupArchitecture(string architecture);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Build: {BuildConfiguration}")]
    private partial void LogStartupBuild(string buildConfiguration);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Culture: {Culture}")]
    private partial void LogStartupCulture(string culture);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Unhandled exception occurred: {Message}")]
    private partial void LogUnhandledException(Exception ex, string message);

    /// <summary>
    /// Logs application startup information including version, platform, and environment details.
    /// </summary>
    [SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Logging startup info is important for diagnostics and only occurs during application launch.")]
    private void LogStartupInfo()
    {
        LogApplicationStarted();
        LogStartupVersion(AppEnvironmentHelper.GetApplicationVersion().ToString(3));
        LogStartupPlatform(GetPlatformType());
        LogStartupOS(RuntimeInformation.OSDescription);
        LogStartupRuntime(RuntimeInformation.FrameworkDescription);
        LogStartupArchitecture(RuntimeInformation.ProcessArchitecture.ToString());
        LogStartupBuild(GetBuildConfiguration());
        LogStartupCulture(CultureInfo.CurrentUICulture.Name);
    }

    /// <summary>
    /// Determines the platform type based on compile-time constants.
    /// </summary>
    private static string GetPlatformType()
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        return AppEnvironmentHelper.IsPackagedApp()
            ? "WinAppSDK (Packaged)"
            : "WinAppSDK (Unpackaged)";
#elif HAS_UNO_SKIA
        return "Skia Desktop";
#else
        return "Unknown";
#endif
    }

    /// <summary>
    /// Gets the build configuration (Debug or Release).
    /// </summary>
    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}
