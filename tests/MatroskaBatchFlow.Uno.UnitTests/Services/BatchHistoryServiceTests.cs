using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

public sealed class BatchHistoryServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("mbf-history-").FullName;
    private readonly string _filePath;
    private readonly UserSettings _settings = new();
    private readonly IWritableSettings<UserSettings> _userSettings = Substitute.For<IWritableSettings<UserSettings>>();

    public BatchHistoryServiceTests()
    {
        _filePath = Path.Combine(_directory, "BatchHistory.json");
        _userSettings.Value.Returns(_settings);
        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.Arg<Action<UserSettings>>()(_settings);
                return Task.CompletedTask;
            });
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
        _settings.BatchHistory.MaxRuns = 3;
        var service = CreateService();
        var runs = Enumerable.Range(0, 5).Select(i => Run(i)).ToArray();

        foreach (var run in runs)
        {
            await service.AddAsync(run);
        }

        var restarted = CreateService();
        await restarted.LoadAsync();

        var expected = runs.Reverse().Take(3).Select(run => run.Id).ToArray();
        Assert.Equal(expected, service.Runs.Select(run => run.Id));
        Assert.Equal(expected, restarted.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task SetMaxRunsAsync_SavesTheLimitAndRemovesTheOldestRuns()
    {
        var service = CreateService();
        var runs = Enumerable.Range(0, 4).Select(i => Run(i)).ToArray();
        foreach (var run in runs)
        {
            await service.AddAsync(run);
        }

        Assert.Equal(0, await service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        var restarted = CreateService();
        await restarted.LoadAsync();

        var expected = new[] { runs[3].Id, runs[2].Id };
        Assert.Equal(2, _settings.BatchHistory.MaxRuns);
        Assert.Equal(expected, service.Runs.Select(run => run.Id));
        Assert.Equal(expected, restarted.Runs.Select(run => run.Id));
    }

    [Theory]
    [InlineData(0, UserSettings.BatchHistorySettings.MinMaxRuns)]
    [InlineData(1000, UserSettings.BatchHistorySettings.MaxMaxRuns)]
    public async Task SetMaxRunsAsync_ClampsTheLimit(int requested, int expected)
    {
        var service = CreateService();

        await service.SetMaxRunsAsync(requested);

        Assert.Equal(expected, _settings.BatchHistory.MaxRuns);
        Assert.Equal(expected, service.MaxRuns);
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
    public async Task ClearAsync_RemovesTheHistoryAndTemporaryFiles()
    {
        var service = CreateService();
        await service.AddAsync(Run(0));
        File.Copy(_filePath, _filePath + ".tmp");

        Assert.True(await service.ClearAsync());

        Assert.Empty(service.Runs);
        Assert.False(File.Exists(_filePath));
        Assert.False(File.Exists(_filePath + ".tmp"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Runs":[]}""")]
    public async Task AddAsync_PreservesAnUnreadableFileUntilExplicitlyCleared(string content)
    {
        await File.WriteAllTextAsync(_filePath, content, TestContext.Current.CancellationToken);
        var service = CreateService();

        await service.LoadAsync();

        Assert.Empty(service.Runs);
        var run = Run(0);
        await service.AddAsync(run);
        Assert.Equal(run.Id, Assert.Single(service.Runs).Id);
        Assert.Equal(content, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));

        Assert.True(await service.ClearAsync());
        Assert.False(File.Exists(_filePath));
        await service.AddAsync(run);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(run.Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task AddAsync_MergesConcurrentWrites_WithoutResurrectingClearedRuns()
    {
        var first = CreateService();
        var second = CreateService();
        await first.AddAsync(Run(0));
        await second.LoadAsync();
        Assert.True(await first.ClearAsync());

        var firstRun = Run(1);
        var secondRun = Run(2);
        await Task.WhenAll(first.AddAsync(firstRun), second.AddAsync(secondRun));

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal([secondRun.Id, firstRun.Id], restarted.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task SetMaxRunsAsync_RequiresFreshConfirmation_WhenAnotherInstanceAddsHistory()
    {
        var service = CreateService();
        await service.AddAsync(Run(0));
        await service.AddAsync(Run(1));
        Assert.Equal(1, await service.SetMaxRunsAsync(1));

        var second = CreateService();
        await second.AddAsync(Run(2));

        Assert.Equal(2, await service.SetMaxRunsAsync(1, confirmedRemovals: 1));
        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, service.MaxRuns);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(3, restarted.Runs.Count);

        Assert.Equal(0, await service.SetMaxRunsAsync(1, confirmedRemovals: 2));
        Assert.Single(service.Runs);
    }

    private BatchHistoryService CreateService() => new(_filePath, _userSettings, NullLogger<BatchHistoryService>.Instance);

    private static BatchRunRecord Run(int minutes) => new(
        Guid.NewGuid(),
        new DateTimeOffset(2026, 11, 23, 19, minutes, 0, TimeSpan.Zero),
        [new FileRunRecord($"{minutes}.mkv", ProcessingStatus.Succeeded, null, null, null, null, Array.Empty<string>(), Array.Empty<string>())]);
}
