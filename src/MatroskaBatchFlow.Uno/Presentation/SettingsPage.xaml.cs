using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Windows.Storage.Pickers;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class SettingsPage : Page
{
    public bool IsCardEnabled { get; set; } = true;

    public SettingsViewModel ViewModel { get; }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Binding requires instance member.")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression is necessary.")]
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
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;
    }

    private async void BrowseExecutable_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();

        // On Windows we can just be specific about .exe files.
        if (OperatingSystem.IsWindows())
        {
            picker.FileTypeFilter.Add(".exe");
        }
        else
        {
            picker.FileTypeFilter.Add("*"); // Fallback for other platforms
        }
        
        picker.SuggestedStartLocation = PickerLocationId.Desktop;

        // Initialize with window handle for WinUI 3
        if (App.MainWindow != null)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
        }

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.CustomMkvPropeditPath = file.Path;
        }
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
