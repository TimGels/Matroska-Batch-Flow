using System.Text.Json;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _scanResult = string.Empty;

    private readonly IFileScanner _fileScanner;

    public ICommand ScanFiles { get; }

    public MainViewModel(IFileScanner fileScanner)
    {
        _fileScanner = fileScanner;
        ScanFiles = new AsyncRelayCommand(ScanFilesAsync);
    }

    private async Task ScanFilesAsync()
    {
        var scannedFiles = await _fileScanner.ScanWithMediaInfoAsync();

        // Create a dictionary to map each file to its scan result
        var scanResults = scannedFiles.ToDictionary(
            file => file.Path,
            file => file.Result
        );

        // Serialize the dictionary to JSON
        ScanResult = JsonSerializer.Serialize(scanResults, new JsonSerializerOptions
        {
            WriteIndented = true // For pretty-printing
        });
    }
}
