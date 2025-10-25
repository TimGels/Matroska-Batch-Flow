using System.Collections.Concurrent;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Messages;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Windows.Foundation;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    private readonly ConcurrentQueue<object> _dialogQueue = new();
    private readonly SemaphoreSlim _dialogSemaphore = new(1, 1);

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();

        ViewModel.NavigationService.Frame = contentFrame;
        ViewModel.NavigationViewService.Initialize(NavigationView);

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
    /// Displays an error dialog with the specified message and title.
    /// </summary>
    /// <param name="message">The <see cref="DialogMessage"/> containing the title and message text to display in the dialog.</param>
    /// <param name="xamlRoot">The <see cref="XamlRoot"/> that defines the UI context in which the dialog is displayed.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    private static async Task ShowDialogForMessageAsync(DialogMessage message, XamlRoot xamlRoot)
    {
        await new ErrorDialog
        {
            Title = message.Title,
            MessageText = message.Message,
            XamlRoot = xamlRoot,
        }.ShowAsync();
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

