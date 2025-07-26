namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for general-related matroska file configuration.
/// </summary>
public sealed partial class GeneralPage : Page
{
    public GeneralViewModel ViewModel { get; }
    public GeneralPage()
    {
        ViewModel = App.GetService<GeneralViewModel>();
        this.InitializeComponent();
    }
}
