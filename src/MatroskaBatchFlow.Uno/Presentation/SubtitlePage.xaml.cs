namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for subtitle-related track configuration.
/// </summary>
public sealed partial class SubtitlePage : Page
{
    public SubtitleViewModel ViewModel { get; }
    public SubtitlePage()
    {
        ViewModel = App.GetService<SubtitleViewModel>();
        this.InitializeComponent();
    }
}
