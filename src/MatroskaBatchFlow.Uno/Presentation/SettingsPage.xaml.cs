using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Utilities;
using Windows.Storage.Pickers;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class SettingsPage : Page
{
    /// <summary>
    /// Scrolls to the Batch Validation section when the page is opened with <see cref="SettingsViewModel.ValidationSection"/>.
    /// </summary>
    /// <param name="e">The navigation data. Its parameter selects the section to show.</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string section && section == SettingsViewModel.ValidationSection)
        {
            if (ValidationHeader.IsLoaded)
            {
                ValidationHeader.StartBringIntoView();
            }
            else
            {
                ValidationHeader.Loaded -= BringValidationIntoView;
                ValidationHeader.Loaded += BringValidationIntoView;
            }
        }
    }

    private void BringValidationIntoView(object sender, RoutedEventArgs e)
    {
        ValidationHeader.Loaded -= BringValidationIntoView;
        ValidationHeader.StartBringIntoView();
    }

    public bool IsCardEnabled { get; set; } = true;

    public SettingsViewModel ViewModel { get; }

    /// <summary>
    /// Gets the warning shown before a lower limit removes saved results, for example "Lowering the limit to 5 removes the 3 oldest saved results.".
    /// </summary>
    /// <param name="count">The number of saved results that would be removed.</param>
    /// <param name="maxRuns">The entered limit.</param>
    /// <returns>The localized warning.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Binding requires instance member.")]
    private string BatchHistoryRemovalText(int count, double maxRuns) =>
        UiText.Format(UiText.CountedKey("BatchHistoryLimitRemoves", count), count, (int)maxRuns);

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Binding requires instance member.")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression is necessary.")]
    public string AppVersion => $"Version {AppEnvironmentHelper.GetApplicationVersion().ToString(3)}";

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Binding requires instance member.")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression is necessary.")]
    public string CopyrightNotice => $"© 2025-{DateTime.Now.Year}. All rights reserved.";

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
