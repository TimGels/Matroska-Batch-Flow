namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for video-related track configuration.
/// </summary>
public sealed partial class VideoPage : Page
{
    public VideoViewModel ViewModel { get; }
    public VideoPage()
    {
        ViewModel = App.GetService<VideoViewModel>();
        this.InitializeComponent();
    }
}
