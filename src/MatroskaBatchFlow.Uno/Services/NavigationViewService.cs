using System.Diagnostics.CodeAnalysis;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Helpers;

namespace MatroskaBatchFlow.Uno.Services;

public class NavigationViewService(INavigationService navigationService, IPageService pageService) : INavigationViewService
{
    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    [MemberNotNull(nameof(_navigationView))]
    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
        _navigationView.Loaded += OnLoaded;
    }

    public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.BackRequested -= OnBackRequested;
            _navigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType) ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => navigationService.GoBack();

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            navigationService.NavigateTo(pageKey: typeof(SettingsViewModel).FullName!, transitionInfo: args.RecommendedNavigationTransitionInfo);
        } else
        {
            var selectedItem = args.InvokedItemContainer as NavigationViewItem;

            if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
            {
                navigationService.NavigateTo(pageKey: pageKey, transitionInfo: args.RecommendedNavigationTransitionInfo);
            }
        }
    }

    /// <summary>
    /// Handles the loading event of the navigation view, setting the selected item to the first default item.
    /// </summary>
    /// <remarks>This method sets the selected item in the navigation view to the first item marked as default
    /// and navigates to the associated page. It only performs these actions if the navigation frame is empty, ensuring
    /// that navigation occurs only once during the initial load.</remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The event data.</param>
    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (_navigationView == null)
            return;

        // Only set the default navigation view item if the frame is not already loaded.
        var frame = navigationService.Frame;
        if (frame?.Content != null)
            return;

        // Find the (first) default item.
        var defaultItem = _navigationView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => NavigationHelper.GetIsDefault(item));

        if (defaultItem == null)
            return;

        _navigationView.SelectedItem = defaultItem;
        if (defaultItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            navigationService.NavigateTo(pageKey);
        }
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return pageService.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }
}
