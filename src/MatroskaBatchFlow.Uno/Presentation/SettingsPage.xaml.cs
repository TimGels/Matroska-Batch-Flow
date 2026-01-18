using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Utilities;
using Windows.Storage.Pickers;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class SettingsPage : Page
{
    public bool IsCardEnabled { get; set; } = true;

    public SettingsViewModel ViewModel { get; }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Binding requires instance member.")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression is necessary.")]
    public string AppVersion => $"Version {AppEnvironmentHelper.GetApplicationVersion().ToString(3)}";

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

        // Initialize the picker with the main window handle
        if (App.MainWindow != null)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new DialogMessage("Error", "File picker is not available at this time. Please try again."));
            return;
        }

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.CustomMkvPropeditPath = file.Path;
        }
    }
}
