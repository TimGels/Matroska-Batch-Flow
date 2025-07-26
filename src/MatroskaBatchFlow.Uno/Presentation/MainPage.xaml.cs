using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Microsoft.UI.Xaml.Media.Animation;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }
    public MainPage(MainViewModel mainViewModel)
    {
        ViewModel = mainViewModel;
        this.InitializeComponent();

        ViewModel.NavigationService.Frame = contentFrame;
        ViewModel.NavigationViewService.Initialize(NavigationView);

        WeakReferenceMessenger.Default.Register<DialogMessage>(this, async (r, m) =>
        {
            await new ErrorDialog
            {
                Title = m.Title,
                MessageText = m.Message,
                XamlRoot = XamlRoot,
            }.ShowAsync();
        });
        Loaded += MainPage_Loaded;
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

