namespace MatroskaBatchFlow.Uno.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class VideoPage : Page
{
    public VideoPage()
    {
        this.InitializeComponent();
        this.DataContext = App.GetService<VideoViewModel>();
    }
}
