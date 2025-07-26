using System.Collections;

namespace MatroskaBatchFlow.Uno.Behavior;

/// <summary>
/// Provides behavior for binding the selected items of a ListView to an external IList.
/// This allows synchronization between the ListView's selected items and a bound collection.
/// </summary>
public static class ListViewSelectedItemsBehavior
{
    /// <summary>
    /// Dependency property for binding the selected items of a ListView.
    /// </summary>
    public static readonly DependencyProperty BoundSelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "BoundSelectedItems",
            typeof(IList),
            typeof(ListViewSelectedItemsBehavior),
            new PropertyMetadata(null, OnBoundSelectedItemsChanged));

    /// <summary>
    /// Sets the bound selected items for the specified dependency object.
    /// </summary>
    /// <param name="element">The dependency object to set the property on.</param>
    /// <param name="value">The IList to bind to the selected items.</param>
    public static void SetBoundSelectedItems(DependencyObject element, IList value)
    {
        element.SetValue(BoundSelectedItemsProperty, value);
    }

    /// <summary>
    /// Gets the bound selected items for the specified dependency object.
    /// </summary>
    /// <param name="element">The dependency object to get the property from.</param>
    /// <returns>The IList bound to the selected items.</returns>
    public static IList GetBoundSelectedItems(DependencyObject element)
    {
        return (IList)element.GetValue(BoundSelectedItemsProperty);
    }

    /// <summary>
    /// Subscribes and unsubscribes to the SelectionChanged and Unloaded events of the ListView.
    /// </summary>
    /// <param name="dependencyObject">The dependency object where the property changed.</param>
    /// <param name="e">Event arguments containing the old and new values.</param>
    private static void OnBoundSelectedItemsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is ListView listView)
        {
            // Unsubscribe from previous event handlers
            listView.SelectionChanged -= OnListViewSelectionChanged;
            listView.Unloaded -= OnListViewUnloaded;

            // Subscribe to new event handlers
            listView.SelectionChanged += OnListViewSelectionChanged;
            listView.Unloaded += OnListViewUnloaded;
        }
    }

    /// <summary>
    /// Handles the Unloaded event of the ListView to clean up event handlers.
    /// </summary>
    /// <param name="sender">The ListView being unloaded.</param>
    /// <param name="e">Event arguments for the Unloaded event.</param>
    private static void OnListViewUnloaded(object sender, RoutedEventArgs e)
    {
        // TODO: See if uploading is necessary. Disabling this for now as it breaks the behavior after navigation.
        //Debug.WriteLine("unloading listview listener");
        //if (sender is ListView listView)
        //{
        //    listView.SelectionChanged -= OnListViewSelectionChanged;
        //}
    }

    /// <summary>
    /// Handles the SelectionChanged event of the ListView to synchronize the bound IList.
    /// </summary>
    /// <param name="sender">The ListView whose selection changed.</param>
    /// <param name="e">Event arguments containing the added and removed items.</param>
    private static void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListView listView)
            return;

        var boundList = GetBoundSelectedItems(listView);
        if (boundList == null)
            return;

        // Remove items that are no longer selected
        foreach (var item in e.RemovedItems)
            boundList.Remove(item);

        // Add newly selected items
        boundList.AddRange(e.AddedItems);

    }
}
