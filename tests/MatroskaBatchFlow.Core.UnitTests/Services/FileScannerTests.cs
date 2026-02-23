using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

/// <summary>
/// Contains unit tests for the FileScanner class.
/// </summary>
public class FileScannerTests
{
    private readonly ILogger<FileScanner> _logger = Substitute.For<ILogger<FileScanner>>();
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "FileScannerTests");

    public FileScannerTests()
    {
        Directory.CreateDirectory(_testDirectory);
        CleanupTestDirectory();
    }

    [Fact]
    public async Task ScanAsync_ThrowsArgumentException_WhenFilesArrayIsNull()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => scanner.ScanAsync(null!));
    }

    [Fact]
    public async Task ScanAsync_ThrowsArgumentException_WhenFilesArrayIsEmpty()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => scanner.ScanAsync(Array.Empty<FileInfo>()));
    }

    [Fact]
    public async Task ScanAsync_ReturnsScannedFiles_WhenGivenValidFiles()
    {
        // Arrange
        var testFile = CreateTestMkvFile("test1.mkv");
        var scanner = CreateScanner();
        var files = new[] { new FileInfo(testFile) };

        // Act
        var result = await scanner.ScanAsync(files);

        // Assert
        Assert.Single(result);
        Assert.Equal(testFile, result.First().Path);
    }

    [Fact]
    public async Task ScanAsync_ScansMultipleFiles()
    {
        // Arrange
        var file1 = CreateTestMkvFile("test1.mkv");
        var file2 = CreateTestMkvFile("test2.mkv");
        var scanner = CreateScanner();
        var files = new[] { new FileInfo(file1), new FileInfo(file2) };

        // Act
        var result = await scanner.ScanAsync(files);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ScanAsync_ReportsDeterministicProgress()
    {
        // Arrange
        var file1 = CreateTestMkvFile("test1.mkv");
        var file2 = CreateTestMkvFile("test2.mkv");
        var scanner = CreateScanner();
        var files = new[] { new FileInfo(file1), new FileInfo(file2) };
        var reports = new List<(int current, int total)>();
        var progress = new Progress<(int current, int total)>(value => reports.Add(value));

        // Act
        await scanner.ScanAsync(files, progress);

        // Assert
        Assert.NotEmpty(reports);
        Assert.Equal((2, 2), reports[^1]);
    }

    [Fact]
    public async Task ScanAsync_UpdatesInternalScannedFilesList()
    {
        // Arrange
        var testFile = CreateTestMkvFile("test1.mkv");
        var scanner = CreateScanner();
        var files = new[] { new FileInfo(testFile) };

        // Act
        await scanner.ScanAsync(files);
        var scannedFiles = scanner.GetScannedFiles();

        // Assert
        Assert.Single(scannedFiles);
    }

    [Fact]
    public async Task ScanAsync_ClearsPreviousScannedFiles()
    {
        // Arrange
        var file1 = CreateTestMkvFile("test1.mkv");
        var file2 = CreateTestMkvFile("test2.mkv");
        var scanner = CreateScanner();

        await scanner.ScanAsync(new[] { new FileInfo(file1) });
        Assert.Single(scanner.GetScannedFiles());

        // Act
        await scanner.ScanAsync(new[] { new FileInfo(file2) });

        // Assert
        Assert.Single(scanner.GetScannedFiles());
        Assert.Equal(file2, scanner.GetScannedFiles().First().Path);
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_ThrowsDirectoryNotFoundException_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var options = CreateScanOptions("C:\\NonExistent", false, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => scanner.ScanWithMediaInfoAsync());
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_ScansFilesInDirectory_NonRecursive()
    {
        // Arrange
        CreateTestMkvFile("file1.mkv");
        CreateTestMkvFile("file2.mkv");
        var subDir = Path.Combine(_testDirectory, "SubDir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file3.mkv"), "dummy");

        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_ScansFilesInDirectory_Recursive()
    {
        // Arrange
        CreateTestMkvFile("file1.mkv");
        var subDir = Path.Combine(_testDirectory, "SubDir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "file2.mkv"), "dummy");

        var options = CreateScanOptions(_testDirectory, true, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_FiltersFilesByExtension()
    {
        // Arrange
        CreateTestMkvFile("video.mkv");
        File.WriteAllText(Path.Combine(_testDirectory, "video.mp4"), "dummy");
        File.WriteAllText(Path.Combine(_testDirectory, "document.txt"), "dummy");

        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Single(result);
        Assert.EndsWith(".mkv", result.First().Path);
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_SupportsMultipleExtensions()
    {
        // Arrange
        CreateTestMkvFile("video.mkv");
        File.WriteAllText(Path.Combine(_testDirectory, "video.mp4"), "dummy");
        File.WriteAllText(Path.Combine(_testDirectory, "document.txt"), "dummy");

        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv", ".mp4" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_ExcludesHiddenFiles_WhenExcludeHiddenIsTrue()
    {
        // Arrange
        var normalFile = CreateTestMkvFile("normal.mkv");
        var hiddenFile = CreateTestMkvFile("hidden.mkv");
        File.SetAttributes(hiddenFile, FileAttributes.Hidden);

        var options = CreateScanOptions(_testDirectory, false, true, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(normalFile, result.First().Path);
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_IncludesHiddenFiles_WhenExcludeHiddenIsFalse()
    {
        // Arrange
        CreateTestMkvFile("normal.mkv");
        var hiddenFile = CreateTestMkvFile("hidden.mkv");
        File.SetAttributes(hiddenFile, FileAttributes.Hidden);

        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_ReturnsEmptyList_WhenNoMatchingFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "document.txt"), "dummy");

        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScanWithMediaInfoAsync_IsCaseInsensitiveForExtensions()
    {
        // Arrange
        CreateTestMkvFile("video.MKV");
        CreateTestMkvFile("video2.mkv");

        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv" });
        var scanner = new FileScanner(options, _logger);

        // Act
        var result = await scanner.ScanWithMediaInfoAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetScannedFiles_ReturnsEmptyList_BeforeAnyScan()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var result = scanner.GetScannedFiles();

        // Assert
        Assert.Empty(result);
    }

    private FileScanner CreateScanner()
    {
        var options = CreateScanOptions(_testDirectory, false, false, new[] { ".mkv" });
        return new FileScanner(options, _logger);
    }

    private IOptionsMonitor<ScanOptions> CreateScanOptions(string directoryPath, bool recursive, bool excludeHidden, string[] allowedExtensions)
    {
        var scanOptions = new ScanOptions
        {
            DirectoryPath = directoryPath,
            Recursive = recursive,
            ExcludeHidden = excludeHidden,
            AllowedExtensions = allowedExtensions
        };
        var optionsMonitor = Substitute.For<IOptionsMonitor<ScanOptions>>();
        optionsMonitor.CurrentValue.Returns(scanOptions);
        return optionsMonitor;
    }

    private string CreateTestMkvFile(string filename)
    {
        var filePath = Path.Combine(_testDirectory, filename);
        File.WriteAllText(filePath, "dummy mkv content");
        return filePath;
    }

    private void CleanupTestDirectory()
    {
        if (Directory.Exists(_testDirectory))
        {
            foreach (var file in Directory.GetFiles(_testDirectory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (var dir in Directory.GetDirectories(_testDirectory))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
