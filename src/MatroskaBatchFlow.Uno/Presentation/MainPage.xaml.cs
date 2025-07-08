using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<DialogMessage>(this, async (r, m) =>
        {
            await new ErrorDialog
            {
                Title = m.Title,
                MessageText = m.Message,
                XamlRoot = XamlRoot,
            }.ShowAsync();
        });

        this.Loaded += MainPage_Loaded;
    }
    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        // No automatic navigation to Settings on load
        //this.Navigator()?.NavigateRouteAsync(this, "Settings");
    }

    // Handles navigation when a NavigationView item is invoked
    private void MainNavigationView_ItemInvoked_1(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            this.Navigator()?.NavigateRouteAsync(this, "Settings");
            //_ = this.Navigator()?.NavigateViewModelAsync<SettingsViewModel>(this);
        }
    }

    private void nvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            contentFrame.Navigate(typeof(SettingsPage));
        } else
        {
            var selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            if (selectedItem != null)
            {
                string selectedItemTag = ((string)selectedItem.Tag);
                //sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                string pageName = "MatroskaBatchFlow.Uno.Presentation." + selectedItemTag;
                Type pageType = Type.GetType(pageName);
                contentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
            }
        }
    }
}

