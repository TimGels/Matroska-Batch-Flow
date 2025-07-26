namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for audio-related track configuration.
/// </summary>
public sealed partial class AudioPage : Page
{
    public AudioViewModel ViewModel { get; }
    public AudioPage()
    {
        ViewModel = App.GetService<AudioViewModel>();
        this.InitializeComponent();
    }
}
