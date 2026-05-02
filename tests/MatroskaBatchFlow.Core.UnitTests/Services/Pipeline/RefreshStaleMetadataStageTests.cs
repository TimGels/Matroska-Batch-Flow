using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.Pipeline;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services.Pipeline;

public class RefreshStaleMetadataStageTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoFilesAreStale_DoesNotScan()
    {
        // Arrange
        var fileScanner = Substitute.For<IFileScanner>();
        var batchConfig = CreateBatchConfiguration();
        var pathComparer = CreatePathComparer();
        var logger = Substitute.For<ILogger<RefreshStaleMetadataStage>>();

        batchConfig.FileList.Add(CreateScannedFile("C:\\media\\episode-01.mkv", 1));

        var stage = new RefreshStaleMetadataStage(fileScanner, batchConfig, pathComparer, logger);

        // Act
        await stage.ExecuteAsync(new PipelineContext(), progress: null, CancellationToken.None);

        // Assert
        await fileScanner.DidNotReceiveWithAnyArgs().ScanAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFreshScanExists_ReplacesFileAndClearsStaleFlag()
    {
        // Arrange
        var fileScanner = Substitute.For<IFileScanner>();
        var batchConfig = CreateBatchConfiguration();
        var pathComparer = CreatePathComparer();
        var logger = Substitute.For<ILogger<RefreshStaleMetadataStage>>();

        var staleFile = CreateScannedFile("C:\\media\\episode-01.mkv", 1);
        var freshScan = CreateScannedFile("C:\\media\\episode-01.mkv", 2);

        batchConfig.FileList.Add(staleFile);
        batchConfig.MarkFileAsStale(staleFile.Id);

        fileScanner.ScanAsync(Arg.Any<FileInfo[]>(), Arg.Any<IProgress<(int current, int total)>>())
            .Returns(new[] { freshScan });

        var stage = new RefreshStaleMetadataStage(fileScanner, batchConfig, pathComparer, logger);

        // Act
        await stage.ExecuteAsync(new PipelineContext(), progress: null, CancellationToken.None);

        // Assert
        Assert.False(batchConfig.IsFileStale(staleFile.Id));
        Assert.Empty(batchConfig.GetStaleFiles());

        var onlyFile = Assert.Single(batchConfig.FileList);
        Assert.Equal(freshScan.Id, onlyFile.Id);
        Assert.Equal(2, onlyFile.AudioTrackCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFreshScanMissing_ClearsStaleFlagAndKeepsOriginalFile()
    {
        // Arrange
        var fileScanner = Substitute.For<IFileScanner>();
        var batchConfig = CreateBatchConfiguration();
        var pathComparer = CreatePathComparer();
        var logger = Substitute.For<ILogger<RefreshStaleMetadataStage>>();

        var staleFile = CreateScannedFile("C:\\media\\episode-01.mkv", 1);

        batchConfig.FileList.Add(staleFile);
        batchConfig.MarkFileAsStale(staleFile.Id);

        fileScanner.ScanAsync(Arg.Any<FileInfo[]>(), Arg.Any<IProgress<(int current, int total)>>())
            .Returns(Array.Empty<ScannedFileInfo>());

        var stage = new RefreshStaleMetadataStage(fileScanner, batchConfig, pathComparer, logger);

        // Act
        await stage.ExecuteAsync(new PipelineContext(), progress: null, CancellationToken.None);

        // Assert
        Assert.False(batchConfig.IsFileStale(staleFile.Id));

        var onlyFile = Assert.Single(batchConfig.FileList);
        Assert.Equal(staleFile.Id, onlyFile.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBatchRescanThrows_ClearsAllStaleFlagsWithoutThrowing()
    {
        // Arrange
        var fileScanner = Substitute.For<IFileScanner>();
        var batchConfig = CreateBatchConfiguration();
        var pathComparer = CreatePathComparer();
        var logger = Substitute.For<ILogger<RefreshStaleMetadataStage>>();

        var staleFile1 = CreateScannedFile("C:\\media\\episode-01.mkv", 1);
        var staleFile2 = CreateScannedFile("C:\\media\\episode-02.mkv", 1);

        batchConfig.FileList.Add(staleFile1);
        batchConfig.FileList.Add(staleFile2);
        batchConfig.MarkFileAsStale(staleFile1.Id);
        batchConfig.MarkFileAsStale(staleFile2.Id);

        fileScanner.ScanAsync(Arg.Any<FileInfo[]>(), Arg.Any<IProgress<(int current, int total)>>())
            .Returns(_ => Task.FromException<IEnumerable<ScannedFileInfo>>(new IOException("Scan failed")));

        var stage = new RefreshStaleMetadataStage(fileScanner, batchConfig, pathComparer, logger);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            stage.ExecuteAsync(new PipelineContext(), progress: null, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        Assert.False(batchConfig.IsFileStale(staleFile1.Id));
        Assert.False(batchConfig.IsFileStale(staleFile2.Id));
        Assert.Equal(2, batchConfig.FileList.Count);
    }

    private static BatchConfiguration CreateBatchConfiguration()
    {
        var pathComparer = CreatePathComparer();
        var logger = Substitute.For<ILogger<BatchConfiguration>>();
        return new BatchConfiguration(pathComparer, logger);
    }

    private static IScannedFileInfoPathComparer CreatePathComparer()
    {
        var platformService = Substitute.For<IPlatformService>();
        platformService.IsWindows().Returns(true);
        return new ScannedFileInfoPathComparer(platformService);
    }

    private static ScannedFileInfo CreateScannedFile(string path, int audioTrackCount)
    {
        var builder = new MediaInfoResultBuilder().WithCreatingLibrary();

        for (var i = 0; i < audioTrackCount; i++)
        {
            builder.AddTrackOfType(TrackType.Audio, i);
        }

        return new ScannedFileInfo(builder.Build(), path);
    }
}
