using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Utilities.MediaInfoLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Scans directories for files based on specified options and analyzes them using MediaInfo.
/// </summary>
public partial class FileScanner : IFileScanner
{
    /// <summary>
    /// The options used for scanning directories and filtering files.
    /// </summary>
    private readonly ScanOptions _options;

    /// <summary>
    /// The logger for recording scan operations.
    /// </summary>
    private readonly ILogger<FileScanner> _logger;

    /// <summary>
    /// List of scanned files containing their paths and MediaInfo parsed results.
    /// </summary>
    private readonly List<ScannedFileInfo> _scannedFiles = [];

    public FileScanner(IOptionsMonitor<ScanOptions> optionsMonitor, ILogger<FileScanner> logger)
    {
        _options = optionsMonitor.CurrentValue;
        _logger = logger;
    }

    /// <summary>
    /// Scans the directory for files that match the specified filtering options.
    /// </summary>
    /// <param name="files">An array of <see cref="FileInfo"/> objects representing the files to scan.</param>
    /// <returns>A collection of <see cref="ScannedFileInfo"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when no files are provided for scanning.</exception>
    public async Task<IEnumerable<ScannedFileInfo>> ScanAsync(FileInfo[] files)
    {
        if (files == null || files.Length == 0)
            throw new ArgumentException("No files provided for scanning.", nameof(files));

        LogScanningFiles(files.Length);
        var scannedFiles = await AnalyzeFilesWithMediaInfoAsync(files.Select(f => f.FullName));

        _scannedFiles.Clear();
        _scannedFiles.AddRange(scannedFiles);
        LogScanCompleted(_scannedFiles.Count);
        return scannedFiles;
    }

    /// <summary>
    /// Scans the directory for files and analyzes them using MediaInfo.
    /// </summary>
    /// <returns> A collection of <see cref="ScannedFileInfo"/>.</returns>
    public async Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync()
    {
        EnsureDirectoryExists();
        LogScanningDirectory(_options.DirectoryPath, _options.Recursive);
        var files = await Task.Run(() => GetFilteredFiles());
        LogFilesFound(files.Count());
        var scannedFiles = await AnalyzeFilesWithMediaInfoAsync(files);

        _scannedFiles.Clear();
        _scannedFiles.AddRange(scannedFiles);

        return scannedFiles;
    }

    /// <summary>
    /// Gets the list of scanned files.
    /// </summary>
    /// <returns>A collection of <see cref="ScannedFileInfo"/>.</returns>
    public IEnumerable<ScannedFileInfo> GetScannedFiles() => _scannedFiles;

    /// <summary>
    /// Ensures that the directory specified in the options exists.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_options.DirectoryPath))
            throw new DirectoryNotFoundException($"Directory: '{_options.DirectoryPath}' does not exist.");
    }

    /// <summary>
    /// Retrieves files from the directory based on the filtering options.
    /// </summary>
    /// <returns>A collection of <see cref="string"/> file paths that match the filtering criteria.</returns>
    private IEnumerable<string> GetFilteredFiles()
    {
        var files = Directory.EnumerateFiles(
            _options.DirectoryPath,
            "*.*",
            _options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        if (_options.ExcludeHidden)
        {
            files = files.Where(f => (File.GetAttributes(f) & FileAttributes.Hidden) == 0);
        }

        return files.Where(f => _options.AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Parses the JSON MediaInfo summary into a <see cref="ScannedFileInfo"/> object.
    /// </summary>
    /// <param name="json"></param>
    /// <param name="filePath"></param>
    /// <returns><see cref="ScannedFileInfo"/></returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    private static ScannedFileInfo ParseMediaInfoJson(string json, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        var mediaInfoResult = JsonSerializer.Deserialize<MediaInfoResult>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize MediaInfo JSON.");

        return new ScannedFileInfo(mediaInfoResult, filePath);
    }

    /// <summary>
    ///  Analyzes the specified files using MediaInfo and returns detailed information.
    /// </summary>
    /// <param name="files"> The collection of file paths to analyze.</param>
    /// <returns> A collection of <see cref="ScannedFileInfo"/>.</returns>
    private static async Task<IEnumerable<ScannedFileInfo>> AnalyzeFilesWithMediaInfoAsync(IEnumerable<string> files)
    {
        return await Task.Run(() =>
        {
            var scannedFiles = new List<ScannedFileInfo>();
            var mediaInfo = new MediaInfo();

            foreach (var file in files)
            {
                mediaInfo.Open(file);
                mediaInfo.Option("Complete", "1");
                mediaInfo.Option("Output", "JSON");
                string info = mediaInfo.Inform();
                mediaInfo.Close();
                // Parse the JSON into a ScannedFileInfo object
                var scannedFile = ParseMediaInfoJson(info, file);
                scannedFiles.Add(scannedFile);
            }

            return scannedFiles;
        });
    }
}
