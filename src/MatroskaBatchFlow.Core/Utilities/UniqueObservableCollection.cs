using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MatroskaBatchFlow.Core.Utilities;

/// <summary>
/// An observable collection that enforces uniqueness of items based on an <see cref="IEqualityComparer{T}"/>.
/// </summary>
/// <remarks>
/// This collection extends <see cref="ObservableCollection{T}"/> and uses a <see cref="HashSet{T}"/> 
/// internally for O(1) duplicate detection. Duplicate items are silently ignored on add operations.
/// </remarks>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class UniqueObservableCollection<T> : ObservableCollection<T>
{
    private readonly HashSet<T> _hashSet;

    /// <summary>
    /// Initializes a new instance using the default equality comparer.
    /// </summary>
    public UniqueObservableCollection() : this(EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance using the specified equality comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use for determining item uniqueness.</param>
    public UniqueObservableCollection(IEqualityComparer<T> comparer)
    {
        _hashSet = new HashSet<T>(comparer ?? EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Initializes a new instance with the specified items using the default equality comparer.
    /// </summary>
    /// <param name="items">The initial items to add to the collection.</param>
    public UniqueObservableCollection(IEnumerable<T> items) : this(items, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified items and equality comparer.
    /// </summary>
    /// <param name="items">The initial items to add to the collection.</param>
    /// <param name="comparer">The equality comparer to use for determining item uniqueness.</param>
    public UniqueObservableCollection(IEnumerable<T> items, IEqualityComparer<T> comparer) : this(comparer)
    {
        AddRange(items);
    }

    /// <summary>
    /// Adds an item to the collection if it does not already exist.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns><see langword="true"/> if the item was added; <see langword="false"/> if it already exists.</returns>
    public new bool Add(T item)
    {
        if (_hashSet.Contains(item))
        {
            return false;
        }

        base.Add(item);

        return true;
    }

    /// <summary>
    /// Adds all items from the specified collection that are not already present.
    /// </summary>
    /// <param name="items">The collection of items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        var itemsToAdd = new List<T>();

        // Collect items that aren't duplicates
        foreach (var item in items)
        {
            if (_hashSet.Add(item))
            {
                itemsToAdd.Add(item);
            }
        }

        if (itemsToAdd.Count == 0)
            return;

        // Add to base collection without events
        var startIndex = Count;
        foreach (var item in itemsToAdd)
        {
            Items.Add(item);
        }

        // Fire single batch event
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemsToAdd, startIndex));
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, T item)
    {
        if (_hashSet.Add(item))
        {
            base.InsertItem(index, item);
        }
    }

    /// <inheritdoc/>
    protected override void ClearItems()
    {
        base.ClearItems();
        _hashSet.Clear();
    }

    /// <inheritdoc/>
    protected override void RemoveItem(int index)
    {
        var item = this[index];
        _hashSet.Remove(item);
        base.RemoveItem(index);
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, T item)
    {
        var oldItem = this[index];

        // If replacing with equivalent item, allow it
        if (_hashSet.Comparer.Equals(oldItem, item))
        {
            base.SetItem(index, item);
            return;
        }

        // If new item already exists elsewhere, reject
        if (_hashSet.Contains(item))
        {
            return;
        }

        _hashSet.Remove(oldItem);
        _hashSet.Add(item);
        base.SetItem(index, item);
    }
}

