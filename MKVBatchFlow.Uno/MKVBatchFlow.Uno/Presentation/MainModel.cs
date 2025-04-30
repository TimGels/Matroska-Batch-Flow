using System.Text.Json;
using MKVBatchFlow.Core.Scanning;
namespace MKVBatchFlow.Uno.Presentation;

public partial record MainModel
{
    private INavigator _navigator;
    private IFileScanner _fileScanner;

    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator,
        IFileScanner fileScanner)
    {
        _navigator = navigator;
        _fileScanner = fileScanner;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
    }

    public string? Title { get; }

    public IState<string> Name => State<string>.Value(this, () => string.Empty);

    public string MediaInfo { get; set; } = "empty media info text";

    public async Task GoToSecond()
    {
        var name = await Name;
        await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
    }

    public async Task RetrieveMediaInfo()
    {
        var mediaInfo = MediaInfo;
        MediaInfo = JsonSerializer.Serialize(await _fileScanner.ScanWithMediaInfoAsync(), new JsonSerializerOptions { WriteIndented = true });
    }

}
