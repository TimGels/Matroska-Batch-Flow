using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MKVBatchFlow.Core;
using MKVBatchFlow.Core.Scanning;

const string settingsFile = "appsettings.json";

// Ensure config file exists
if (!File.Exists(settingsFile))
{
    var defaultOptions = new
    {
        ScanOptions = new ScanOptions
        {
            DirectoryPath = Directory.GetCurrentDirectory(),
            AllowedExtensions = [".mkv"],
            Recursive = true,
            ExcludeHidden = false
        }
    };

    var defaultJson = JsonSerializer.Serialize(defaultOptions, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(settingsFile, defaultJson);

    Console.WriteLine("✅ Created default appsettings.json.");
}

var builder = Host.CreateDefaultBuilder(args);

// Explicitly configure JSON with reloadOnChange = true
builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.AddJsonFile(settingsFile, optional: false, reloadOnChange: true);
});

builder.ConfigureServices((context, services) =>
{
    services.Configure<ScanOptions>(context.Configuration.GetSection("ScanOptions"));
    services.AddSingleton<IFileScanner, FileScanner>();
});

var app = builder.Build();

var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<ScanOptions>>();
var currentOptions = optionsMonitor.CurrentValue;

Console.WriteLine("🎬 MKV Batch Flow - Console Scanner");

Console.WriteLine("Current directory path: " + currentOptions.DirectoryPath);
Console.WriteLine("Enter a new directory path (or press Enter to keep current):");
var directoryPath = Console.ReadLine()?.Trim('"');
if (!string.IsNullOrEmpty(directoryPath))
{
    currentOptions.DirectoryPath = directoryPath;
}

Console.WriteLine("Scan recursively? (y/n):");
var recursiveInput = Console.ReadLine()?.Trim().ToLower();
currentOptions.Recursive = recursiveInput == "y";

Console.WriteLine("Exclude hidden files? (y/n):");
var excludeHiddenInput = Console.ReadLine()?.Trim().ToLower();
currentOptions.ExcludeHidden = excludeHiddenInput == "y";

// ✅ Write updated values back to appsettings.json
var updatedJson = new
{
    ScanOptions = currentOptions
};

var json = JsonSerializer.Serialize(updatedJson, new JsonSerializerOptions
{
    WriteIndented = true
});

await File.WriteAllTextAsync(settingsFile, json);
Console.WriteLine("\n✅ Settings saved to appsettings.json.");

var scanner = app.Services.GetRequiredService<IFileScanner>();

try
{
    var files = await scanner.ScanWithMediaInfoAsync();

    Console.WriteLine($"\n📁 Found {files.Count()} MKV file(s):");
    foreach (var file in files)
    {
        Console.WriteLine($" - {file}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
