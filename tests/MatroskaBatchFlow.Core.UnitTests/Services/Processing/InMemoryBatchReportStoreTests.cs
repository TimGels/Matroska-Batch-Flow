using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services.Processing;

/// <summary>
/// Contains unit tests for the InMemoryBatchReportStore class.
/// </summary>
public class InMemoryBatchReportStoreTests
{
    [Fact]
    public void Constructor_InitializesActiveBatchAndBatchesCollection()
    {
        // Arrange & Act
        var store = new InMemoryBatchReportStore();

        // Assert
        Assert.NotNull(store.ActiveBatch);
        Assert.NotNull(store.Batches);
        Assert.Empty(store.Batches);
    }

    [Fact]
    public void CreateBatch_AddsNewBatchToCollection()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();

        // Act
        var batch = store.CreateBatch();

        // Assert
        Assert.NotNull(batch);
        Assert.Single(store.Batches);
        Assert.Contains(batch, store.Batches);
    }

    [Fact]
    public void CreateBatch_ReturnsNewBatchWithUniqueId()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();

        // Act
        var batch1 = store.CreateBatch();
        var batch2 = store.CreateBatch();

        // Assert
        Assert.NotEqual(batch1.Id, batch2.Id);
        Assert.Equal(2, store.Batches.Count);
    }

    [Fact]
    public void SetActiveBatch_UpdatesActiveBatch()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var newBatch = new BatchExecutionReport();

        // Act
        store.SetActiveBatch(newBatch);

        // Assert
        Assert.Equal(newBatch, store.ActiveBatch);
    }

    [Fact]
    public void SetActiveBatch_RaisesPropertyChangedEvent()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var newBatch = new BatchExecutionReport();
        string? propertyName = null;
        store.PropertyChanged += (_, e) => propertyName = e.PropertyName;

        // Act
        store.SetActiveBatch(newBatch);

        // Assert
        Assert.Equal(nameof(store.ActiveBatch), propertyName);
    }

    [Fact]
    public void SetActiveBatch_DoesNotRaisePropertyChangedWhenSettingSameBatch()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var batch = new BatchExecutionReport();
        store.SetActiveBatch(batch);
        
        var eventRaised = false;
        store.PropertyChanged += (_, _) => eventRaised = true;

        // Act
        store.SetActiveBatch(batch);

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void SetActiveBatch_ThrowsArgumentNullException_WhenBatchIsNull()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => store.SetActiveBatch(null!));
    }

    [Fact]
    public void GetBatchById_ReturnsCorrectBatch()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var batch1 = store.CreateBatch();
        var batch2 = store.CreateBatch();

        // Act
        var result = store.GetBatchById(batch2.Id);

        // Assert
        Assert.Equal(batch2, result);
    }

    [Fact]
    public void GetBatchById_ReturnsNull_WhenBatchNotFound()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = store.GetBatchById(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RemoveBatch_RemovesBatchFromCollection()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var batch = store.CreateBatch();
        Assert.Single(store.Batches);

        // Act
        store.RemoveBatch(batch);

        // Assert
        Assert.Empty(store.Batches);
    }

    [Fact]
    public void RemoveBatch_ClearsBatchData()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var batch = store.CreateBatch();
        var fileReport = new FileProcessingReport
        {
            SourceFile = new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "test.mkv"),
            Path = "test.mkv"
        };
        batch.TryAddFileReport(fileReport);
        Assert.Single(batch.FileReports);

        // Act
        store.RemoveBatch(batch);

        // Assert
        Assert.Empty(batch.FileReports);
    }

    [Fact]
    public void RemoveBatch_ThrowsArgumentNullException_WhenBatchIsNull()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => store.RemoveBatch(null!));
    }

    [Fact]
    public void Reset_ClearsAllBatchesAndResetsActiveBatch()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var batch1 = store.CreateBatch();
        var batch2 = store.CreateBatch();
        store.SetActiveBatch(batch1);
        
        Assert.Equal(2, store.Batches.Count);
        Assert.Equal(batch1, store.ActiveBatch);

        // Act
        store.Reset();

        // Assert
        Assert.Empty(store.Batches);
        Assert.NotEqual(batch1, store.ActiveBatch);
        Assert.NotNull(store.ActiveBatch);
    }

    [Fact]
    public void Reset_ClearsAllBatchFileReports()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();
        var batch = store.CreateBatch();
        var fileReport = new FileProcessingReport
        {
            SourceFile = new ScannedFileInfo(new MediaInfoResultBuilder().Build(), "test.mkv"),
            Path = "test.mkv"
        };
        batch.TryAddFileReport(fileReport);

        // Act
        store.Reset();

        // Assert
        Assert.Empty(batch.FileReports);
    }

    [Fact]
    public void Batches_IsReadOnly()
    {
        // Arrange
        var store = new InMemoryBatchReportStore();

        // Act & Assert
        Assert.IsAssignableFrom<System.Collections.ObjectModel.ReadOnlyObservableCollection<BatchExecutionReport>>(store.Batches);
    }
}
