namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class SubtitlePage : Page
{
    public SubtitlePage()
    {
        this.InitializeComponent();
        this.DataContext = App.GetService<SubtitleViewModel>();
    }
}
