using System.Diagnostics;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.Services.Processing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace MatroskaBatchFlow.Core.UnitTests.Services.Processing;

/// <summary>
/// Contains unit tests for the <see cref="MkvPropeditExecutor"/> class.
/// </summary>
public class MkvPropeditExecutorTests
{
    private const string FileArguments = "\"test.mkv\" --add-track-statistics-tags";

    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private ProcessStartInfo? _startInfo;

    [Fact]
    public async Task ExecuteAsync_ReportsProgress_AndRemovesProgressLinesFromOutput()
    {
        // Arrange
        SetupRunner((onLine, _) =>
        {
            onLine("The file is read in order to create track statistics.");
            onLine("Progress: 25%");
            onLine("Progress: 100%");
            return SuccessResult("The file is read in order to create track statistics.", "Progress: 25%", "Progress: 100%", "Done.");
        });
        var progress = new RecordingProgress();

        // Act
        var result = await CreateExecutor().ExecuteAsync(FileArguments, progress, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([25, 100], progress.Values);
        Assert.Equal(MkvPropeditStatus.Success, result.Status);
        Assert.DoesNotContain("Progress", result.StandardOutput);
        Assert.Contains("Done.", result.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_ForcesEnglishOutput_OnWindows()
    {
        // Arrange
        SetupRunner((_, _) => SuccessResult());

        // Act
        await CreateExecutor().ExecuteAsync(FileArguments, ct: TestContext.Current.CancellationToken);

        // Assert
        var expected = OperatingSystem.IsWindows() ? $"--ui-language en {FileArguments}" : FileArguments;
        Assert.Equal(expected, _startInfo!.Arguments);
    }

    [Fact]
    public async Task ExecuteAsync_StopsProcessAndThrows_WhenCanceledWhileReading()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        SetupRunner((onLine, stopToken) =>
        {
            onLine("Progress: 40%");
            cts.Cancel();

            // ProcessRunner throws once the token it observes is canceled.
            stopToken.ThrowIfCancellationRequested();
            return SuccessResult();
        });

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateExecutor().ExecuteAsync(FileArguments, ct: cts.Token));
        Assert.Equal(cts.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_LetsProcessFinish_WhenCanceledAfterReading()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var wasStopped = true;
        SetupRunner((onLine, stopToken) =>
        {
            onLine("Progress: 100%");
            cts.Cancel();
            wasStopped = stopToken.IsCancellationRequested;
            return SuccessResult();
        });

        // Act
        var result = await CreateExecutor().ExecuteAsync(FileArguments, ct: cts.Token);

        // Assert
        Assert.False(wasStopped);
        Assert.Equal(MkvPropeditStatus.Success, result.Status);
    }

    private void SetupRunner(Func<Action<string>, CancellationToken, ProcessExecutionResult> run)
    {
        _processRunner
            .RunAsync(Arg.Any<ProcessStartInfo>(), Arg.Any<Func<string, bool>?>(), Arg.Any<Action<string>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _startInfo = callInfo.ArgAt<ProcessStartInfo>(0);
                var onLine = callInfo.ArgAt<Action<string>?>(2) ?? (_ => { });
                return Task.FromResult(run(onLine, callInfo.ArgAt<CancellationToken>(3)));
            });
    }

    private MkvPropeditExecutor CreateExecutor()
    {
        var appOptions = Options.Create(new AppConfigOptions
        {
            Environment = "Test",
            MkvPropeditPath = "mkvpropedit",
            UserSettingsPath = "UserSettings.json"
        });

        // Any existing file resolves as the executable; the process itself is never started.
        var userSettings = Substitute.For<IWritableSettings<UserSettings>>();
        userSettings.Value.Returns(new UserSettings
        {
            MkvPropedit = new UserSettings.MkvPropeditSettings
            {
                IsCustomPathEnabled = true,
                CustomPath = Environment.ProcessPath
            }
        });

        return new MkvPropeditExecutor(appOptions, userSettings, _processRunner, Substitute.For<ILogger<MkvPropeditExecutor>>());
    }

    private static ProcessExecutionResult SuccessResult(params string[] stdOutLines)
    {
        return new ProcessExecutionResult(0, string.Join(Environment.NewLine, stdOutLines), string.Empty, [], Canceled: false);
    }

    private sealed class RecordingProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];

        public void Report(int value) => Values.Add(value);
    }
}
