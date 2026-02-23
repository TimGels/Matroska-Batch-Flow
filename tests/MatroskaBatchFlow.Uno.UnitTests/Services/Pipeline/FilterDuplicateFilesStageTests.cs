using MatroskaBatchFlow.Core.Abstractions.Pipeline;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Services.Pipeline;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Services.Pipeline;

/// <summary>
/// Contains unit tests for the <see cref="FilterDuplicateFilesStage"/> class.
/// </summary>
public class FilterDuplicateFilesStageTests
{
    private readonly IBatchConfiguration _batchConfig;
    private readonly IPlatformService _platformService;
    private readonly FilterDuplicateFilesStage _stage;

    public FilterDuplicateFilesStageTests()
    {
        _batchConfig = Substitute.For<IBatchConfiguration>();
        _batchConfig.FileList.Returns([]);
        _platformService = Substitute.For<IPlatformService>();
        _platformService.IsWindows().Returns(true);

        _stage = new FilterDuplicateFilesStage(
            _batchConfig,
            _platformService,
            Substitute.For<ILogger<FilterDuplicateFilesStage>>());
    }

    [Fact]
    public async Task ExecuteAsync_AbortsContext_WhenAllFilesAreDuplicates()
    {
        var existingFile = Path.GetTempFileName();
        try
        {
            _batchConfig.FileList.Returns([new ScannedFileInfo(null!, existingFile)]);

            var context = new PipelineContext();
            context.Set(PipelineContextKeys.InputFiles, new[] { new FileInfo(existingFile) });

            await _stage.ExecuteAsync(context, null, CancellationToken.None);

            Assert.True(context.IsAborted);
            var remaining = context.Get<FileInfo[]>(PipelineContextKeys.InputFiles);
            Assert.Empty(remaining);
        }
        finally
        {
            File.Delete(existingFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotAbort_WhenSomeFilesAreUnique()
    {
        var existingFile = Path.GetTempFileName();
        var newFile = Path.GetTempFileName();
        try
        {
            _batchConfig.FileList.Returns([new ScannedFileInfo(null!, existingFile)]);

            var context = new PipelineContext();
            context.Set(PipelineContextKeys.InputFiles, new[] { new FileInfo(existingFile), new FileInfo(newFile) });

            await _stage.ExecuteAsync(context, null, CancellationToken.None);

            Assert.False(context.IsAborted);
            var remaining = context.Get<FileInfo[]>(PipelineContextKeys.InputFiles);
            Assert.Single(remaining);
        }
        finally
        {
            File.Delete(existingFile);
            File.Delete(newFile);
        }
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotAbort_WhenNoInputFiles()
    {
        var context = new PipelineContext();

        await _stage.ExecuteAsync(context, null, CancellationToken.None);

        Assert.False(context.IsAborted);
    }
}
