using System.Text.Json;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Uno.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

public sealed class WritableJsonSettingsTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("mbf-settings-").FullName;
    private readonly string _filePath;

    public WritableJsonSettingsTests()
    {
        _filePath = Path.Combine(_directory, "UserSettings.json");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task UpdateAsync_KeepsValuesSavedByAnotherInstance()
    {
        var first = CreateSettings();
        var second = CreateSettings();

        await second.UpdateAsync(settings => settings.BatchHistory.MaxRuns = 100);
        await first.UpdateAsync(settings => settings.UI.Theme = "Dark");

        var saved = await CreateSettings().ReadFromDiskAsync();
        Assert.Equal(100, saved.BatchHistory.MaxRuns);
        Assert.Equal("Dark", saved.UI.Theme);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAnUnreadableFile()
    {
        var settings = CreateSettings();
        await File.WriteAllTextAsync(_filePath, "not json", TestContext.Current.CancellationToken);

        await settings.UpdateAsync(value => value.UI.Theme = "Dark");

        var saved = JsonSerializer.Deserialize<UserSettings>(await File.ReadAllTextAsync(_filePath, TestContext.Current.CancellationToken));
        Assert.Equal("Dark", saved!.UI.Theme);
    }

    [Fact]
    public async Task UpdateAsync_RetriesAChange_WhenThePreviousSaveFailed()
    {
        var settings = CreateSettings();
        var temporaryPath = _filePath + ".tmp";
        Directory.CreateDirectory(temporaryPath);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => settings.UpdateAsync(value => value.UI.Theme = "Dark"));

        Directory.Delete(temporaryPath);
        await settings.UpdateAsync(value => value.UI.Theme = "Dark");

        var saved = await CreateSettings().ReadFromDiskAsync();
        Assert.Equal("Dark", saved.UI.Theme);
    }

    [Fact]
    public async Task ReadFromDiskAsync_Throws_WhenThePathCannotBeRead()
    {
        // File.Exists reports an inaccessible path as missing, which must not be read as default settings.
        Directory.CreateDirectory(_filePath);
        var settings = CreateSettings();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => settings.ReadFromDiskAsync());
    }

    private WritableJsonSettings<UserSettings> CreateSettings() =>
        new(NullLogger<WritableJsonSettings<UserSettings>>.Instance, _filePath);
}
