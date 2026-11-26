using System.Globalization;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatroskaBatchFlow.HistoryPeer;

public static class HistoryPeer
{
    public const int ClearRefused = 10;

    public static BatchHistoryService OpenHistory(string directory) => new(
        Path.Combine(directory, "BatchHistory.json"),
        new WritableJsonSettings<UserSettings>(NullLogger<WritableJsonSettings<UserSettings>>.Instance,
            Path.Combine(directory, "UserSettings.json")),
        NullLogger<BatchHistoryService>.Instance);

    public static async Task<int> Main(string[] args)
    {
        var history = OpenHistory(Environment.CurrentDirectory);
        switch (args)
        {
            case ["clear"]:
                return await history.ClearAsync() ? 0 : ClearRefused;
            case ["add", var id, var seconds]:
                var time = DateTimeOffset.FromUnixTimeSeconds(long.Parse(seconds, CultureInfo.InvariantCulture));
                var run = new BatchRunRecord(Guid.Parse(id), time, []);
                await history.AddAsync(run);
                return !history.HasUnsavedRuns && history.Runs.Any(saved => saved.Id == run.Id) ? 0 : 1;
            default:
                throw new ArgumentException("Expected 'clear' or 'add <run-id> <unix-seconds>'.");
        }
    }
}
