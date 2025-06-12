using CommunityToolkit.Mvvm.Messaging;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;
public sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialogViewModel ViewModel { get; } = new();

    public ErrorDialog()
    {
        this.InitializeComponent();
        // Notify the dialog status when it is opened or closed.
        this.Opened += (sender, eventArgs) 
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(ErrorDialog), true));
        this.Closed += (sender, eventArgs) 
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(ErrorDialog), false));
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
}
