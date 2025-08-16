using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

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
    }

    /// <summary>
    /// Handles the <see cref="PointerEntered"/> event for a ListView item, updating its visual state when a mouse or
    /// pen pointer enters.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a <see cref="Control"/>.</param>
    /// <param name="eventArgs">The event data containing information about the pointer that triggered the event.</param>
    private void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs eventArgs)
    {
        if (eventArgs.Pointer.PointerDeviceType is not (PointerDeviceType.Mouse or PointerDeviceType.Pen))
        {
            return;
        }

        if (sender is Control control)
        {
            VisualStateManager.GoToState(control, "HoverButtonsShown", true);
        }
    }

    /// <summary>
    /// Handles the <see cref="UIElement.PointerExited"/> event for a ListView item.
    /// </summary>
    /// <param name="sender">The source of the event, typically the ListView item that the pointer exited.</param>
    /// <param name="eventArgs">The event data containing information about the pointer event.</param>
    private void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs eventArgs)
    {
        if (sender is Control control)
        {
            VisualStateManager.GoToState(control, "HoverButtonsHidden", true);
        }
    }
}
