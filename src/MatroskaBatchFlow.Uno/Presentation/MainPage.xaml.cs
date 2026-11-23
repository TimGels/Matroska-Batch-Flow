using System.Collections.Concurrent;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Helpers;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Windows.Foundation;
using static MatroskaBatchFlow.Uno.Presentation.UiText;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    private readonly ConcurrentQueue<object> _dialogQueue = new();
    private readonly SemaphoreSlim _dialogSemaphore = new(1, 1);

    /// <summary>
    /// Gets the text that explains the batch state, for example "2 unsupported files block processing.".
    /// </summary>
    /// <param name="status">The current batch state.</param>
    /// <returns>The localized explanation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> has an unknown reason.</exception>
    private string AvailabilityText(BatchAvailability status) => status.Reason switch
    {
        BatchAvailabilityReason.NoFiles => Get("FooterNoFiles"),
        BatchAvailabilityReason.Busy => Get("FooterBusy"),
        BatchAvailabilityReason.UnsupportedFiles => Counted("FooterUnsupportedFiles", status.Count),
        BatchAvailabilityReason.BlockingChecks => Counted("FooterBlockingChecks", status.Count),
        BatchAvailabilityReason.ValidationBlocked => Get("FooterValidationBlocked"),
        BatchAvailabilityReason.NoChanges => Get("FooterNoChanges"),
        BatchAvailabilityReason.ReadyWithAdvisories => Get("FooterReadyWithAdvisories"),
        BatchAvailabilityReason.Ready => Get("FooterReady"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status.Reason, "Unknown batch availability reason.")
    };

    /// <summary>
    /// Gets the label of the review banner action that opens the Validation page, for example "Review 2 unsupported files".
    /// </summary>
    /// <param name="status">The current batch state.</param>
    /// <returns>The localized label.</returns>
    private string ReviewActionText(BatchAvailability status) => status.Reason switch
    {
        BatchAvailabilityReason.UnsupportedFiles => Counted("FooterReviewUnsupportedFiles", status.Count),
        BatchAvailabilityReason.BlockingChecks => Counted("FooterReviewBlockingChecks", status.Count),
        _ => Get("FooterReviewChecks")
    };

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();

        ViewModel.NavigationService.Frame = contentFrame;
        ViewModel.NavigationViewService.Initialize(NavigationView);

#if !WINDOWS10_0_19041_0_OR_GREATER
        // Uno does not apply the separator's line state, so its template falls back to a horizontal line.
        // The separator only appears in the top footer, where WinUI draws it as a vertical line.
        SupportSeparator.Loaded += (_, _) => VisualStateManager.GoToState(SupportSeparator, "VerticalLine", false);
