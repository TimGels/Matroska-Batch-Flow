using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

public sealed partial class ErrorDialog : ContentDialog
{
    public string MessageText { get; set; } = string.Empty;

    public ErrorDialog()
    {
        this.InitializeComponent();
        // Notify the dialog status when it is opened or closed.
        this.Opened += (sender, eventArgs) 
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(ErrorDialog), true));
        this.Closed += (sender, eventArgs) 
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(ErrorDialog), false));
    }
}
