using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Windows.Foundation;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    private readonly ConcurrentQueue<DialogMessage> _dialogQueue = new();
    private readonly SemaphoreSlim _dialogSemaphore = new(1, 1);

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();

        ViewModel.NavigationService.Frame = contentFrame;
        ViewModel.NavigationViewService.Initialize(NavigationView);

        WeakReferenceMessenger.Default.Register<DialogMessage>(this, (r, m) =>
        {
            _dialogQueue.Enqueue(m);
            _ = ShowDialogAsync().ConfigureAwait(true);
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
            // Process the dialog queue and show dialogs one by one.
            while (_dialogQueue.TryDequeue(out var message))
            {
                await new ErrorDialog
                {
                    Title = message.Title,
                    MessageText = message.Message,
                    XamlRoot = XamlRoot,
                }.ShowAsync();
            }

        }
        finally
        {
            _dialogSemaphore.Release();
        }
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

