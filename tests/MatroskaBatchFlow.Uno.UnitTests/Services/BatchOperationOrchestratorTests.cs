using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.FileProcessing;
using MatroskaBatchFlow.Core.Services.FileProcessing.Track;
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
            Assert.Same(_addFilesStage, capturedStages[3]);
            Assert.Same(_initTrackConfigStage, capturedStages[4]);
            Assert.Same(_validateStage, capturedStages[5]);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ImportFilesAsync_AddsFilesBeforeApplyingTrackConfiguration()
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

            var addFilesIndex = capturedStages.IndexOf(_addFilesStage);
            var initializeIndex = capturedStages.IndexOf(_initTrackConfigStage);

            Assert.True(addFilesIndex >= 0, "AddFilesToBatchStage should be part of the import pipeline.");
            Assert.True(initializeIndex >= 0, "InitializeTrackConfigStage should be part of the import pipeline.");
            Assert.True(
                addFilesIndex < initializeIndex,
                "Imported files must be added to the batch before track initialization runs so aggregate defaults can see the full batch.");
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task ImportFilesAsync_WhenImportedFilesDisagreeOnDefaultFlag_UsesAllImportedFilesForDerivedDefault()
    {
        var platformService = Substitute.For<IPlatformService>();
        platformService.IsWindows().Returns(true);

        var pathComparer = new ScannedFileInfoPathComparer(platformService);
        var batchConfig = new BatchConfiguration(pathComparer, Substitute.For<ILogger<BatchConfiguration>>());
        var fileListAdapter = new FileListAdapter(batchConfig, Substitute.For<ILogger<FileListAdapter>>());

        var fileScanner = Substitute.For<IFileScanner>();
        var languageProvider = Substitute.For<ILanguageProvider>();
        languageProvider.Resolve(Arg.Any<string?>()).Returns(MatroskaLanguageOption.Undetermined);

        var validationStateService = Substitute.For<IValidationStateService>();
        validationStateService.RevalidateAsync().Returns(Task.CompletedTask);

        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            var firstScannedFile = CreateScannedFile(firstPath, audioDefault: true);
            var secondScannedFile = CreateScannedFile(secondPath, audioDefault: false);

            var scannedFilesByPath = new Dictionary<string, ScannedFileInfo>(StringComparer.OrdinalIgnoreCase)
            {
                [firstPath] = firstScannedFile,
                [secondPath] = secondScannedFile,
            };

            fileScanner.ScanAsync(Arg.Any<FileInfo[]>(), Arg.Any<IProgress<(int current, int total)>>())
                .Returns(callInfo =>
                {
                    var files = callInfo.Arg<FileInfo[]>();
                    return files.Select(file => scannedFilesByPath[file.FullName]).ToList();
                });

            var orchestrator = new BatchOperationOrchestrator(
                new ExecutingPipelineRunner(),
                new FilterDuplicateFilesStage(batchConfig, platformService, Substitute.For<ILogger<FilterDuplicateFilesStage>>()),
                new ScanFilesStage(fileScanner, Substitute.For<ILogger<ScanFilesStage>>()),
                new RefreshStaleMetadataStage(fileScanner, batchConfig, pathComparer, Substitute.For<ILogger<RefreshStaleMetadataStage>>()),
                new InitializeTrackConfigStage(
                    new BatchTrackConfigurationInitializer(batchConfig, new TrackIntentFactory(languageProvider)),
                    new FileProcessingEngine([new TrackDefaultRule()]),
                    batchConfig),
                new AddFilesToBatchStage(fileListAdapter),
                new RemoveFilesFromBatchStage(fileListAdapter),
                new ValidateStage(validationStateService),
                Substitute.For<ILogger<BatchOperationOrchestrator>>());

            await orchestrator.ImportFilesAsync([new FileInfo(firstPath), new FileInfo(secondPath)]);

            Assert.Equal(2, batchConfig.FileList.Count);

            var audioTrack = Assert.Single(batchConfig.AudioTracks);

            Assert.True(firstScannedFile.GetTracks(TrackType.Audio)[0].Default);
            Assert.False(secondScannedFile.GetTracks(TrackType.Audio)[0].Default);
            Assert.False(audioTrack.Default);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
            fileListAdapter.Dispose();
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

    private sealed class ExecutingPipelineRunner : IPipelineRunner
    {
        public async Task RunAsync(IReadOnlyList<IPipelineStage> stages, PipelineContext context, CancellationToken ct = default)
        {
            foreach (var stage in stages)
            {
                ct.ThrowIfCancellationRequested();

                if (context.IsAborted)
                {
                    break;
                }

                await stage.ExecuteAsync(context, progress: null, ct);
            }
        }
    }

    private static ScannedFileInfo CreateScannedFile(string path, bool audioDefault = false)
    {
        var builder = new MediaInfoResultBuilder()
            .AddTrackOfType(TrackType.Video)
            .AddTrack(new TrackInfoBuilder()
                .WithType(TrackType.Audio)
                .WithStreamKindID(0)
                .WithDefault(audioDefault)
                .Build());

        return new ScannedFileInfo(builder.Build(), path);
    }
}
