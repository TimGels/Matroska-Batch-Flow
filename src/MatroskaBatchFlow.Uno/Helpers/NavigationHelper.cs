namespace MatroskaBatchFlow.Uno.Helpers;

// Helper class to set the navigation target for a NavigationViewItem.
//
// Usage in XAML:
// <NavigationViewItem x:Uid="Shell_Main" Icon="Document" helpers:NavigationHelper.NavigateTo="AppName.ViewModels.MainViewModel" />
// Usage in XAML with default navigation:
// <NavigationViewItem x:Uid="Shell_Main" Icon="Document" helpers:NavigationHelper.NavigateTo="AppName.ViewModels.MainViewModel" helpers:NavigationHelper.IsDefault="True" />
// Usage in code:
// NavigationHelper.SetNavigateTo(navigationViewItem, typeof(MainViewModel).FullName);
public static class NavigationHelper
{
    public static string GetNavigateTo(NavigationViewItem item) => (string)item.GetValue(NavigateToProperty);

    public static void SetNavigateTo(NavigationViewItem item, string value) => item.SetValue(NavigateToProperty, value);

    public static bool GetIsDefault(NavigationViewItem item) => (bool)item.GetValue(IsDefaultProperty);

    public static void SetIsDefault(NavigationViewItem item, bool value) => item.SetValue(IsDefaultProperty, value);

    public static readonly DependencyProperty NavigateToProperty =
        DependencyProperty.RegisterAttached("NavigateTo", typeof(string), typeof(NavigationHelper), new PropertyMetadata(null));

    public static readonly DependencyProperty IsDefaultProperty =
        DependencyProperty.RegisterAttached("IsDefault", typeof(bool), typeof(NavigationHelper), new PropertyMetadata(false));
}
