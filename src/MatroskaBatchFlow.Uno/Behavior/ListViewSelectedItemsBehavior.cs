using System.Collections;
using System.Collections.Specialized;

namespace MatroskaBatchFlow.Uno.Behavior;

/// <summary>
/// Provides behavior for binding the selected items of a ListView to an external IList.
/// This allows synchronization between the ListView's selected items and a bound collection.
/// </summary>
public static class ListViewSelectedItemsBehavior
{
    /// <summary>
    /// A mapping of bound collections to their associated <see cref="ListView"/> controls.
    /// </summary>
    /// <remarks>This dictionary is used to track the relationship between collections implementing  <see
    /// cref="INotifyCollectionChanged"/> and the <see cref="ListView"/> controls that are bound to them.</remarks>
    private static readonly Dictionary<INotifyCollectionChanged, ListView> _collectionToListView = [];

    /// <summary>
    /// A dictionary that tracks the synchronization state of <see cref="ListView"/> controls.
    /// </summary>
    /// <remarks>The key represents a <see cref="ListView"/> instance, and the value indicates whether the
    /// control is currently synchronizing. A value of <see langword="true"/> means the <see cref="ListView"/> is in a
    /// synchronization state; otherwise, <see langword="false"/>.</remarks>
    private static readonly Dictionary<ListView, bool> _isSynchronizing = [];

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
    /// <param name="eventArgs">Event arguments containing the old and new values.</param>
    private static void OnBoundSelectedItemsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not ListView listView)
        {
            return;
        }

        listView.SelectionChanged -= OnListViewSelectionChanged;
        listView.Unloaded -= OnListViewUnloaded;

        listView.SelectionChanged += OnListViewSelectionChanged;
        listView.Unloaded += OnListViewUnloaded;

        if (eventArgs.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnBoundCollectionChangedHandler;

            // Remove the old collection from the mapping.
            _collectionToListView.Remove(oldCollection);
            listView.SelectedItems.Clear();
        }

        if (eventArgs.NewValue is IList newList and INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnBoundCollectionChangedHandler;
            _collectionToListView[newCollection] = listView;

            // Add items from the bound collection to the ListView's selected items.
            foreach (object item in newList)
            {
                if (!listView.SelectedItems.Contains(item))
                    listView.SelectedItems.Add(item);
            }
        }
    }

    /// <summary>
    /// Handles the Unloaded event of the ListView to clean up event handlers.
    /// </summary>
    /// <param name="sender">The ListView being unloaded.</param>
    /// <param name="eventArgs">Event arguments for the Unloaded event.</param>
    private static void OnListViewUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        // TODO: See if uploading is necessary. Disabling this for now as it breaks the behavior after navigation.
        //if (sender is not ListView listView)
        //    return;

        //// Clean up SelectionChanged event
        //listView.SelectionChanged -= OnListViewSelectionChanged;
        //listView.Unloaded -= OnListViewUnloaded;

        //// Remove any collections associated with this ListView
        //foreach (var kvp in _collectionToListView.Where(kvp => kvp.Value == listView).ToList())
        //{
        //    kvp.Key.CollectionChanged -= OnBoundCollectionChangedHandler;
        //    _collectionToListView.Remove(kvp.Key);
        //}
    }

    /// <summary>
    /// Handles the <see cref="ListView.SelectionChanged"/> event to synchronize the selected items in a <see
    /// cref="ListView"/> with a bound collection.
    /// </summary>
    /// <remarks>This method ensures that the bound collection remains in sync with the selected items in
    /// the <see cref="ListView"/>. It prevents re-entrancy during synchronization by using a flag stored in the
    /// <see cref="_isSynchronizing"/> dictionary.</remarks>
    /// <param name="sender">The source of the event, expected to be a <see cref="ListView"/>.</param>
    /// <param name="eventArgs">The event data containing the items that were added to or removed from the selection.</param>
    private static void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (sender is not ListView listView)
            return;

        if (_isSynchronizing.TryGetValue(listView, out bool syncing) && syncing)
            return;

        // Prevent re-entrancy.
        _isSynchronizing[listView] = true;

        var boundList = GetBoundSelectedItems(listView);
        if (boundList is null)
        {
            _isSynchronizing[listView] = false;
            return;
        }

        // Remove items that were deselected in the ListView.
        foreach (var item in eventArgs.RemovedItems)
            boundList.Remove(item);

        // Add newly selected items to the bound list.
        foreach (var item in eventArgs.AddedItems)
            if (!boundList.Contains(item))
                boundList.Add(item);

        _isSynchronizing[listView] = false;
    }

    /// <summary>
    /// Handles changes to a bound collection and updates the associated <see cref="ListView"/> to reflect the changes in
    /// its selected items.
    /// </summary>
    /// <remarks>This method synchronizes the selected items of a <see cref="ListView"/> with the changes in a bound
    /// collection. It handles actions such as adding, removing, replacing, or resetting items in the collection. If the
    /// sender is not an <see cref="INotifyCollectionChanged"/> or if the associated <see cref="ListView"/> cannot be
    /// found, the method exits without making changes.</remarks>
    /// <param name="sender">The source of the event, which is expected to be an <see cref="INotifyCollectionChanged"/> instance.</param>
    /// <param name="eventArgs">The event data containing information about the changes to the collection.</param>
    private static void OnBoundCollectionChangedHandler(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (sender is not INotifyCollectionChanged collection)
            return;

        if (!_collectionToListView.TryGetValue(collection, out ListView? listView))
            return;

        if (listView is null || eventArgs == null)
            return;

        void AddItems(IList items)
        {
            foreach (var item in items)
            {
                if (!listView.SelectedItems.Contains(item))
                {
                    listView.SelectedItems.Add(item);
                }
            }
        }

        // Remove items from the ListView's selected items.
        void RemoveItems(IList items)
        {
            foreach (var item in items)
            {
                listView.SelectedItems.Remove(item);
            }
        }

        // Handle the different actions that can occur in the collection.
        switch (eventArgs.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                listView.SelectedItems.Clear();
                break;

            case NotifyCollectionChangedAction.Add:
                if (eventArgs.NewItems is { Count: > 0 })
                    AddItems(eventArgs.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove:
                if (eventArgs.OldItems is { Count: > 0 })
                    RemoveItems(eventArgs.OldItems);
                break;

            case NotifyCollectionChangedAction.Replace:
                if (eventArgs.OldItems is { Count: > 0 })
                    RemoveItems(eventArgs.OldItems);
                if (eventArgs.NewItems is { Count: > 0 })
                    AddItems(eventArgs.NewItems);
                break;
        }
    }
}