#endif

        WeakReferenceMessenger.Default.Register<DialogMessage>(this, async (r, m) =>
        {
            _dialogQueue.Enqueue(m);
            await ShowDialogAsync().ConfigureAwait(true);
        });

        WeakReferenceMessenger.Default.Register<MkvPropeditArgumentsDialogMessage>(this, async (r, m) =>
        {
            _dialogQueue.Enqueue(m);
            await ShowDialogAsync().ConfigureAwait(true);
        });

        WeakReferenceMessenger.Default.Register<ExceptionDialogMessage>(this, async (r, m) =>
        {
            _dialogQueue.Enqueue(m);
            await ShowDialogAsync().ConfigureAwait(true);
        });

        // Clip navigation view content to the content host's bounds to prevent navigation transitions from overlapping other UI elements.
        ContentHost.SizeChanged += (s, e) =>
        {
            ContentClip.Rect = new Rect(0, 0, ContentHost.ActualWidth, ContentHost.ActualHeight);
        };

        Loaded += MainPage_Loaded;
    }

    /// <summary>
    /// Shows dialogs from the queue one by one, ensuring that only one dialog is displayed at a time.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    private async Task ShowDialogAsync()
    {
        await _dialogSemaphore.WaitAsync();

        try
        {
            // Ensure that the XamlRoot is available before showing dialogs.
            if (XamlRoot is null)
            {
                return;
            }

            // Process the dialog queue and show dialogs one by one.
            while (_dialogQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                    case DialogMessage dialogMessage:
                        await ShowDialogForMessageAsync(dialogMessage, XamlRoot);
                        break;
                    case MkvPropeditArgumentsDialogMessage mkvMessage:
                        await ShowDialogForMessageAsync(mkvMessage, XamlRoot);
                        break;
                    case ExceptionDialogMessage exceptionMessage:
                        await ShowDialogForMessageAsync(exceptionMessage, XamlRoot);
                        break;
                    default:
                        // Unknown message type.
                        Debug.WriteLine($"Unknown dialog message type: {message.GetType().FullName}");
                        break;
                }
            }
        }
        finally
        {
            _dialogSemaphore.Release();
        }
    }

    /// <summary>
    /// Displays a simple dialog with the specified message and title.
    /// </summary>
    /// <param name="message">The <see cref="DialogMessage"/> containing the title and message text to display in the dialog.</param>
    /// <param name="xamlRoot">The <see cref="XamlRoot"/> that defines the UI context in which the dialog is displayed.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    private static async Task ShowDialogForMessageAsync(DialogMessage message, XamlRoot xamlRoot)
    {
        await new ContentDialog
        {
            Title = message.Title,
            Content = message.Message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot,
        }.ShowAsync();
    }

    /// <summary>
    /// Displays an exception dialog with full error details and options to copy, continue, or exit.
    /// </summary>
    /// <param name="message">The <see cref="ExceptionDialogMessage"/> containing the exception details to display.</param>
    /// <param name="xamlRoot">The <see cref="XamlRoot"/> that defines the UI context in which the dialog is displayed.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    private static async Task ShowDialogForMessageAsync(ExceptionDialogMessage message, XamlRoot xamlRoot)
    {
        var viewModel = App.GetService<ErrorDialogViewModel>();
        viewModel.Initialize(message.Title, message.Summary, message.Exception, message.Timestamp);

        var dialog = new ErrorDialog(viewModel)
        {
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();

        if (dialog.ShouldExit)
        {
            Application.Current.Exit();
        }
    }

    /// <summary>
    /// Displays a dialog showing the command-line arguments that will be used with mkvpropedit.
    /// </summary>
    /// <param name="message">The message containing the command-line arguments to display in the dialog.</param>
    /// <param name="xamlRoot">The <see cref="XamlRoot"/> that defines the UI context in which the dialog is displayed.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    private static async Task ShowDialogForMessageAsync(MkvPropeditArgumentsDialogMessage message, XamlRoot xamlRoot)
    {
        await new MkvPropeditArgumentsDialog
        {
            ArgumentsText = message.Arguments,
            XamlRoot = xamlRoot,
        }.ShowAsync();
    }

    /// <summary>
    /// Closes the support flyout when its button opens the support page.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private void SupportFlyoutButton_Click(object sender, RoutedEventArgs args) => SupportFlyout.Hide();

    /// <summary>
    /// Keeps the support heart filled while its panel is open.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private void SupportFlyout_Opened(object? sender, object args) => SupportHeart.SetIsHighlighted(SupportNavItem, true);

    /// <summary>
    /// Lets the support heart return to its outline once its panel is closed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private void SupportFlyout_Closed(object? sender, object args) => SupportHeart.SetIsHighlighted(SupportNavItem, false);

    /// <summary>
    /// Handles the Loaded event of the MainPage, initializing the default navigation view item.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private void MainPage_Loaded(object sender, RoutedEventArgs args)
    {
        // Only set the default navigation view item if not already loaded.
        if (contentFrame.Content == null)
        {
            NavigationView.SelectedItem = InputPageNavItem;
        }
    }
}
