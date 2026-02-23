using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.Services.Pipeline;
using MatroskaBatchFlow.Core.UnitTests.Builders;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Services;
using MatroskaBatchFlow.Uno.Services.Pipeline;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

/// <summary>
/// Contains unit tests for the <see cref="BatchOperationOrchestrator"/> class.
/// </summary>
public class BatchOperationOrchestratorTests
{
    private readonly IPipelineRunner _pipelineRunner;
    private readonly FilterDuplicateFilesStage _filterDuplicatesStage;
    private readonly ScanFilesStage _scanFilesStage;
    private readonly RefreshStaleMetadataStage _refreshStaleMetadataStage;
    private readonly InitializeTrackConfigStage _initTrackConfigStage;
    private readonly AddFilesToBatchStage _addFilesStage;
    private readonly RemoveFilesFromBatchStage _removeFilesStage;
    private readonly ValidateStage _validateStage;
    private readonly ILogger<BatchOperationOrchestrator> _logger;

    public BatchOperationOrchestratorTests()
    {
        _pipelineRunner = Substitute.For<IPipelineRunner>();
        _logger = Substitute.For<ILogger<BatchOperationOrchestrator>>();

        // Create real stage instances with mocked dependencies.
        // The runner is mocked so stage methods are never called — only identity matters.
        _filterDuplicatesStage = new FilterDuplicateFilesStage(
            Substitute.For<IBatchConfiguration>(),
            Substitute.For<IPlatformService>(),
            Substitute.For<ILogger<FilterDuplicateFilesStage>>());

        _scanFilesStage = new ScanFilesStage(
            Substitute.For<IFileScanner>(),
            Substitute.For<ILogger<ScanFilesStage>>());

        _refreshStaleMetadataStage = new RefreshStaleMetadataStage(
            Substitute.For<IFileScanner>(),
            Substitute.For<IBatchConfiguration>(),
            Substitute.For<IScannedFileInfoPathComparer>(),
            Substitute.For<ILogger<RefreshStaleMetadataStage>>());

        _initTrackConfigStage = new InitializeTrackConfigStage(
            Substitute.For<IBatchTrackConfigurationInitializer>(),
            Substitute.For<IFileProcessingEngine>(),
            Substitute.For<IBatchConfiguration>());

        _addFilesStage = new AddFilesToBatchStage(Substitute.For<IFileListAdapter>());
        _removeFilesStage = new RemoveFilesFromBatchStage(Substitute.For<IFileListAdapter>());
        _validateStage = new ValidateStage(Substitute.For<IValidationStateService>());
    }

