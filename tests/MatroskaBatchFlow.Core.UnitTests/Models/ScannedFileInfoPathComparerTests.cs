using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

public class ScannedFileInfoPathComparerTests
{
    private readonly ScannedFileInfoPathComparer _windowsComparer;
    private readonly ScannedFileInfoPathComparer _linuxComparer;

    public ScannedFileInfoPathComparerTests()
    {
        var windowsPlatform = Substitute.For<IPlatformService>();
        windowsPlatform.IsWindows().Returns(true);
        _windowsComparer = new ScannedFileInfoPathComparer(windowsPlatform);

        var linuxPlatform = Substitute.For<IPlatformService>();
        linuxPlatform.IsWindows().Returns(false);
        _linuxComparer = new ScannedFileInfoPathComparer(linuxPlatform);
    }
    
    // Helper to create ScannedFileInfo with required Result
    private static ScannedFileInfo CreateFile(string path) => 
        new(new MediaInfoResultBuilder().Build(), path);

    // Theory-Based Cross-Platform Behavior Tests
    // These tests verify both Windows and Linux behavior regardless of the current platform

    [Theory]
    [InlineData(true, "C:\\Test.mkv", "c:\\test.mkv", true)]     // Windows: case-insensitive
    [InlineData(true, "C:\\Test.MKV", "C:\\TEST.MKV", true)]     // Windows: extension case
    [InlineData(true, "C:\\Path\\File.mkv", "c:\\path\\file.mkv", true)] // Windows: full path
    [InlineData(true, "D:\\Videos\\Movie.mkv", "D:\\VIDEOS\\MOVIE.MKV", true)] // Windows: multiple dirs
    [InlineData(false, "/home/test/File.mkv", "/home/test/file.mkv", false)] // Linux: case-sensitive
    [InlineData(false, "/home/Test.mkv", "/home/Test.mkv", true)]            // Linux: exact match
    [InlineData(false, "/home/test/FILE.MKV", "/home/test/FILE.MKV", true)]  // Linux: same case
    [InlineData(false, "/mnt/videos/Movie.mkv", "/mnt/VIDEOS/Movie.mkv", false)] // Linux: dir case matters
    [InlineData(false, "/var/log/test.mkv", "/var/LOG/test.mkv", false)] // Linux: different dirs
    [InlineData(false, "/usr/local/bin/file.mkv", "/usr/local/bin/file.mkv", true)] // Linux: exact match
    [InlineData(false, "/tmp/Test.MKV", "/tmp/test.mkv", false)] // Linux: case differs
    [InlineData(true, "test.mkv", "TEST.MKV", true)]             // Windows: relative path
    [InlineData(false, "test.mkv", "TEST.mkv", false)]           // Linux: relative path
    [InlineData(false, "./test.mkv", "./test.mkv", true)]        // Linux: relative with dot
    [InlineData(false, "../parent/test.mkv", "../parent/Test.mkv", false)] // Linux: parent dir
    public void Equals_ComparisonMode_BehavesCorrectly(
        bool isWindows,
        string path1,
        string path2,
        bool expectedEqual)
    {
        // Arrange
        var platform = Substitute.For<IPlatformService>();
        platform.IsWindows().Returns(isWindows);
        var comparer = new ScannedFileInfoPathComparer(platform);
        var file1 = CreateFile(path1);
        var file2 = CreateFile(path2);

        // Act
        bool result = comparer.Equals(file1, file2);

        // Assert
        Assert.Equal(expectedEqual, result);
    }

    [Theory]
    [InlineData(true, "C:\\Test.mkv", "c:\\test.mkv", true)]     // Windows: same hash
    [InlineData(true, "D:\\VIDEO.MKV", "d:\\video.mkv", true)]   // Windows: all caps vs lowercase
    [InlineData(false, "/home/Test.mkv", "/home/test.mkv", false)]          // Linux: different hash
    [InlineData(false, "/mnt/VIDEO.MKV", "/mnt/video.mkv", false)]          // Linux: all caps vs lowercase
    [InlineData(false, "/var/log/test.mkv", "/var/log/test.mkv", true)]    // Linux: same path
    [InlineData(false, "/usr/local/FILE.mkv", "/usr/local/file.mkv", false)] // Linux: case differs
    [InlineData(true, "relative\\Path.mkv", "RELATIVE\\path.mkv", true)]   // Windows: relative
    [InlineData(false, "relative/Path.mkv", "relative/path.mkv", false)]   // Linux: relative
    public void GetHashCode_ComparisonMode_BehavesCorrectly(
        bool isWindows,
        string path1,
        string path2,
        bool shouldHaveSameHash)
    {
        // Arrange
        var platform = Substitute.For<IPlatformService>();
        platform.IsWindows().Returns(isWindows);
        var comparer = new ScannedFileInfoPathComparer(platform);
        var file1 = CreateFile(path1);
        var file2 = CreateFile(path2);

        // Act
        int hash1 = comparer.GetHashCode(file1);
        int hash2 = comparer.GetHashCode(file2);

        // Assert
        if (shouldHaveSameHash)
        {
            Assert.Equal(hash1, hash2);
        }
        else
        {
            Assert.NotEqual(hash1, hash2);
        }
    }

