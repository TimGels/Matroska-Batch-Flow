using CommunityToolkit.Mvvm.Messaging;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;
public sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialogViewModel ViewModel { get; } = new();

    public ErrorDialog()
    {
        this.InitializeComponent();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
}
