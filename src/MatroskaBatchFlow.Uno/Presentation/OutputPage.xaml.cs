namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class OutputPage : Page
{
    public OutputPage()
    {
        this.InitializeComponent();
        this.DataContext = App.GetService<OutputViewModel>();
    }
}
