namespace MatroskaBatchFlow.Uno.Utilities;

public static class AppPathHelper
{
    /// <summary>
    /// Gets the local application data folder for the current user.
    /// </summary>
    /// <returns>The path to the local application data folder.</returns>
    public static string GetLocalAppDataFolder()
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        if (AppEnvironmentHelper.IsPackagedApp())
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
#endif
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(localAppData, App.AppName);
        return appFolder;
    }
}
