using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

public sealed class BatchHistoryServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("mbf-history-").FullName;
    private readonly string _filePath;

    public BatchHistoryServiceTests()
    {
        _filePath = Path.Combine(_directory, "BatchHistory.json");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task SavedRuns_AreLoadedAfterRestart()
    {
        var run = new BatchRunRecord(Guid.NewGuid(), DateTimeOffset.Now, [
            new FileRunRecord(@"C:\media\movie.mkv", ProcessingStatus.Failed, DateTimeOffset.Now, DateTimeOffset.Now,
                TimeSpan.FromSeconds(2), "mkvpropedit movie.mkv", ["Error: file is locked"], Array.Empty<string>())
        ]);
        await CreateService().AddAsync(run);

        var restarted = CreateService();
        await restarted.LoadAsync();

        var loaded = Assert.Single(restarted.Runs);
        Assert.Equal(run.Id, loaded.Id);
        Assert.Equal(run.RecordedAt, loaded.RecordedAt);
        var file = Assert.Single(loaded.Files);
        Assert.Equal(run.Files[0] with { Errors = file.Errors, Warnings = file.Warnings }, file);
        Assert.Equal(["Error: file is locked"], file.Errors);
        Assert.Empty(file.Warnings);
    }

    [Fact]
    public async Task AddAsync_KeepsOnlyTheNewestRuns()
    {
        var service = CreateService();
        var runs = Enumerable.Range(0, BatchHistoryService.MaxRuns + 2).Select(i => Run(i)).ToArray();

        foreach (var run in runs)
        {
            await service.AddAsync(run);
        }

        var restarted = CreateService();
        await restarted.LoadAsync();

        var expected = runs.Reverse().Take(BatchHistoryService.MaxRuns).Select(run => run.Id).ToArray();
        Assert.Equal(expected, service.Runs.Select(run => run.Id));
        Assert.Equal(expected, restarted.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task LoadAsync_KeepsRunsAddedBeforeLoadingOnTop()
    {
        var saved = Run(0);
        await CreateService().AddAsync(saved);

        var service = CreateService();
        var added = Run(1);
        await service.AddAsync(added);
        await service.LoadAsync();

        Assert.Equal([added.Id, saved.Id], service.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task ClearAsync_RemovesTheFile()
    {
        var service = CreateService();
        await service.AddAsync(Run(0));
        Assert.True(File.Exists(_filePath));

        await service.ClearAsync();

        Assert.Empty(service.Runs);
        Assert.False(File.Exists(_filePath));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Runs":[]}""")]
    public async Task LoadAsync_StartsEmpty_WhenFileCannotBeRead(string content)
    {
        await File.WriteAllTextAsync(_filePath, content, TestContext.Current.CancellationToken);
        var service = CreateService();

        await service.LoadAsync();

        Assert.Empty(service.Runs);
    }

    private BatchHistoryService CreateService() => new(_filePath, NullLogger<BatchHistoryService>.Instance);

    private static BatchRunRecord Run(int minutes) => new(
        Guid.NewGuid(),
        new DateTimeOffset(2026, 11, 23, 19, minutes, 0, TimeSpan.Zero),
        [new FileRunRecord($"{minutes}.mkv", ProcessingStatus.Succeeded, null, null, null, null, Array.Empty<string>(), Array.Empty<string>())]);
}
