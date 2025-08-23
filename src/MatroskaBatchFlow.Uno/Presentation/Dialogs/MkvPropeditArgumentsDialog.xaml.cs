using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;
using Windows.ApplicationModel.DataTransfer;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

public sealed partial class MkvPropeditArgumentsDialog : ContentDialog
{
    public string ArgumentsText { get; set; } = string.Empty;

    private string PrimaryButtonDefaultText { get; } = "Copy to clipboard";

    public MkvPropeditArgumentsDialog()
    {
        this.InitializeComponent();

        // Notify the dialog status when it is opened or closed.
        this.Opened += (sender, eventArgs)
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(MkvPropeditArgumentsDialog), true));
        this.Closed += (sender, eventArgs)
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(MkvPropeditArgumentsDialog), false));
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs eventArgs)
    {
        eventArgs.Cancel = true; // Prevent the dialog from closing.

        var package = new DataPackage();
        package.SetText(CliArgumentsTextBlock.Text);

        Clipboard.SetContent(package);
    }
}
