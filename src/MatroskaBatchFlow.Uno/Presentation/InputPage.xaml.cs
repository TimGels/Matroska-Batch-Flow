using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for adding and managing matroska files.
/// </summary>
public sealed partial class InputPage : Page
{
    public InputViewModel ViewModel { get; }

    public InputPage()
    {
        ViewModel = App.GetService<InputViewModel>();
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<DialogStatusMessage>(this, (r, dialogMessage) =>
        {
            // If the dialog is open, disable file drop on the ListView.
            this.FileDropListView.AllowDrop = !dialogMessage.IsOpen;
        });

        // Register to handle showing validation details dialog
        WeakReferenceMessenger.Default.Register<ShowValidationDetailsMessage>(this, async (r, message) =>
        {
            await ShowValidationDetailsDialogAsync();
        });
    }

    /// <summary>
    /// Shows the validation details dialog with all validation results.
    /// </summary>
    private async Task ShowValidationDetailsDialogAsync()
    {
        var dialog = new ValidationDetailsDialog(ViewModel)
        {
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
