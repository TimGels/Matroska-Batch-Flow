using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;

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

    private void OnReviewFileChecksClicked(object sender, RoutedEventArgs e) =>
        ViewModel.ReviewFileChecksCommand.Execute(((FrameworkElement)sender).DataContext);

    // Context flyout items do not reliably inherit the row's DataContext, so the row is passed through Tag.
    private void OnRemoveFileClicked(object sender, RoutedEventArgs e) =>
        ViewModel.RemoveFileCommand.Execute(((FrameworkElement)sender).Tag);
}