    [Fact]
    public async Task ImportFilesAsync_WithNullOrEmpty_DoesNotCallRunner()
    {
        var orchestrator = CreateOrchestrator();

        await orchestrator.ImportFilesAsync(null!);
        await orchestrator.ImportFilesAsync([]);

        await _pipelineRunner.DidNotReceive().RunAsync(
            Arg.Any<IReadOnlyList<IPipelineStage>>(),
            Arg.Any<PipelineContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportFilesAsync_ComposesCorrectStageSequence()
    {
        IReadOnlyList<IPipelineStage>? capturedStages = null;
        await _pipelineRunner.RunAsync(
            Arg.Do<IReadOnlyList<IPipelineStage>>(s => capturedStages = s),
            Arg.Any<PipelineContext>(),
            Arg.Any<CancellationToken>());

        var testFile = Path.GetTempFileName();
        try
        {
            var orchestrator = CreateOrchestrator();
            await orchestrator.ImportFilesAsync([new FileInfo(testFile)]);

            Assert.NotNull(capturedStages);
            Assert.Equal(6, capturedStages.Count);
            Assert.Same(_filterDuplicatesStage, capturedStages[0]);
            Assert.Same(_scanFilesStage, capturedStages[1]);
            Assert.Same(_refreshStaleMetadataStage, capturedStages[2]);
            Assert.Same(_initTrackConfigStage, capturedStages[3]);
            Assert.Same(_addFilesStage, capturedStages[4]);
            Assert.Same(_validateStage, capturedStages[5]);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ImportFilesAsync_SetsInputFilesInContext()
    {
        PipelineContext? capturedContext = null;
        await _pipelineRunner.RunAsync(
            Arg.Any<IReadOnlyList<IPipelineStage>>(),
            Arg.Do<PipelineContext>(c => capturedContext = c),
            Arg.Any<CancellationToken>());

        var testFile = Path.GetTempFileName();
        try
        {
            var fileInfo = new FileInfo(testFile);
            var orchestrator = CreateOrchestrator();

            await orchestrator.ImportFilesAsync([fileInfo]);

            Assert.NotNull(capturedContext);
            var inputFiles = capturedContext.Get<FileInfo[]>(PipelineContextKeys.InputFiles);
            Assert.Single(inputFiles);
            Assert.Equal(fileInfo.FullName, inputFiles[0].FullName);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task RemoveFilesAsync_WithEmptyList_DoesNotCallRunner()
    {
        var orchestrator = CreateOrchestrator();

        await orchestrator.RemoveFilesAsync([]);

        await _pipelineRunner.DidNotReceive().RunAsync(
            Arg.Any<IReadOnlyList<IPipelineStage>>(),
            Arg.Any<PipelineContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFilesAsync_ComposesCorrectStageSequence()
    {
        IReadOnlyList<IPipelineStage>? capturedStages = null;
        await _pipelineRunner.RunAsync(
            Arg.Do<IReadOnlyList<IPipelineStage>>(s => capturedStages = s),
            Arg.Any<PipelineContext>(),
            Arg.Any<CancellationToken>());

        var file = CreateScannedFile("file1.mkv");
        var orchestrator = CreateOrchestrator();

        await orchestrator.RemoveFilesAsync([file]);

        Assert.NotNull(capturedStages);
        Assert.Equal(2, capturedStages.Count);
        Assert.Same(_removeFilesStage, capturedStages[0]);
        Assert.Same(_validateStage, capturedStages[1]);
    }

    [Fact]
    public async Task RemoveFilesAsync_SetsFilesToRemoveInContext()
    {
        PipelineContext? capturedContext = null;
        await _pipelineRunner.RunAsync(
            Arg.Any<IReadOnlyList<IPipelineStage>>(),
            Arg.Do<PipelineContext>(c => capturedContext = c),
            Arg.Any<CancellationToken>());

        var file = CreateScannedFile("file1.mkv");
        var orchestrator = CreateOrchestrator();

        await orchestrator.RemoveFilesAsync([file]);

        Assert.NotNull(capturedContext);
        var filesToRemove = capturedContext.Get<List<ScannedFileInfo>>(PipelineContextKeys.FilesToRemove);
        Assert.Single(filesToRemove);
        Assert.Same(file, filesToRemove[0]);
    }

    [Fact]
    public async Task RevalidateAsync_ComposesCorrectStageSequence()
    {
        IReadOnlyList<IPipelineStage>? capturedStages = null;
        await _pipelineRunner.RunAsync(
            Arg.Do<IReadOnlyList<IPipelineStage>>(s => capturedStages = s),
            Arg.Any<PipelineContext>(),
            Arg.Any<CancellationToken>());

        var orchestrator = CreateOrchestrator();

        await orchestrator.RevalidateAsync();

        Assert.NotNull(capturedStages);
        Assert.Single(capturedStages);
        Assert.Same(_validateStage, capturedStages[0]);
    }

    private BatchOperationOrchestrator CreateOrchestrator()
    {
        return new BatchOperationOrchestrator(
            _pipelineRunner,
            _filterDuplicatesStage,
            _scanFilesStage,
            _refreshStaleMetadataStage,
            _initTrackConfigStage,
            _addFilesStage,
            _removeFilesStage,
            _validateStage,
            _logger);
    }

    private static ScannedFileInfo CreateScannedFile(string path)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrackOfType(TrackType.Audio);
        return new ScannedFileInfo(builder.Build(), path);
    }
}
