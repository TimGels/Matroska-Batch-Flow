using System.Collections.Specialized;
using MatroskaBatchFlow.Core.Utilities;

namespace MatroskaBatchFlow.Core.UnitTests.Utilities;

public class UniqueObservableCollectionTests
{
    // Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyCollection()
    {
        var collection = new UniqueObservableCollection<int>();

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithItems_AddsUniqueItems()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        Assert.Equal(3, collection.Count);
        Assert.Contains(1, collection);
        Assert.Contains(2, collection);
        Assert.Contains(3, collection);
    }

    [Fact]
    public void Constructor_WithDuplicateItems_IgnoresDuplicates()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 2, 3, 1]);

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Constructor_WithCustomComparer_UsesComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var collection = new UniqueObservableCollection<string>(["A", "a", "B"], comparer);

        Assert.Equal(2, collection.Count); // "A" and "a" are equal
    }

    // Add Tests

    [Fact]
    public void Add_NewItem_ReturnsTrue()
    {
        var collection = new UniqueObservableCollection<int>();

        bool result = collection.Add(1);

        Assert.True(result);
        Assert.Single(collection);
    }

    [Fact]
    public void Add_DuplicateItem_ReturnsFalse()
    {
        var collection = new UniqueObservableCollection<int>([1]);

        bool result = collection.Add(1);

        Assert.False(result);
        Assert.Single(collection);
    }

    [Fact]
    public void Add_NewItem_RaisesCollectionChanged()
    {
        var collection = new UniqueObservableCollection<int>();
        var eventRaised = false;
        collection.CollectionChanged += (s, e) =>
        {
            eventRaised = true;
            Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
            Assert.Equal(1, e.NewItems![0]);
        };

        collection.Add(1);

        Assert.True(eventRaised);
    }

    [Fact]
    public void Add_DuplicateItem_DoesNotRaiseCollectionChanged()
    {
        var collection = new UniqueObservableCollection<int>([1]);
        var eventRaised = false;
        collection.CollectionChanged += (s, e) => eventRaised = true;

        collection.Add(1);

        Assert.False(eventRaised);
    }

    // AddRange Tests

    [Fact]
    public void AddRange_AddsOnlyUniqueItems()
    {
        var collection = new UniqueObservableCollection<int>([1, 2]);

        collection.AddRange([2, 3, 4]);

        Assert.Equal(4, collection.Count);
        Assert.Contains(3, collection);
        Assert.Contains(4, collection);
    }

    [Fact]
    public void AddRange_EmptyCollection_AddsAllItems()
    {
        var collection = new UniqueObservableCollection<int>();

        collection.AddRange([1, 2, 3]);

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void AddRange_WithDuplicatesInInput_IgnoresDuplicates()
    {
        var collection = new UniqueObservableCollection<int>();

        collection.AddRange([1, 1, 2, 2, 3]);

        Assert.Equal(3, collection.Count);
    }

    [Theory]
    [InlineData(new int[] { 1, 2, 3 })]
    [InlineData(new int[] { 2, 3, 4, 5, 6, 7, 8 })]
    public void AddRange_RaisesCollectionChanged_OnlyOnce(IEnumerable<int> itemsToAdd)
    {
        var collection = new UniqueObservableCollection<int>();
        var eventRaisedCount = 0;
        collection.CollectionChanged += (s, e) =>
        {
            eventRaisedCount++;
            Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
        };
        collection.AddRange(itemsToAdd);
        Assert.Equal(1, eventRaisedCount);
    }

    // Insert Tests

    [Fact]
    public void Insert_NewItem_InsertsAtIndex()
    {
        var collection = new UniqueObservableCollection<int>([1, 3]);

        collection.Insert(1, 2);

        Assert.Equal(3, collection.Count);
        Assert.Equal(2, collection[1]);
    }

    [Fact]
    public void Insert_DuplicateItem_DoesNotInsert()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        collection.Insert(1, 2);

        Assert.Equal(3, collection.Count);
        Assert.Equal(2, collection[1]); // Unchanged
    }

    // Remove Tests

    [Fact]
    public void Remove_ExistingItem_RemovesAndReturnsTrue()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        bool result = collection.Remove(2);

        Assert.True(result);
        Assert.Equal(2, collection.Count);
        Assert.DoesNotContain(2, collection);
    }

    [Fact]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        bool result = collection.Remove(4);

        Assert.False(result);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Remove_AllowsReaddingSameItem()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        collection.Remove(2);
        bool result = collection.Add(2);

        Assert.True(result);
        Assert.Contains(2, collection);
    }

    [Fact]
    public void RemoveAt_RemovesItemAtIndex()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        collection.RemoveAt(1);

        Assert.Equal(2, collection.Count);
        Assert.DoesNotContain(2, collection);
    }

    // RemoveRange Tests

    [Fact]
    public void RemoveRange_RemovesMatchingItems()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3, 4]);

        collection.RemoveRange([2, 4, 5]);

        Assert.Equal(2, collection.Count);
        Assert.Contains(1, collection);
        Assert.Contains(3, collection);
        Assert.DoesNotContain(2, collection);
        Assert.DoesNotContain(4, collection);
    }

    [Fact]
    public void RemoveRange_WhenNoMatches_DoesNotRaiseCollectionChanged()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        collection.RemoveRange([4, 5]);

        Assert.False(eventRaised);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void RemoveRange_RaisesCollectionChanged_OnlyOnce()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3, 4]);
        var eventRaisedCount = 0;
        collection.CollectionChanged += (_, e) =>
        {
            eventRaisedCount++;
            Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
        };

        collection.RemoveRange([1, 3]);

        Assert.Equal(1, eventRaisedCount);
    }

    // Clear Tests

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void Clear_AllowsReaddingItems()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);
        collection.Clear();

        bool result = collection.Add(1);

        Assert.True(result);
        Assert.Single(collection);
    }

    [Fact]
    public void Clear_RaisesResetEvent()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (s, e) =>
        {
            eventRaised = true;
            Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
        };

        collection.Clear();

        Assert.True(eventRaised);
    }

    // SetItem (Indexer) Tests

    [Fact]
    public void SetItem_WithEquivalentItem_Replaces()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var collection = new UniqueObservableCollection<string>(["A", "B"], comparer);

        collection[0] = "a"; // Equivalent to "A"

        Assert.Equal("a", collection[0]);
    }

    [Fact]
    public void SetItem_WithNewUniqueItem_Replaces()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        collection[1] = 5;

        Assert.Equal(5, collection[1]);
        Assert.DoesNotContain(2, collection);
    }

    [Fact]
    public void SetItem_WithExistingDuplicate_DoesNotReplace()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        collection[1] = 3; // 3 already exists at index 2

        Assert.Equal(2, collection[1]); // Unchanged
    }

    [Fact]
    public void SetItem_WithEquivalentItem_RaisesReplaceEvent()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var collection = new UniqueObservableCollection<string>(["A", "B"], comparer);
        var eventRaised = false;
        collection.CollectionChanged += (s, e) =>
        {
            eventRaised = true;
            Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
        };

        collection[0] = "a";

        Assert.True(eventRaised);
    }

    // Custom Comparer Tests

    [Fact]
    public void CustomComparer_PathBasedEquality_PreventsDuplicatePaths()
    {
        var comparer = new PathComparer();
        var collection = new UniqueObservableCollection<FileItem>(comparer);

        var file1 = new FileItem("c:\\test.mkv", "Instance1");
        var file2 = new FileItem("c:\\test.mkv", "Instance2"); // Same path, different instance

        collection.Add(file1);
        bool result = collection.Add(file2);

        Assert.False(result);
        Assert.Single(collection);
        Assert.Equal("Instance1", collection[0].Name);
    }

    private record FileItem(string Path, string Name);

    private class PathComparer : IEqualityComparer<FileItem>
    {
        public bool Equals(FileItem? x, FileItem? y) =>
            string.Equals(x?.Path, y?.Path, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(FileItem obj) =>
            obj.Path.ToUpperInvariant().GetHashCode();
    }

    // Contains Tests

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        Assert.Contains(2, collection);
    }

    [Fact]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        Assert.DoesNotContain(4, collection);
    }

    // IndexOf Tests

    [Fact]
    public void IndexOf_ExistingItem_ReturnsIndex()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        Assert.Equal(1, collection.IndexOf(2));
    }

    [Fact]
    public void IndexOf_NonExistingItem_ReturnsNegativeOne()
    {
        var collection = new UniqueObservableCollection<int>([1, 2, 3]);

        Assert.Equal(-1, collection.IndexOf(4));
    }
}