    [Theory]
    [InlineData(true, "C:\\file1.mkv", "c:\\FILE1.MKV", 1)] // Windows: treats as same
    [InlineData(true, "test.mkv", "TEST.MKV", 1)]           // Windows: relative path
    [InlineData(false, "/home/file1.mkv", "/home/FILE1.MKV", 2)]       // Linux: treats as different
    [InlineData(false, "test.mkv", "TEST.mkv", 2)]                     // Linux: relative path
    [InlineData(false, "/var/test.mkv", "/VAR/test.mkv", 2)]          // Linux: dir case differs
    [InlineData(false, "/tmp/Movie.mkv", "/tmp/movie.mkv", 2)]        // Linux: file case differs
    [InlineData(false, "/home/user/file.mkv", "/home/user/file.mkv", 1)] // Linux: exact match
    [InlineData(true, "D:\\Path\\FILE.mkv", "d:\\path\\file.mkv", 1)] // Windows: full path
    public void HashSet_ComparisonMode_AddsCorrectNumberOfItems(
        bool isWindows,
        string path1,
        string path2,
        int expectedCount)
    {
        // Arrange
        var platform = Substitute.For<IPlatformService>();
        platform.IsWindows().Returns(isWindows);
        var comparer = new ScannedFileInfoPathComparer(platform);
        var hashSet = new HashSet<ScannedFileInfo>(comparer);
        var file1 = CreateFile(path1);
        var file2 = CreateFile(path2);

        // Act
        hashSet.Add(file1);
        hashSet.Add(file2);

        // Assert
        Assert.Equal(expectedCount, hashSet.Count);
    }

    // Edge Case Tests

    [Theory]
    [InlineData(true)]  // Windows comparer
    [InlineData(false)] // Linux comparer
    public void Equals_SameReference_ReturnsTrue(bool isWindows)
    {
        var comparer = isWindows ? _windowsComparer : _linuxComparer;
        var file = CreateFile("/test.mkv");

        bool result = comparer.Equals(file, file);

        Assert.True(result);
    }

    [Theory]
    [InlineData(true)]  // Windows comparer
    [InlineData(false)] // Linux comparer
    public void Equals_BothNull_ReturnsTrue(bool isWindows)
    {
        var comparer = isWindows ? _windowsComparer : _linuxComparer;

        bool result = comparer.Equals(null, null);

        Assert.True(result);
    }

    [Theory]
    [InlineData(true)]  // Windows comparer
    [InlineData(false)] // Linux comparer
    public void Equals_FirstNull_ReturnsFalse(bool isWindows)
    {
        var comparer = isWindows ? _windowsComparer : _linuxComparer;
        var file = CreateFile("/test.mkv");

        bool result = comparer.Equals(null, file);

        Assert.False(result);
    }

    [Theory]
    [InlineData(true)]  // Windows comparer
    [InlineData(false)] // Linux comparer
    public void Equals_SecondNull_ReturnsFalse(bool isWindows)
    {
        var comparer = isWindows ? _windowsComparer : _linuxComparer;
        var file = CreateFile("/test.mkv");

        bool result = comparer.Equals(file, null);

        Assert.False(result);
    }

    [Theory]
    [InlineData(true)]  // Windows comparer
    [InlineData(false)] // Linux comparer
    public void GetHashCode_NullObject_ReturnsZero(bool isWindows)
    {
        var comparer = isWindows ? _windowsComparer : _linuxComparer;

        int hash = comparer.GetHashCode(null!);

        Assert.Equal(0, hash);
    }

    [Fact]
    public void GetHashCode_NullPath_ThrowsArgumentNullException()
    {
        var mediaInfo = new MediaInfoResultBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => new ScannedFileInfo(mediaInfo, null!));
    }

    // Integration Tests

    [Fact]
    public void WorksWithDictionary_WindowsBehavior()
    {
        var dict = new Dictionary<ScannedFileInfo, string>(_windowsComparer);

        var file1 = CreateFile("C:\\test.mkv");
        var file2 = CreateFile("c:\\TEST.MKV"); // Same path, different case

        dict[file1] = "value1";
        dict[file2] = "value2"; // Should overwrite due to case-insensitive comparison

        Assert.Single(dict);
        Assert.Equal("value2", dict[file1]); // Can access with either key
    }

    [Fact]
    public void WorksWithDictionary_LinuxBehavior()
    {
        var dict = new Dictionary<ScannedFileInfo, string>(_linuxComparer);

        var file1 = CreateFile("/home/user/test.mkv");
        var file2 = CreateFile("/home/user/TEST.MKV"); // Same path, different case

        dict[file1] = "value1";
        dict[file2] = "value2"; // Should create separate entries due to case-sensitive comparison

        Assert.Equal(2, dict.Count);
        Assert.Equal("value1", dict[file1]);
        Assert.Equal("value2", dict[file2]);
    }
}
