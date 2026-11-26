using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;
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
        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>(), Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.ArgAt<Action<UserSettings>>(0)(_settings);
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
    public async Task RemoveAsync_SelectingAllListedRuns_PreservesTheClearMarker()
    {
        var service = CreateService();
        Assert.True(await service.ClearAsync());
        await service.AddAsync(Run(0));
        File.Copy(_filePath, _filePath + ".tmp");

        Assert.Equal(HistoryRemovalStatus.Removed, (await service.RemoveAsync(SelectAll(service))).Status);

        Assert.Empty(service.Runs);
        var clearedContent = await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken);
        using var document = System.Text.Json.JsonDocument.Parse(clearedContent);
        Assert.Equal(3, document.RootElement.GetProperty("Version").GetInt32());
        Assert.NotEqual(Guid.Empty, document.RootElement.GetProperty("ClearId").GetGuid());

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
        await first.AddAsync(Run(0) with { RecordedAt = DateTimeOffset.UtcNow.AddDays(1) });
        Assert.True(first.HasUnsavedRuns);
        Directory.Delete(_filePath + ".tmp");
        Assert.True(await CreateService().ClearAsync());

        var added = Run(1);
        await first.AddAsync(added);

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(added.Id, Assert.Single(first.Runs).Id);
        Assert.Equal(added.Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task AddAsync_RetriesAPostClearRunWithAnEarlierRecordedTime()
    {
        var service = CreateService();
        Assert.True(await CreateService().ClearAsync());
        var pending = Run(1) with { RecordedAt = DateTimeOffset.UnixEpoch };
        Directory.CreateDirectory(_filePath + ".tmp");
        await service.AddAsync(pending);
        Assert.True(service.HasUnsavedRuns);

        await service.LoadAsync();
        Assert.True(service.HasUnsavedRuns);
        Assert.Equal(pending.Id, Assert.Single(service.Runs).Id);

        Directory.Delete(_filePath + ".tmp");
        var next = Run(2);
        await service.AddAsync(next);
        Assert.False(service.HasUnsavedRuns);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal([next.Id, pending.Id], restarted.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task AddAsync_SavesAfterALegacyClearEvenWhenTheClockHasMovedBack()
    {
        var cleared = """{"Version":2,"ClearedAt":"2099-01-01T00:00:00Z","Runs":[]}""";
        await File.WriteAllTextAsync(_filePath, cleared, TestContext.Current.CancellationToken);

        var run = Run(1);
        var service = CreateService();
        await service.AddAsync(run);

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(run.Id, Assert.Single(restarted.Runs).Id);
        Assert.False(service.HasUnsavedRuns);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAsync_WhenTheInitialMarkerIsUnreadable_OnlyRetriesAcrossAKnownGeneration(bool sawClear)
    {
        var service = CreateService();
        var other = CreateService();
        Assert.True(await other.ClearAsync());
        if (sawClear)
        {
            await service.LoadAsync();
        }

        var pending = Run(1);
        using (new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await service.AddAsync(pending);
        }

        Assert.Equal(pending.Id, Assert.Single(service.Runs).Id);
        Assert.True(service.HasUnsavedRuns);

        var next = Run(2);
        await service.AddAsync(next);
        Assert.Equal([next.Id, pending.Id], service.Runs.Select(run => run.Id));
        Assert.Equal(!sawClear, service.HasUnsavedRuns);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(sawClear ? new[] { next.Id, pending.Id } : [next.Id], restarted.Runs.Select(run => run.Id));

        if (!sawClear)
        {
            // The session-only row does not consume retention or require confirmation to trim saved runs.
            Assert.Equal(0, await service.SetMaxRunsAsync(1));
            Assert.True(service.HasUnsavedRuns);
            Assert.Equal([next.Id, pending.Id], service.Runs.Select(run => run.Id));
            Assert.True(await other.ClearAsync());
            await service.LoadAsync();
            Assert.Empty(service.Runs);
            Assert.False(service.HasUnsavedRuns);
        }
    }

    [Fact]
    public async Task AddAsync_DoesNotShowRunsAnotherInstanceCleared_WhenSavingFails()
    {
        var service = CreateService();
        await service.AddAsync(Run(0));
        await service.AddAsync(Run(1));
        Assert.True(await CreateService().ClearAsync());

        Directory.CreateDirectory(_filePath + ".tmp");
        var added = Run(2);
        await service.AddAsync(added);

        Assert.Equal(added.Id, Assert.Single(service.Runs).Id);
    }

    [Fact]
    public async Task AddAsync_DoesNotRestoreARunStartedBeforeAnotherInstanceClearsHistory()
    {
        var service = CreateService();
        await service.AddAsync(Run(0));

        using var storageLock = new FileStream(
            _filePath + ".lock",
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);
        var run = Run(1) with { RecordedAt = DateTimeOffset.UtcNow.AddMinutes(5) };
        var addTask = service.AddAsync(run);

        var clearedDocument = System.Text.Json.JsonSerializer.Serialize(new
        {
            Version = 3,
            Runs = Array.Empty<BatchRunRecord>(),
            ClearId = Guid.NewGuid()
        });
        await File.WriteAllTextAsync(_filePath + ".tmp", clearedDocument, TestContext.Current.CancellationToken);
        File.Move(_filePath + ".tmp", _filePath, overwrite: true);

        storageLock.Dispose();
        await addTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Empty(service.Runs);
        Assert.Empty(restarted.Runs);
    }

    [Fact]
    public async Task LoadAsync_ReadsVersionOneHistory_AndAcceptsAnExplicitPendingStatus()
    {
        var run = new BatchRunRecord(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            [new FileRunRecord("pending.mkv", ProcessingStatus.Pending, null, null, null, null, [], [])]);
        var options = new System.Text.Json.JsonSerializerOptions();
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var legacyDocument = System.Text.Json.JsonSerializer.Serialize(
            new { Version = 1, Runs = new[] { run } },
            options);
        await File.WriteAllTextAsync(_filePath, legacyDocument, TestContext.Current.CancellationToken);
        var service = CreateService();

        await service.LoadAsync();

        var loadedRun = Assert.Single(service.Runs);
        Assert.Equal(run.Id, loadedRun.Id);
        Assert.Equal(ProcessingStatus.Pending, Assert.Single(loadedRun.Files).Status);

        await service.AddAsync(Run(1));
        var rewrittenContent = await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken);
        using var rewrittenDocument = System.Text.Json.JsonDocument.Parse(rewrittenContent);
        Assert.Equal(1, rewrittenDocument.RootElement.GetProperty("Version").GetInt32());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Runs":[]}""")]
    [InlineData("""{"Version":1}""")]
    [InlineData("""{"Version":1,"Runs":[null]}""")]
    [InlineData("""{"Version":1,"Runs":[{"RecordedAt":"2026-11-23T19:00:00Z","Files":[]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","Files":[]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","RecordedAt":"2026-11-23T19:00:00Z","Files":[{"Path":"a.mkv"}]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","RecordedAt":"2026-11-23T19:00:00Z","Files":null}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","RecordedAt":"2026-11-23T19:00:00Z","Files":[{"Status":"Succeeded"}]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","RecordedAt":"2026-11-23T19:00:00Z","Files":[{"Path":"a.mkv","Status":99}]}]}""")]
    [InlineData("""{"Version":1,"Runs":[{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","RecordedAt":"2026-11-23T19:00:00Z","Files":[]},{"Id":"8f0c1d6e-2b1e-4a57-9a43-5d0b8e3f0a11","RecordedAt":"2026-11-23T19:00:00Z","Files":[]}]}""")]
    [InlineData("""{"Version":2,"Runs":[]}""")]
    [InlineData("""{"Version":2,"ClearedAt":null,"Runs":[]}""")]
    [InlineData("""{"Version":2,"ClearedAt":"0001-01-01T00:00:00+00:00","Runs":[]}""")]
    [InlineData("""{"Version":3,"Runs":[]}""")]
    [InlineData("""{"Version":3,"ClearId":"00000000-0000-0000-0000-000000000000","Runs":[]}""")]
    public async Task AddAsync_PreservesAnUnreadableFileUntilExplicitlyCleared(string content)
    {
        await File.WriteAllTextAsync(_filePath, content, TestContext.Current.CancellationToken);
        var service = CreateService();

        await service.LoadAsync();

        Assert.Empty(service.Runs);
        Assert.True(service.HasUnreadableHistory);
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

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Runs":[]}""")]
    public async Task ResetUnreadableHistory_PreservesSessionRuns_WhenRebuildingAnUnreadableFile(string content)
    {
        await File.WriteAllTextAsync(_filePath, content, TestContext.Current.CancellationToken);
        var service = CreateService();
        await service.LoadAsync();
        var run = Run(0);
        await service.AddAsync(run);

        Assert.Equal(content, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
        Assert.Equal(HistoryRecoveryStatus.Recovered, (await service.ResetUnreadableHistoryAsync()).Status);
        Assert.False(service.HasUnreadableHistory);

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
    public async Task AddAsync_SavesAllRunsWithoutTrimming_WhenTheLimitCannotBeRead()
    {
        _settings.BatchHistory.MaxRuns = 100;
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 25).Select(Run))
        {
            await service.AddAsync(run);
        }

        _settings.BatchHistory.MaxRuns = UserSettings.BatchHistorySettings.DefaultMaxRuns;
        _userSettings.ReadFromDiskAsync().Returns(Task.FromException<UserSettings>(new IOException("settings file is locked")));
        var restarted = CreateService();
        await restarted.LoadAsync();

        Assert.Equal(25, restarted.Runs.Count);
        await restarted.AddAsync(Run(25));

        Assert.Equal(26, restarted.Runs.Count);
        var reloaded = CreateService();
        await reloaded.LoadAsync();
        Assert.Equal(26, reloaded.Runs.Count);
    }

    [Fact]
    public async Task LoadAsync_ShowsAllSavedRuns_WhenTheHistorySettingsSectionIsNull()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 3).Select(Run))
        {
            await service.AddAsync(run);
        }

        var invalidSettings = new UserSettings { BatchHistory = null! };
        _userSettings.ReadFromDiskAsync().Returns(Task.FromResult(invalidSettings));
        var restarted = CreateService();

        await restarted.LoadAsync();

        Assert.Equal(3, restarted.Runs.Count);
    }

    [Fact]
    public async Task AddAsync_SavesAllRunsWithoutTrimming_WhenTheHistorySettingsSectionIsNull()
    {
        _settings.BatchHistory.MaxRuns = 100;
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 25).Select(Run))
        {
            await service.AddAsync(run);
        }

        _settings.BatchHistory.MaxRuns = UserSettings.BatchHistorySettings.DefaultMaxRuns;
        _userSettings.ReadFromDiskAsync().Returns(Task.FromResult(new UserSettings { BatchHistory = null! }));
        var restarted = CreateService();
        await restarted.LoadAsync();

        Assert.Equal(25, restarted.Runs.Count);
        await restarted.AddAsync(Run(25));

        Assert.Equal(26, restarted.Runs.Count);
        var reloaded = CreateService();
        await reloaded.LoadAsync();
        Assert.Equal(26, reloaded.Runs.Count);
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
        Assert.Equal(HistoryRemovalStatus.Removed, (await first.RemoveAsync(SelectAll(first))).Status);

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
    public async Task SetMaxRunsAsync_RestoresHistoryAndViewState_WhenSavingTheLimitFails()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        var collectionChangedCount = 0;
        var runsCollection = (System.Collections.Specialized.INotifyCollectionChanged)service.Runs;
        runsCollection.CollectionChanged += (_, _) => collectionChangedCount++;
        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>(), Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.ArgAt<Action<UserSettings>>(0)(_settings);
                call.ArgAt<Action<UserSettings>>(1)(_settings);
                throw new IOException("settings file is read-only");
            });

        var exception = await Assert.ThrowsAsync<BatchHistoryLimitSaveException>(
            () => service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        Assert.False(exception.HistoryTrimmed);
        Assert.IsType<IOException>(exception.InnerException);
        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, service.MaxRuns);
        Assert.Equal(0, collectionChangedCount);
        Assert.Equal(4, service.Runs.Count);
        var afterFailure = CreateService();
        await afterFailure.LoadAsync();
        Assert.Equal(4, afterFailure.Runs.Count);

        _userSettings.UpdateAsync(Arg.Any<Action<UserSettings>>(), Arg.Any<Action<UserSettings>>())
            .Returns(call =>
            {
                call.ArgAt<Action<UserSettings>>(0)(_settings);
                return Task.CompletedTask;
            });
        Assert.Equal(0, await service.SetMaxRunsAsync(2, confirmedRemovals: 2));
        Assert.Equal(2, service.MaxRuns);
        Assert.Equal(2, service.Runs.Count);
        Assert.NotEqual(0, collectionChangedCount);
    }

    [Fact]
    public async Task SetMaxRunsAsync_KeepsTheLimitAndHistory_WhenTheHistoryCannotBeTrimmed()
    {
        var service = CreateService();
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        Directory.CreateDirectory(_filePath + ".tmp");
        _userSettings.ClearReceivedCalls();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        await _userSettings.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default!);
        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, service.MaxRuns);
        Directory.Delete(_filePath + ".tmp");
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(4, restarted.Runs.Count);
    }

    [Fact]
    public async Task SetMaxRunsAsync_ShowsTheSavedRuns_WhenTheHistoryCannotBeTrimmed()
    {
        var first = CreateService();
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await first.AddAsync(run);
        }

        var second = CreateService();
        await second.LoadAsync();
        var removedElsewhere = first.Runs[0];
        Assert.Equal(HistoryRemovalStatus.Removed, (await first.RemoveAsync(new([removedElsewhere.Id]))).Status);
        Directory.CreateDirectory(_filePath + ".tmp");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => second.SetMaxRunsAsync(2, confirmedRemovals: 1));

        Assert.Equal(3, second.Runs.Count);
        Assert.DoesNotContain(second.Runs, run => run.Id == removedElsewhere.Id);
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
        var exception = await Assert.ThrowsAsync<BatchHistoryLimitSaveException>(
            () => service.SetMaxRunsAsync(2, confirmedRemovals: 2));
        Assert.False(exception.HistoryTrimmed);
        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
        Assert.Equal(20, userSettings.Value.BatchHistory.MaxRuns);
        Assert.Equal(4, service.Runs.Count);

        Directory.Delete(temporaryPath);
        await userSettings.UpdateAsync(settings => settings.UI.Theme = "Dark");

        var savedSettings = await userSettings.ReadFromDiskAsync();
        Assert.Equal(20, savedSettings.BatchHistory.MaxRuns);
        Assert.Equal("Dark", savedSettings.UI.Theme);
        var restarted = new BatchHistoryService(_filePath, userSettings, NullLogger<BatchHistoryService>.Instance);
        await restarted.LoadAsync();
        Assert.Equal(4, restarted.Runs.Count);
    }

    [Fact]
    public async Task RemoveAsync_PreservesOtherInstancesChanges_WithoutTrimmingOrResurrectingRuns()
    {
        var first = CreateService();
        var older = Run(0);
        var removed = Run(1);
        var alsoRemoved = Run(4);
        await first.AddAsync(older);
        await first.AddAsync(removed);
        await first.AddAsync(alsoRemoved);

        var second = CreateService();
        var recent = Run(2);
        await second.AddAsync(recent);
        _settings.BatchHistory.MaxRuns = 1;

        var result = await first.RemoveAsync(new([removed.Id, alsoRemoved.Id, Guid.NewGuid()]));
        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 2), result);
        Assert.Equal([recent.Id, older.Id], first.Runs.Select(run => run.Id));

        _settings.BatchHistory.MaxRuns = 20;
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal([recent.Id, older.Id], restarted.Runs.Select(run => run.Id));

        var newest = Run(3);
        await second.AddAsync(newest);
        Assert.Equal([newest.Id, recent.Id, older.Id], second.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task RemoveAsync_LastRunKeepsTheClearMarker_AndCanBeRepeated()
    {
        var service = CreateService();
        var run = Run(0);
        await service.AddAsync(run);
        var stale = CreateService();
        await stale.LoadAsync();
        File.Copy(_filePath, _filePath + ".tmp");

        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 1), await service.RemoveAsync(new([run.Id])));
        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 0), await stale.RemoveAsync(new([run.Id])));

        Assert.Empty(service.Runs);
        Assert.Empty(stale.Runs);
        Assert.True(File.Exists(_filePath));
        Assert.False(File.Exists(_filePath + ".tmp"));
        Assert.True(File.Exists(_filePath + ".lock"));
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Empty(restarted.Runs);
    }

    [Fact]
    public async Task RemoveAsync_WhenAnotherInstanceAlreadyRemovedRun_DoesNotWriteOrReportFailure()
    {
        var first = CreateService();
        var run = Run(0);
        await first.AddAsync(run);
        var stale = CreateService();
        await stale.LoadAsync();
        await first.RemoveAsync(new([run.Id]));
        var blockedPath = Directory.CreateDirectory(_filePath + ".tmp");

        var result = await stale.RemoveAsync(new([run.Id]));

        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 0), result);
        Assert.Empty(stale.Runs);
        Assert.True(Directory.Exists(blockedPath.FullName));
    }

    [Fact]
    public async Task RemoveAsync_WhenWriteFails_PreservesDiskAndListedRuns_UntilRetry()
    {
        var service = CreateService();
        var kept = Run(0);
        var removed = Run(1);
        var other = Run(2);
        await service.AddAsync(kept);
        await service.AddAsync(removed);
        await service.AddAsync(other);
        var content = await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken);
        var blockedPath = Directory.CreateDirectory(_filePath + ".tmp");

        var removal = new HistoryRemoval([removed.Id, other.Id]);
        Assert.Equal(HistoryRemovalStatus.Failed, (await service.RemoveAsync(removal)).Status);

        Assert.Equal([other.Id, removed.Id, kept.Id], service.Runs.Select(run => run.Id));
        Assert.Equal(content, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
        blockedPath.Delete();

        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 2), await service.RemoveAsync(removal));
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(kept.Id, Assert.Single(restarted.Runs).Id);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Version":99,"Runs":[]}""")]
    public async Task RemoveAsync_DoesNotOverwriteUnreadableHistory(string content)
    {
        await File.WriteAllTextAsync(_filePath, content, TestContext.Current.CancellationToken);
        var service = CreateService();
        var unsaved = Run(0);
        await service.AddAsync(unsaved);

        Assert.Equal(HistoryRemovalStatus.Failed, (await service.RemoveAsync(new([unsaved.Id]))).Status);

        Assert.Equal(unsaved.Id, Assert.Single(service.Runs).Id);
        Assert.Equal(content, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_RemovesAnUnsavedRun_AndPersistsOtherUnsavedRuns()
    {
        var service = CreateService();
        var saved = Run(0);
        var unsaved = Run(1);
        var removed = Run(2);
        await service.AddAsync(saved);
        var blockedPath = Directory.CreateDirectory(_filePath + ".tmp");
        await service.AddAsync(unsaved);
        await service.AddAsync(removed);
        blockedPath.Delete();

        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 1), await service.RemoveAsync(new([removed.Id])));

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal([unsaved.Id, saved.Id], restarted.Runs.Select(run => run.Id));
        var newest = Run(3);
        await service.AddAsync(newest);
        Assert.Equal([newest.Id, unsaved.Id, saved.Id], service.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task RemoveAsync_SerializesConcurrentRemovals()
    {
        var first = CreateService();
        var second = CreateService();
        var runs = Enumerable.Range(0, 3).Select(Run).ToArray();
        foreach (var run in runs)
        {
            await first.AddAsync(run);
        }
        await second.LoadAsync();

        var results = await Task.WhenAll(first.RemoveAsync(new([runs[0].Id])), second.RemoveAsync(new([runs[1].Id])));

        Assert.All(results, result => Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 1), result));
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(runs[2].Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task SelectAll_DoesNotRemoveANewRun_WhenAnotherInstanceReplacesASelectedRun()
    {
        var first = CreateService();
        var original = Run(0);
        await first.AddAsync(original);
        var confirmed = SelectAll(first);
        var second = CreateService();
        await second.RemoveAsync(new([original.Id]));
        var replacement = Run(1);
        await second.AddAsync(replacement);

        Assert.Equal(new HistoryRemovalResult(HistoryRemovalStatus.Removed, 0), await first.RemoveAsync(confirmed));
        Assert.Equal(replacement.Id, Assert.Single(first.Runs).Id);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(replacement.Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task ResetUnreadableHistory_LeavesAConcurrentlyRepairedFileUntouched()
    {
        var first = CreateService();
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        await first.LoadAsync();
        Assert.True(first.HasUnreadableHistory);

        var second = CreateService();
        await second.LoadAsync();
        Assert.Equal(HistoryRecoveryStatus.Recovered, (await second.ResetUnreadableHistoryAsync()).Status);
        Assert.True(File.Exists(_filePath));
        var saved = Run(1);
        await second.AddAsync(saved);
        var content = await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken);

        Assert.Equal(HistoryRecoveryStatus.Recovered, (await first.ResetUnreadableHistoryAsync()).Status);
        Assert.False(first.HasUnreadableHistory);
        Assert.Equal(saved.Id, Assert.Single(first.Runs).Id);
        Assert.Equal(content, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetUnreadableHistory_PersistsPendingRunsAfterUnreadableFileIsRepaired_AndCanRetry()
    {
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        var first = CreateService();
        var pending = Run(1);
        await first.AddAsync(pending);
        Assert.True(first.HasUnreadableHistory);

        File.Delete(_filePath);
        var repaired = Run(2);
        await CreateService().AddAsync(repaired);
        var blockedPath = Directory.CreateDirectory(_filePath + ".tmp");

        Assert.Equal(HistoryRecoveryStatus.Failed, (await first.ResetUnreadableHistoryAsync()).Status);
        Assert.True(first.HasUnreadableHistory);
        Assert.Equal(pending.Id, Assert.Single(first.Runs).Id);
        blockedPath.Delete();
        Assert.Equal(HistoryRecoveryStatus.Recovered, (await first.ResetUnreadableHistoryAsync()).Status);
        Assert.False(first.HasUnreadableHistory);

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal([repaired.Id, pending.Id], restarted.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task ResetUnreadableHistory_PersistsPendingRunsAfterAnotherInstanceDeletesTheFile()
    {
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        var first = CreateService();
        var pending = Run(1);
        await first.AddAsync(pending);
        File.Delete(_filePath);

        Assert.Equal(HistoryRecoveryStatus.Recovered, (await first.ResetUnreadableHistoryAsync()).Status);

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(pending.Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task ResetUnreadableHistory_FailureKeepsRecoveryAvailableForRetry()
    {
        var service = CreateService();
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        await service.LoadAsync();
        var blockedPath = Directory.CreateDirectory(_filePath + ".tmp");

        Assert.Equal(HistoryRecoveryStatus.Failed, (await service.ResetUnreadableHistoryAsync()).Status);
        Assert.True(service.HasUnreadableHistory);
        Assert.Equal("invalid", await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));

        blockedPath.Delete();
        Assert.Equal(HistoryRecoveryStatus.Recovered, (await service.ResetUnreadableHistoryAsync()).Status);
        Assert.False(service.HasUnreadableHistory);
        Assert.True(File.Exists(_filePath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Recovery_RechecksMissingIdsAndPersistsOnlyTheChosenResults(bool restore)
    {
        var first = CreateService();
        var previouslySaved = Run(0);
        var shared = Run(1);
        await first.AddAsync(previouslySaved);
        await first.AddAsync(shared);
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        var pending = Run(2);
        await first.AddAsync(pending);

        var other = CreateService();
        await other.LoadAsync();
        await other.ResetUnreadableHistoryAsync();
        await other.AddAsync(shared);
        var remote = Run(3);
        await other.AddAsync(remote);
        var disk = await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken);

        var prompt = await first.ResetUnreadableHistoryAsync();
        Assert.Equal(HistoryRecoveryStatus.ChooseMissingResults, prompt.Status);
        Assert.Equal(previouslySaved.Id, Assert.Single(prompt.MissingIds));
        Assert.Equal([pending.Id, shared.Id, previouslySaved.Id], first.Runs.Select(run => run.Id));
        Assert.Equal(disk, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));

        // Ordinary operations must not apply a recovery choice implicitly.
        _settings.BatchHistory.MaxRuns = 3;
        var later = Run(4);
        await first.AddAsync(later);
        await Assert.ThrowsAsync<InvalidDataException>(() => first.SetMaxRunsAsync(10));
        Assert.True(first.HasUnreadableHistory);
        Assert.Equal([later.Id, pending.Id, shared.Id, previouslySaved.Id], first.Runs.Select(run => run.Id));
        Assert.Equal(disk, await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));

        // The other instance swaps which result is absent: a count-only confirmation would still match.
        _settings.BatchHistory.MaxRuns = 10;
        await other.AddAsync(previouslySaved);
        await other.RemoveAsync(new([shared.Id]));
        var changed = await first.ResetUnreadableHistoryAsync(new(prompt.MissingIds, restore));
        Assert.Equal(HistoryRecoveryStatus.ChooseMissingResults, changed.Status);
        Assert.Equal(shared.Id, Assert.Single(changed.MissingIds));
        Assert.True(first.HasUnreadableHistory);
        Assert.Equal(HistoryRecoveryStatus.Recovered,
            (await first.ResetUnreadableHistoryAsync(new(changed.MissingIds, restore))).Status);

        Assert.False(first.HasUnreadableHistory);
        Assert.True(first.HasUnsavedRuns);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Guid[] persisted = restore ? [later.Id, remote.Id, shared.Id, previouslySaved.Id] : [later.Id, remote.Id, previouslySaved.Id];
        Guid[] listed = restore ? [later.Id, remote.Id, pending.Id, shared.Id, previouslySaved.Id] : [later.Id, remote.Id, pending.Id, previouslySaved.Id];
        Assert.Equal(persisted, restarted.Runs.Select(run => run.Id));
        Assert.Equal(listed, first.Runs.Select(run => run.Id));
    }

    [Fact]
    public async Task Recovery_RequiresNewResetConfirmation_IfRepairedFileBecomesUnreadableDuringChoice()
    {
        var service = CreateService();
        var old = Run(0);
        await service.AddAsync(old);
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        Assert.Equal(HistoryRemovalStatus.Failed, (await service.RemoveAsync(new([old.Id]))).Status);
        File.Delete(_filePath);

        var conflict = await service.ResetUnreadableHistoryAsync();
        Assert.Equal(HistoryRecoveryStatus.ChooseMissingResults, conflict.Status);
        await File.WriteAllTextAsync(_filePath, "invalid again", TestContext.Current.CancellationToken);
        var result = await service.ResetUnreadableHistoryAsync(new(conflict.MissingIds, RestoreMissing: true));

        Assert.Equal(HistoryRecoveryStatus.ConfirmUnreadableReset, result.Status);
        Assert.Equal("invalid again", await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
        Assert.Equal(old.Id, Assert.Single(service.Runs).Id);
        Assert.True(service.HasUnreadableHistory);
    }

    [Fact]
    public async Task Recovery_StaysAvailableAfterANonTrimmingLimitChangeAndFailedAdd_UntilAWriteSucceeds()
    {
        var service = CreateService();
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        var pending = Run(1);
        await service.AddAsync(pending);
        File.Delete(_filePath);
        var remote = Run(0);
        await CreateService().AddAsync(remote);

        Assert.Equal(0, await service.SetMaxRunsAsync(10));
        Assert.True(service.HasUnreadableHistory);
        var blocked = Directory.CreateDirectory(_filePath + ".tmp");
        var another = Run(2);
        await service.AddAsync(another);
        Assert.True(service.HasUnreadableHistory);
        blocked.Delete();

        var latest = Run(3);
        await service.AddAsync(latest);
        Assert.False(service.HasUnreadableHistory);
        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal([latest.Id, another.Id, pending.Id, remote.Id], restarted.Runs.Select(run => run.Id));
    }

    private static HistoryRemoval SelectAll(BatchHistoryService service) =>
        new(service.Runs.Select(run => run.Id).ToImmutableHashSet());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetUnreadableHistory_AppliesThePersistedLimit_WhenRecovering(bool anotherInstanceRepairedHistory)
    {
        _settings.BatchHistory.MaxRuns = 1;
        await File.WriteAllTextAsync(_filePath, "invalid", TestContext.Current.CancellationToken);
        var service = CreateService();
        await service.LoadAsync();

        var older = Run(0);
        var newer = Run(1);
        await service.AddAsync(older);
        await service.AddAsync(newer);

        var expected = newer;
        if (anotherInstanceRepairedHistory)
        {
            expected = Run(2);
            var repairedPath = Path.Combine(_directory, "RepairedHistory.json");
            var otherInstance = new BatchHistoryService(repairedPath, _userSettings, NullLogger<BatchHistoryService>.Instance);
            await otherInstance.AddAsync(expected);
            File.Copy(repairedPath, _filePath, overwrite: true);
        }

        Assert.Equal(HistoryRecoveryStatus.Recovered, (await service.ResetUnreadableHistoryAsync()).Status);
        Assert.Equal(expected.Id, Assert.Single(service.Runs).Id);

        using var document = System.Text.Json.JsonDocument.Parse(
            await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
        Assert.Single(document.RootElement.GetProperty("Runs").EnumerateArray());

        var restarted = CreateService();
        await restarted.LoadAsync();
        Assert.Equal(expected.Id, Assert.Single(restarted.Runs).Id);
    }

    [Fact]
    public async Task SetMaxRunsAsync_DoesNotPersistTheLowerLimit_WhenHistoryCannotBeTrimmed()
    {
        var settingsPath = Path.Combine(_directory, "UserSettings.json");
        var userSettings = new WritableJsonSettings<UserSettings>(
            NullLogger<WritableJsonSettings<UserSettings>>.Instance,
            settingsPath);
        var service = new BatchHistoryService(_filePath, userSettings, NullLogger<BatchHistoryService>.Instance);
        foreach (var run in Enumerable.Range(0, 4).Select(Run))
        {
            await service.AddAsync(run);
        }

        Directory.CreateDirectory(_filePath + ".tmp");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.SetMaxRunsAsync(2, confirmedRemovals: 2));

        Assert.Equal(UserSettings.BatchHistorySettings.DefaultMaxRuns, service.MaxRuns);
        Assert.False(File.Exists(settingsPath));
        Directory.Delete(_filePath + ".tmp");

        var restartedSettings = new WritableJsonSettings<UserSettings>(
            NullLogger<WritableJsonSettings<UserSettings>>.Instance,
            settingsPath);
        var restarted = new BatchHistoryService(_filePath, restartedSettings, NullLogger<BatchHistoryService>.Instance);
        await restarted.LoadAsync();
        Assert.Equal(4, restarted.Runs.Count);
        await restarted.AddAsync(Run(4));
        Assert.Equal(5, restarted.Runs.Count);
    }

    [Fact]
    public async Task WritableSettingsUpdate_RestoresRejectedLimitBeforeTheNextUpdate_WhenSaveFails()
    {
        var settingsPath = Path.Combine(_directory, "SettingsUpdate.json");
        var settings = new WritableJsonSettings<UserSettings>(
            NullLogger<WritableJsonSettings<UserSettings>>.Instance,
            settingsPath);
        await settings.UpdateAsync(value => value.BatchHistory.MaxRuns = 20);

        using (new FileStream(settingsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            await Assert.ThrowsAsync<IOException>(
                () => settings.UpdateAsync(
                    value => value.BatchHistory.MaxRuns = 10,
                    value => value.BatchHistory.MaxRuns = 20));
        }

        await settings.UpdateAsync(value => value.UI.Theme = "Dark");

        var saved = await new WritableJsonSettings<UserSettings>(
            NullLogger<WritableJsonSettings<UserSettings>>.Instance,
            settingsPath).ReadFromDiskAsync();
        Assert.Equal(20, saved.BatchHistory.MaxRuns);
        Assert.Equal("Dark", saved.UI.Theme);
        Assert.Equal(20, settings.Value.BatchHistory.MaxRuns);
    }

    private BatchHistoryService CreateService() => new(_filePath, _userSettings, NullLogger<BatchHistoryService>.Instance);

    private static BatchRunRecord Run(int minutes) => new(
        Guid.NewGuid(),
        new DateTimeOffset(2026, 11, 23, 19, minutes, 0, TimeSpan.Zero),
        [new FileRunRecord($"{minutes}.mkv", ProcessingStatus.Succeeded, null, null, null, null, Array.Empty<string>(), Array.Empty<string>())]);
}
