using System.Reflection;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class SettingsPage : Page
{
    public bool IsCardEnabled { get; set; } = true;

    public string AppVersion
    {
        get
        {
            if (IsPackagedApp())
            {
                var version = Package.Current.Id.Version;
                return $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
            }
        }
    }

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Determines whether the current application is running as a packaged app.
    /// </summary>
    /// <returns><see langword="true"/> if the application is running as a packaged app; otherwise, <see langword="false"/>.</returns>
    private static bool IsPackagedApp()
    {
        try
        {
            // This will throw an exception if the app is not packaged.
            var name = Package.Current.Id.Name;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
