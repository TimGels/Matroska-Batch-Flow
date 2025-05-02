using System.Text.Json;
using MediaInfoLib;
using Microsoft.Extensions.Options;

namespace MKVBatchFlow.Core.Scanning
{
    /// <summary>
    /// Scans directories for files based on specified options and analyzes them using MediaInfo.
    /// </summary>
    public class FileScanner(IOptionsMonitor<ScanOptions> optionsMonitor) : IFileScanner
    {
        private readonly ScanOptions _options = optionsMonitor.CurrentValue;

        // Holds the list of scanned files
        private readonly List<ScannedFileInfo> _scannedFiles = [];

        /// <summary>
        /// Scans the directory for files that match the specified filtering options.
        /// </summary>
        /// <returns>A collection of file paths that match the filtering criteria.</returns>
        public async Task<IEnumerable<string>> ScanAsync()
        {
            EnsureDirectoryExists();
            var files = await Task.Run(() => GetFilteredFiles());
            return files;
        }

        /// <summary>
        /// Scans the directory for files and analyzes them using MediaInfo.
        /// </summary>
        /// <returns>A collection of <see cref="ScannedFileInfo"/> objects containing file paths and MediaInfo summaries.</returns>
        public async Task<IEnumerable<ScannedFileInfo>> ScanWithMediaInfoAsync()
        {
            EnsureDirectoryExists();
            var files = await Task.Run(() => GetFilteredFiles());
            var scannedFiles = await AnalyzeFilesWithMediaInfoAsync(files);

            // Store the scanned files in the private list
            _scannedFiles.Clear();
            _scannedFiles.AddRange(scannedFiles);

            return scannedFiles;
        }

        /// <summary>
        /// Parses the JSON MediaInfo summary into a <see cref="ScannedFileInfo"/> object.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="filePath">The file path associated with the JSON.</param>
        /// <returns>A <see cref="ScannedFileInfo"/> object.</returns>
        private static ScannedFileInfo ParseMediaInfoJson(string json, string filePath)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mediaInfoResult = JsonSerializer.Deserialize<MediaInfoResult>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize MediaInfo JSON.");

            // Attach the file path to the parsed object
            return new ScannedFileInfo()
            {
                FilePath = filePath,
                Result = mediaInfoResult
            };
        }

        /// <summary>
        /// Ensures that the directory specified in the options exists.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_options.DirectoryPath))
                throw new DirectoryNotFoundException("Directory does not exist.");
        }

        /// <summary>
        /// Retrieves files from the directory based on the filtering options.
        /// </summary>
        /// <returns>A collection of file paths that match the filtering criteria.</returns>
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
        /// Analyzes the specified files using MediaInfo and returns detailed information.
        /// </summary>
        /// <param name="files">The collection of file paths to analyze.</param>
        /// <returns>A collection of <see cref="ScannedFileInfo"/> objects containing file paths and MediaInfo summaries.</returns>
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

        /// <summary>
        /// Gets the list of scanned files.
        /// </summary>
        /// <returns>The list of scanned files.</returns>
        public IEnumerable<ScannedFileInfo> GetScannedFiles() => _scannedFiles;
    }
}
