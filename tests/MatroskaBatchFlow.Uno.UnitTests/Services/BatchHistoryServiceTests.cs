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
        _userSettings.ReadFromDiskAsync().Returns(Task.FromResult(_settings));
        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.Arg<Action<UserSettings>>()(_settings);
                return Task.CompletedTask;
            });
        _userSettings.DiscardPendingChanges(Arg.Any<Action<UserSettings>>())
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
    public async Task ClearAsync_RemovesTheSavedRunsAndTemporaryFiles()
    {
        var service = CreateService();
        await service.AddAsync(Run(0));
        File.Copy(_filePath, _filePath + ".tmp");

        Assert.True(await service.ClearAsync());

        Assert.Empty(service.Runs);
        Assert.DoesNotContain("0.mkv", await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(_filePath + ".tmp"));
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Empty(restarted.Runs);
    }

    [Fact]
    public async Task ClearAsync_DropsUnsavedRunsFromBeforeTheClear_InOtherInstances()
    {
        var first = CreateService();
        Directory.CreateDirectory(_filePath + ".tmp");
        await first.AddAsync(Run(0) with { RecordedAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
        Directory.Delete(_filePath + ".tmp");
        Assert.True(await CreateService().ClearAsync());

        var added = Run(1);
        await first.AddAsync(added);

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(added.Id, Assert.Single(first.Runs).Id);
        Assert.Equal(added.Id, Assert.Single(restarted.Runs).Id);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Runs":[]}""")]
    [InlineData("""{"Version":1}""")]
    [InlineData("""{"Version":1,"Runs":[null]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","Files":null}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","Files":[{"Status":"Succeeded"}]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","Files":[{"Path":"a.mkv","Status":99}]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","Files":[]},{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","Files":[]}]}""")]
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
        Assert.Empty(service.Runs);
        await service.AddAsync(run);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(run.Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task LoadAsync_ShowsAllSavedRuns_WhenTheLimitCannotBeRead()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 3).Select(Run))
        {
            await service.AddAsync(run);
        }

        _settings.BatchHistory.MaxRuns = 1;
        _userSettings.ReadFromDiskAsync().Returns(Task.FromException<UserSettings>(new IOException("settings file is locked")));
        var restarted = CreateService();
        await restarted.LoadAsync();

        Assert.Equal(3, restarted.Runs.Count);
    }

    [Fact]
    public async Task LoadAsync_TriesAgain_AfterAFailedLoad()
    {
        var run = Run(0);
        await CreateService().AddAsync(run);
        var service = CreateService();

        using (new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await service.LoadAsync();
        }

        Assert.Empty(service.Runs);
        await service.LoadAsync();
        Assert.Equal(run.Id, Assert.Single(service.Runs).Id);
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

    [Fact]
    public async Task AddAsync_UsesPersistedLimitFromAnotherSettingsInstance()
    {
        var settingsPath = Path.Combine(_directory, "UserSettings.json");
        var firstSettings = new WritableJsonSettings<UserSettings>(NullLogger<WritableJsonSettings<UserSettings>>.Instance, settingsPath);
        await firstSettings.UpdateAsync(settings => settings.BatchHistory.MaxRuns = 20);
        var secondSettings = new WritableJsonSettings<UserSettings>(NullLogger<WritableJsonSettings<UserSettings>>.Instance, settingsPath);
        var first = new BatchHistoryService(_filePath, firstSettings, NullLogger<BatchHistoryService>.Instance);
        var second = new BatchHistoryService(_filePath, secondSettings, NullLogger<BatchHistoryService>.Instance);

        Assert.Equal(0, await first.SetMaxRunsAsync(100));
        foreach (var run in Enumerable.Range(0, 25).Select(Run))
        {
            await first.AddAsync(run);
        }

        await second.AddAsync(Run(25));

        var restarted = new BatchHistoryService(_filePath, firstSettings, NullLogger<BatchHistoryService>.Instance);
        await restarted.LoadAsync();
        Assert.Equal(26, restarted.Runs.Count);
        Assert.Equal(100, second.MaxRuns);
    }

    [Fact]
    public async Task SetMaxRunsAsync_RestoresCachedLimitAndHistory_WhenSettingsSaveFails()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.Arg<Action<UserSettings>>()(_settings);
                throw new IOException("settings file is read-only");
            });

        await Assert.ThrowsAsync<IOException>(() => service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, service.MaxRuns);
        var afterFailure = CreateService();
        await afterFailure.LoadAsync();
        Assert.Equal(4, afterFailure.Runs.Count);

        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.Arg<Action<UserSettings>>()(_settings);
                return Task.CompletedTask;
            });
        Assert.Equal(0, await service.SetMaxRunsAsync(2, confirmedRemovals: 2));
        Assert.Equal(2, service.MaxRuns);
        Assert.Equal(2, service.Runs.Count);
    }

    [Fact]
    public async Task SetMaxRunsAsync_RestoresTheSavedLimit_WhenTheHistoryCannotBeTrimmed()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        Directory.CreateDirectory(_filePath + ".tmp");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, _settings.BatchHistory.MaxRuns);
        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, service.MaxRuns);
        Directory.Delete(_filePath + ".tmp");
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(4, restarted.Runs.Count);
    }

    [Fact]
    public async Task SetMaxRunsAsync_KeepsUsingTheSavedLowerLimit_WhenRestoringTheLimitFails()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        Directory.CreateDirectory(_filePath + ".tmp");
        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>())
            .Returns(
                call =>
                {
                    call.Arg<Action<UserSettings>>()(_settings);
                    return Task.CompletedTask;
                },
                call =>
                {
                    call.Arg<Action<UserSettings>>()(_settings);
                    throw new IOException("settings file is read-only");
                });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        Assert.Equal(2, service.MaxRuns);
        Assert.Equal(4, service.Runs.Count);
    }

    [Fact]
    public async Task SetMaxRunsAsync_DiscardsFailedLimit_WhenAnUnrelatedSettingsUpdateFollows()
    {
        var settingsPath = Path.Combine(_directory, "UserSettings.json");
        var userSettings = new WritableJsonSettings<UserSettings>(
            NullLogger<WritableJsonSettings<UserSettings>>.Instance,
            settingsPath);
        await userSettings.UpdateAsync(settings => settings.BatchHistory.MaxRuns = 20);
        var service = new BatchHistoryService(_filePath, userSettings, NullLogger<BatchHistoryService>.Instance);
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        var temporaryPath = settingsPath + ".tmp";
        Directory.CreateDirectory(temporaryPath);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.SetMaxRunsAsync(2, confirmedRemovals: 2));
        Assert.Equal(20, userSettings.Value.BatchHistory.MaxRuns);

        Directory.Delete(temporaryPath);
        await userSettings.UpdateAsync(settings => settings.UI.Theme = "Dark");

        var savedSettings = await userSettings.ReadFromDiskAsync();
        Assert.Equal(20, savedSettings.BatchHistory.MaxRuns);
        Assert.Equal("Dark", savedSettings.UI.Theme);
        Assert.Equal(4, service.Runs.Count);
    }

    private BatchHistoryService CreateService() => new(_filePath, _userSettings, NullLogger<BatchHistoryService>.Instance);

    private static BatchRunRecord Run(int minutes) => new(
        Guid.NewGuid(),
        new DateTimeOffset(2026, 11, 23, 19, minutes, 0, TimeSpan.Zero),
        [new FileRunRecord($"{minutes}.mkv", ProcessingStatus.Succeeded, null, null, null, null, Array.Empty<string>(), Array.Empty<string>())]);
}
