using MatroskaBatchFlow.Uno.Extensions;
using Moq;
using Windows.Storage;

namespace MatroskaBatchFlow.Uno.Tests.Extensions;

public class StorageItemExtensionsTests
{
    [Fact]
    public void ToFileInfo_ValidStorageItem_ReturnsFileInfo()
    {
        // Arrange
        var expectedPath = @"C:\test\file.txt";
        var mock = new Mock<IStorageItem>();
        mock.SetupGet(x => x.Path).Returns(expectedPath);

        // Act
        var fileInfo = mock.Object.ToFileInfo();

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal(expectedPath, fileInfo.FullName);
    }

    [Fact]
    public void ToFileInfo_NullStorageItem_ThrowsArgumentNullException()
    {
        // Arrange
        IStorageItem storageItem = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StorageItemExtensions.ToFileInfo(storageItem));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToFileInfo_InvalidPath_ThrowsInvalidOperationException(string? invalidPath)
    {
        // Arrange
        var mock = new Mock<IStorageItem>();
        mock.SetupGet(x => x.Path).Returns(invalidPath!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mock.Object.ToFileInfo());
    }

    [Fact]
    public void ToFileInfo_Enumerable_ValidItems_ReturnsFileInfoArray()
    {
        // Arrange
        var mock1 = new Mock<IStorageItem>();
        mock1.SetupGet(x => x.Path).Returns(@"C:\file1.txt");
        var mock2 = new Mock<IStorageItem>();
        mock2.SetupGet(x => x.Path).Returns(@"C:\file2.txt");
        var items = new[] { mock1.Object, mock2.Object };

        // Act
        var result = items.ToFileInfo();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(@"C:\file1.txt", result[0].FullName);
        Assert.Equal(@"C:\file2.txt", result[1].FullName);
    }

    [Fact]
    public void ToFileInfo_Enumerable_Null_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<IStorageItem> items = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StorageItemExtensions.ToFileInfo(items));
    }

    [Fact]
    public void ToFileInfo_Enumerable_ContainsInvalidItem_ThrowsInvalidOperationException()
    {
        // Arrange
        var mock1 = new Mock<IStorageItem>();
        mock1.SetupGet(x => x.Path).Returns(@"C:\file1.txt");
        var mock2 = new Mock<IStorageItem>();
        mock2.SetupGet(x => x.Path).Returns(() => null!);
        var items = new[] { mock1.Object, mock2.Object };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => items.ToFileInfo());
    }
}
