using System.Diagnostics;
using System.Text.Json;
using MatroskaBatchFlow.Uno.Models;
using Peer = MatroskaBatchFlow.HistoryPeer.HistoryPeer;

namespace MatroskaBatchFlow.Uno.IntegrationTests;

public sealed class BatchHistoryProcessTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("mbf-history-process-").FullName;
    private string HistoryPath => Path.Combine(_directory, "BatchHistory.json");

    [Fact]
    public async Task ClearReplacesAnOpenSnapshot_WithoutRestoringPendingOrQueuedRuns()
    {
        var history = Peer.OpenHistory(_directory);
        var seed = Run(1);
        await history.AddAsync(seed);
        var before = await File.ReadAllTextAsync(HistoryPath, TestContext.Current.CancellationToken);
        using var snapshot = new StreamReader(File.Open(HistoryPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));

        Directory.CreateDirectory(HistoryPath + ".tmp");
        Task? queued = null;
        history.HasUnsavedRunsChanged += ClearWhileAddIsQueued;
        await history.AddAsync(Run(2));
        Assert.NotNull(queued);
        await queued.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Equal(before, await snapshot.ReadToEndAsync(TestContext.Current.CancellationToken));
        Assert.Empty(history.Runs);
        Assert.Empty(await ReadSavedIdsAsync());
        Assert.False(history.HasUnsavedRuns);

        var after = Run(0);
        await history.AddAsync(after);
        Assert.Equal([after.Id], await ReadSavedIdsAsync());

        void ClearWhileAddIsQueued(object? sender, EventArgs args)
        {
            history.HasUnsavedRunsChanged -= ClearWhileAddIsQueued;
            Assert.True(history.HasUnsavedRuns);
            Directory.Delete(HistoryPath + ".tmp");
            // The warning is raised before the local gate is released. The next add takes its snapshot now,
            // but cannot merge until this callback returns and the other process has committed the clear.
            queued = history.AddAsync(Run(3));
            Assert.False(queued.IsCompleted);
            Assert.Equal(0, RunPeer("clear"));
        }
    }

    [Fact]
    public async Task ClearCannotReplaceHistoryWhileAnotherProcessOwnsTheSidecar()
    {
        var seed = Run(1);
        await Peer.OpenHistory(_directory).AddAsync(seed);
        var before = await File.ReadAllBytesAsync(HistoryPath, TestContext.Current.CancellationToken);
        using (File.Open(HistoryPath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Equal(Peer.ClearRefused, RunPeer("clear"));
            Assert.Equal(before, await File.ReadAllBytesAsync(HistoryPath, TestContext.Current.CancellationToken));
        }

        Assert.Equal(0, RunPeer("clear"));
        Assert.Empty(await ReadSavedIdsAsync());
    }

    [Fact]
    public async Task ConcurrentAddRequiresFreshRetentionConfirmation()
    {
        var history = Peer.OpenHistory(_directory);
        var first = Run(1);
        var second = Run(2);
        await history.AddAsync(first);
        await history.AddAsync(second);
        Assert.Equal(1, await history.SetMaxRunsAsync(1));

        var peerRun = Guid.NewGuid();
        Assert.Equal(0, RunPeer("add", peerRun.ToString(), "3"));
        Assert.Equal(2, await history.SetMaxRunsAsync(1, confirmedRemovals: 1));
        Assert.Equal([peerRun, second.Id, first.Id], await ReadSavedIdsAsync());

        Assert.Equal(0, await history.SetMaxRunsAsync(1, confirmedRemovals: 2));
        Assert.Equal([peerRun], await ReadSavedIdsAsync());
        var next = Guid.NewGuid();
        Assert.Equal(0, RunPeer("add", next.ToString(), "4"));
        Assert.Equal([next], await ReadSavedIdsAsync());
    }

    private static BatchRunRecord Run(int seconds) => new(Guid.NewGuid(), DateTimeOffset.UnixEpoch.AddSeconds(seconds), []);

    private async Task<Guid[]> ReadSavedIdsAsync()
    {
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(HistoryPath, TestContext.Current.CancellationToken));
        return document.RootElement.GetProperty("Runs").EnumerateArray().Select(run => run.GetProperty("Id").GetGuid()).ToArray();
    }

    private int RunPeer(params string[] args)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            UseShellExecute = false,
            WorkingDirectory = _directory,
            ArgumentList = { typeof(Peer).Assembly.Location }
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the history peer.");
        if (!process.WaitForExit(TimeSpan.FromSeconds(30)))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException($"The history peer timed out during '{args[0]}'.");
        }

        return process.ExitCode;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
