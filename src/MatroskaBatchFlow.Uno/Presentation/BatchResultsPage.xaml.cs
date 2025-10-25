namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for displaying batch processing results.
/// </summary>
public sealed partial class BatchResultsPage : Page
{
    public BatchResultsViewModel ViewModel { get; }

    public BatchResultsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<BatchResultsViewModel>();
        DataContext = ViewModel;
    }
}
