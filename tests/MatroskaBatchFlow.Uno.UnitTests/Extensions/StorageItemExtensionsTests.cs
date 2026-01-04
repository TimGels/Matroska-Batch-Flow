using MatroskaBatchFlow.Uno.Extensions;
using NSubstitute;
using Windows.Storage;

namespace MatroskaBatchFlow.Uno.UnitTests.Extensions;

/// <summary>
/// Contains unit tests for extension methods that convert IStorageItem instances to FileInfo objects.
/// </summary>
/// <remarks>These tests verify the correct behavior of the ToFileInfo extension methods, including handling of
/// valid and invalid input, null arguments, and collections of storage items. The tests ensure that exceptions are
/// thrown as expected for invalid scenarios and that the returned FileInfo objects accurately reflect the underlying
/// storage item paths.</remarks>
public class StorageItemExtensionsTests
{
    [Fact]
    public void ToFileInfo_ValidStorageItem_ReturnsFileInfo()
    {
        // Arrange
        var expectedPath = @"C:\test\file.txt";
        var mock = Substitute.For<IStorageItem>();
        mock.Path.Returns(expectedPath);

        // Act
        var fileInfo = mock.ToFileInfo();

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
        var mock = Substitute.For<IStorageItem>();
        mock.Path.Returns(invalidPath!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mock.ToFileInfo());
    }

    [Fact]
    public void ToFileInfo_Enumerable_ValidItems_ReturnsFileInfoArray()
    {
        // Arrange
        var mock1 = Substitute.For<IStorageItem>();
        mock1.Path.Returns(@"C:\file1.txt");
        var mock2 = Substitute.For<IStorageItem>();
        mock2.Path.Returns(@"C:\file2.txt");
        var items = new[] { mock1, mock2 };

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
        var mock1 = Substitute.For<IStorageItem>();
        mock1.Path.Returns(@"C:\file1.txt");
        var mock2 = Substitute.For<IStorageItem>();
        mock2.Path.Returns(x => null!);
        var items = new[] { mock1, mock2 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => items.ToFileInfo());
    }
}
