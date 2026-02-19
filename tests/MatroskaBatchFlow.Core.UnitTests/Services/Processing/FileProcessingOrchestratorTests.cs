using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.Processing;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services.Processing;

/// <summary>
/// Contains unit tests for the FileProcessingOrchestrator class.
/// </summary>
public class FileProcessingOrchestratorTests
{
    private readonly IBatchConfiguration _batchConfig = Substitute.For<IBatchConfiguration>();
    private readonly IBatchReportStore _batchReportStore = Substitute.For<IBatchReportStore>();
    private readonly IMkvToolExecutor _mkvToolExecutor = Substitute.For<IMkvToolExecutor>();
    private readonly IMkvPropeditArgumentsGenerator _argsGenerator = Substitute.For<IMkvPropeditArgumentsGenerator>();
    private readonly ILogger<FileProcessingOrchestrator> _logger = Substitute.For<ILogger<FileProcessingOrchestrator>>();

    [Fact]
    public async Task ProcessFileAsync_ReturnsReport_WhenAlreadyRunning()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport(ProcessingStatus.Running);

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.Running, result.Status);
        Assert.Equal(report, result);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsCanceledStatus_WhenCancellationRequestedBeforeStart()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await orchestrator.ProcessFileAsync(report, cts.Token);

        // Assert
        Assert.Equal(ProcessingStatus.Canceled, result.Status);
        Assert.Contains("canceled before start", result.Notes!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsRunningStatus_WhenProcessingStarts()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns(string.Empty);

        // Act
        await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.NotNull(report.StartedAt);
    }

    [Fact]
    public async Task ProcessFileAsync_SkipsFile_WhenNoModificationsRequested()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns(string.Empty);

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.Skipped, result.Status);
        Assert.Contains("No modifications", result.Notes!);
    }

    [Fact]
    public async Task ProcessFileAsync_SkipsFile_WhenArgumentsAreWhitespace()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("   ");

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.Skipped, result.Status);
    }

    [Fact]
    public async Task ProcessFileAsync_ExecutesMkvToolExecutor_WhenArgumentsProvided()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        var args = "test arguments";
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns(args);
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResult());

        // Act
        await orchestrator.ProcessFileAsync(report);

        // Assert
        await _mkvToolExecutor.Received(1).ExecuteAsync(args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessFileAsync_SetsSucceededStatus_WhenExecutionSucceeds()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResult());

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsSucceededWithWarningsStatus_WhenWarningsExist()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateWarningResult());

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.SucceededWithWarnings, result.Status);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsFailedStatus_WhenExecutionFails()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateFailureResult());

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.Failed, result.Status);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsCanceledStatus_WhenCancellationRequestedDuringExecution()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        var cts = new CancellationTokenSource();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return CreateSuccessResult();
            });

        // Act
        var result = await orchestrator.ProcessFileAsync(report, cts.Token);

        // Assert
        Assert.Equal(ProcessingStatus.Canceled, result.Status);
        Assert.Contains("canceled", result.Notes!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessFileAsync_CapturesException_WhenExecutionThrows()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<MkvPropeditResult>(_ => throw new InvalidOperationException("Test error"));

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Equal(ProcessingStatus.Failed, result.Status);
        Assert.Contains("Test error", result.Errors[0]);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsExecutedCommand_WhenExecuted()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        var executablePath = "mkvpropedit";
        var arguments = "test.mkv --args";
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MkvPropeditResult
            {
                Status = MkvPropeditStatus.Success,
                Warnings = Array.Empty<string>(),
                ResolvedExecutablePath = executablePath,
                ExecutableArguments = arguments
            });

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.Contains(executablePath, result.ExecutedCommand);
        Assert.Contains(arguments, result.ExecutedCommand);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsDuration_AfterExecution()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(100);
                return CreateSuccessResult();
            });

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.NotNull(result.Duration);
        Assert.True(result.Duration.Value.TotalMilliseconds >= 100);
    }

    [Fact]
    public async Task ProcessFileAsync_SetsStartedAtAndFinishedAt()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var report = CreateFileReport();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResult());

        // Act
        var result = await orchestrator.ProcessFileAsync(report);

        // Assert
        Assert.NotNull(result.StartedAt);
        Assert.NotNull(result.FinishedAt);
        Assert.True(result.FinishedAt >= result.StartedAt);
    }

    [Fact]
    public async Task ProcessAllAsync_ProcessesAllFiles()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var files = new[]
        {
            CreateScannedFile("file1.mkv"),
            CreateScannedFile("file2.mkv"),
            CreateScannedFile("file3.mkv")
        };
        SetupActiveBatchWithReports(files);
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResult());

        // Act
        var results = await orchestrator.ProcessAllAsync(files);

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ProcessAllAsync_EnrollsFilesInActiveBatch()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var files = new[] { CreateScannedFile("file1.mkv") };
        var activeBatch = new BatchExecutionReport();
        _batchReportStore.ActiveBatch.Returns(activeBatch);
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResult());

        // Act
        await orchestrator.ProcessAllAsync(files);

        // Assert
        Assert.Single(activeBatch.FileReports);
    }

    [Fact]
    public async Task ProcessAllAsync_RespectsIsCancellation()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var files = new[]
        {
            CreateScannedFile("file1.mkv"),
            CreateScannedFile("file2.mkv")
        };
        SetupActiveBatchWithReports(files);
        var cts = new CancellationTokenSource();
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns("args");

        var callCount = 0;
        _mkvToolExecutor.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1)
                {
                    cts.Cancel();
                }
                return CreateSuccessResult();
            });

        // Act
        var results = await orchestrator.ProcessAllAsync(files, cts.Token);

        // Assert
        Assert.Equal(ProcessingStatus.Succeeded, results[0].Status);
        Assert.Equal(ProcessingStatus.Canceled, results[1].Status);
    }

    [Fact]
    public async Task ProcessAllAsync_ReturnsEmptyList_WhenNoFilesProvided()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var activeBatch = new BatchExecutionReport();
        _batchReportStore.ActiveBatch.Returns(activeBatch);

        // Act
        var results = await orchestrator.ProcessAllAsync(Array.Empty<ScannedFileInfo>());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ProcessAllAsync_ProcessesNewFilesEvenIfSamePathExists()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var file1 = CreateScannedFile("file1.mkv");
        var file2 = CreateScannedFile("file1.mkv");
        var activeBatch = new BatchExecutionReport();
        
        var existingReport = new FileProcessingReport
        {
            SourceFile = file1,
            Path = file1.Path,
            Status = ProcessingStatus.Succeeded
        };
        activeBatch.TryAddFileReport(existingReport);
        _batchReportStore.ActiveBatch.Returns(activeBatch);
        _argsGenerator.BuildFileArgumentString(Arg.Any<ScannedFileInfo>(), Arg.Any<IBatchConfiguration>())
            .Returns(string.Empty);

        // Act - Pass file2 which has a different report ID
        var results = await orchestrator.ProcessAllAsync(new[] { file2 });

        // Assert - Should create a new report since it's a different FileProcessingReport instance
        Assert.Single(results);
    }

    private FileProcessingOrchestrator CreateOrchestrator()
    {
        return new FileProcessingOrchestrator(
            _batchConfig,
            _batchReportStore,
            _mkvToolExecutor,
            _argsGenerator,
            _logger);
    }

    private FileProcessingReport CreateFileReport(ProcessingStatus status = ProcessingStatus.Pending)
    {
        var scannedFile = CreateScannedFile("test.mkv");
        return new FileProcessingReport
        {
            SourceFile = scannedFile,
            Path = scannedFile.Path,
            Status = status
        };
    }

    private ScannedFileInfo CreateScannedFile(string path)
    {
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.General)
            .Build();
        return new ScannedFileInfo(mediaInfoResult, path);
    }

    private MkvPropeditResult CreateSuccessResult(string commandLine = "mkvpropedit test.mkv")
    {
        return new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = Array.Empty<string>(),
            ResolvedExecutablePath = "mkvpropedit",
            ExecutableArguments = "test.mkv"
        };
    }

    private MkvPropeditResult CreateWarningResult()
    {
        return new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Warning,
            Warnings = new[] { "Warning message" }
        };
    }

    private MkvPropeditResult CreateFailureResult()
    {
        return new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Error,
            Warnings = Array.Empty<string>(),
            StandardError = "Error occurred"
        };
    }

    private void SetupActiveBatchWithReports(ScannedFileInfo[] files)
    {
        var activeBatch = new BatchExecutionReport();
        _batchReportStore.ActiveBatch.Returns(activeBatch);
    }
}
