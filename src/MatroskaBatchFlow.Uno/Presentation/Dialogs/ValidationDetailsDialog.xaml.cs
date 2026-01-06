namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

/// <summary>
/// Dialog for displaying detailed batch validation results.
/// </summary>
public sealed partial class ValidationDetailsDialog : ContentDialog
{
    public InputViewModel ViewModel { get; }

    public ValidationDetailsDialog(InputViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }
}
