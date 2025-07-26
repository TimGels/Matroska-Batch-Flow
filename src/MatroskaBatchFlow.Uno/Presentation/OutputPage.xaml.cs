namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class OutputPage : Page
{
    public OutputViewModel ViewModel { get; }
    public OutputPage()
    {
        ViewModel = App.GetService<OutputViewModel>();
        this.InitializeComponent();  
    }
}
