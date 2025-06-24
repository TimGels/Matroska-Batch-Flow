using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;

namespace MatroskaBatchFlow.Uno.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InputPage : Page
{
    public InputPage()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<DialogStatusMessage>(this, (r, dialogMessage) =>
        {
            // If the dialog is open, disable file drop on the ListView.
            this.FileDropListView.AllowDrop = !dialogMessage.IsOpen;
        });
    }
}
