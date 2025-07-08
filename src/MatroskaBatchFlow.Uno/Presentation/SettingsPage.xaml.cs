// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MatroskaBatchFlow.Uno.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    // Property to bind to IsEnabled in XAML
    public bool IsCardEnabled { get; set; } = true;

    public string AppVersion => $"Version {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

    public SettingsPage()
    {
        this.InitializeComponent();
    }
}
