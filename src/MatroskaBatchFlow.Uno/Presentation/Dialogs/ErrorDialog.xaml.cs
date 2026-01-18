using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

/// <summary>
/// Dialog for displaying unhandled exception details to the user.
/// </summary>
public sealed partial class ErrorDialog : ContentDialog
{
    /// <summary>
    /// Gets the ViewModel for this dialog.
    /// </summary>
    public ErrorDialogViewModel ViewModel { get; }

    /// <summary>
    /// Gets or sets whether the user chose to exit the application.
    /// </summary>
    public bool ShouldExit { get; private set; }

    public ErrorDialog(ErrorDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        // Notify the dialog status when it is opened or closed.
        this.Opened += (sender, eventArgs)
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(ErrorDialog), true));
        this.Closed += (sender, eventArgs)
            => WeakReferenceMessenger.Default.Send(new DialogStatusMessage(nameof(ErrorDialog), false));
    }

    /// <summary>
    /// Handles the Copy Details button click - copies exception details to clipboard without closing dialog.
    /// </summary>
    private void OnCopyDetailsClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true; // Prevent the dialog from closing
        ViewModel.CopyDetailsCommand.Execute(null);
    }

    /// <summary>
    /// Handles the Continue button click - closes dialog and allows application to continue.
    /// </summary>
    private void OnContinueClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ShouldExit = false;
        // Dialog will close automatically
    }

    /// <summary>
    /// Handles the Save &amp; Exit button click - saves log and exits application.
    /// </summary>
    private async void OnSaveAndExitClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            await ViewModel.SaveLogCommand.ExecuteAsync(null);
            ShouldExit = true;
        }
        finally
        {
            deferral.Complete();
        }
    }
}
